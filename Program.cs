using System.ComponentModel;
using System.ClientModel;
using Newtonsoft.Json;
using OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Realtime;

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Create logger factory for agent logging
using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Error));

var alias = "qwen2.5-7b";

ChatOptions options = new()
{
    ToolMode = ChatToolMode.RequireAny,
};

ChatClientAgentRunOptions runOptions = new()
{
    ChatOptions = options
};

var defaultPrompt = "Find out what the weather is like in Sydney and send it via sms to (123) 234-3456. Respond in French";
var prompt = GetPromptFromArgs(args) ?? defaultPrompt;

var manager = await RunWithAnimationAsync(
    "Model starting",
    "Model started",
    alias,
    () => FoundryLocalManager.StartModelAsync(aliasOrModelId: alias));

var model = await manager.GetModelInfoAsync(aliasOrModelId: alias);

IList<AITool> tools = [
    AIFunctionFactory.Create(Reverse),
    AIFunctionFactory.Create(SendSms),
    AIFunctionFactory.Create(GetWeather)];

AIAgent translationAgent = new OpenAIClient(
  new ApiKeyCredential(manager.ApiKey),
  new OpenAIClientOptions { Endpoint = manager.Endpoint })
    .GetChatClient(model?.ModelId ?? alias)
    .AsIChatClient()
    .AsBuilder()
    .UseLogging(loggerFactory)
    .Build()
    .CreateAIAgent(
        instructions: "You translate a message into French.",
        name: "TranslationAgent",
        description: "An agent that translates messages into French.");

AIAgent agent = new OpenAIClient(
  new ApiKeyCredential(manager.ApiKey),
  new OpenAIClientOptions { Endpoint = manager.Endpoint })
    .GetChatClient(model?.ModelId ?? alias)
    .AsIChatClient()
    .AsBuilder()
    .UseLogging(loggerFactory)
    .Build()
    .CreateAIAgent(
        instructions: "You are a helpful assistant with some tools.",
        tools: [AIFunctionFactory.Create(SendSms), AIFunctionFactory.Create(GetWeather), translationAgent.AsAIFunction()]);

var response = await RunWithAnimationAsync(
    "Agent running",
    null,
    prompt,
    () => agent.RunAsync(prompt, options: runOptions));

WriteConsoleOutput("Agent output", response.AsChatResponse().Messages[^1].ToString());

static async Task<T> RunWithAnimationAsync<T>(string startLabel, string? completedLabel, string value, Func<Task<T>> taskFunc)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write(startLabel);
    Console.ResetColor();
    var animationCursorLeft = Console.CursorLeft;
    var animationCursorTop = Console.CursorTop;
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.SetCursorPosition(27, animationCursorTop);
    Console.Write(value);
    Console.ResetColor();

    using var cts = new CancellationTokenSource();
    var animationTask = AnimateDotsAsync(animationCursorTop, animationCursorLeft, cts.Token);
    var result = await taskFunc();
    cts.Cancel();
    await animationTask;

    // Clear the dots
    Console.SetCursorPosition(animationCursorLeft, animationCursorTop);
    Console.Write(new string(' ', 27 - animationCursorLeft));

    if (completedLabel != null)
    {
        // Replace with completed label
        Console.SetCursorPosition(0, animationCursorTop);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(completedLabel.PadRight(27));
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write(value);
        Console.ResetColor();
    }
    Console.WriteLine();

    return result;
}

static async Task AnimateDotsAsync(int cursorTop, int cursorLeft, CancellationToken cancellationToken)
{
    int dotCount = 0;
    try
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            dotCount = (dotCount % 3) + 1;
            await Task.Delay(500, cancellationToken);
            Console.SetCursorPosition(cursorLeft, cursorTop);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(new string('.', dotCount));
            Console.Write(new string(' ', 3 - dotCount));
            Console.ResetColor();
        }
    }
    catch (OperationCanceledException)
    {
        // Animation cancelled, which is expected
    }
}

static void WriteConsoleOutput(string label, string value)
{
    const int labelWidth = 27; // Width of longest label "Running agent with prompt:"
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write(label.PadRight(labelWidth));
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine(value);
    Console.ResetColor();
}

static void ShowUsage()
{
    Console.WriteLine("Usage: agent [--prompt|-p <text>] [--help|-h]");
    Console.WriteLine("Options:");
    Console.WriteLine("  --prompt, -p    Specify the input prompt text");
    Console.WriteLine("  --help, -h      Show this help message and exit");
}

static string? GetPromptFromArgs(string[] args)
{
    if (args is null || args.Length == 0)
    {
        return null;
    }

    for (int i = 0; i < args.Length; i++)
    {
        var a = args[i];
        if (a == "--help" || a == "-h")
        {
            ShowUsage();
            Environment.Exit(0);
        }
        if (a == "--prompt" || a == "-p")
        {
            if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
            {
                return args[i + 1];
            }
            Console.Error.WriteLine("Error: Missing value for --prompt/-p.");
            ShowUsage();
            Environment.Exit(1);
        }
        if (a.StartsWith("--prompt="))
        {
            var val = a.Substring("--prompt=".Length);
            if (string.IsNullOrEmpty(val))
            {
                Console.Error.WriteLine("Error: Missing value for --prompt.");
                ShowUsage();
                Environment.Exit(1);
            }
            return val;
        }
        if (a.StartsWith("-p="))
        {
            var val = a.Substring(3);
            if (string.IsNullOrEmpty(val))
            {
                Console.Error.WriteLine("Error: Missing value for -p.");
                ShowUsage();
                Environment.Exit(1);
            }
            return val;
        }
    }
    return null;
}

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
{
    // Load configuration to get API settings
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
    
    var apiKey = configuration["WeatherApi:ApiKey"];
    if (string.IsNullOrEmpty(apiKey))
    {
        return "Error: Weather API key not configured.";
    }
    
    var baseUrl = configuration["WeatherApi:BaseUrl"] ?? "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline";
    var unitGroup = configuration["WeatherApi:UnitGroup"] ?? "metric";
    var contentType = configuration["WeatherApi:ContentType"] ?? "json";

    try
    {
        using var httpClient = new HttpClient();
        string url = $"{baseUrl}/{location}?unitGroup={unitGroup}&key={apiKey}&contentType={contentType}";

        HttpResponseMessage response = httpClient.GetAsync(url).Result;
        response.EnsureSuccessStatusCode();

        string jsonResponse = response.Content.ReadAsStringAsync().Result;
        dynamic? weatherData = JsonConvert.DeserializeObject(jsonResponse);

        if (weatherData?.days?.Count > 0)
        {
            var today = weatherData.days[0];
            return $"Weather in {location}: {today.description}. Temperature: {today.tempmax}°C max, {today.tempmin}°C min.";
        }
        
        return $"Weather data not available for {location}";
    }
    catch (Exception e)
    {
        return $"Error getting weather for {location}: {e.Message}";
    }
}


[Description("Given a phone number and a message send an SMS")]
static string SendSms([Description("The message to send")] string message, [Description("The number to send it to")] string phoneNumber)
    => $"SMS sent";

[Description("Given a string, return the reverse of that string")]
static string Reverse([Description("The string to be reversed")] string input)
    => $"String reversed";
