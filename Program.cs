using System.ComponentModel;
using System.ClientModel;
using Newtonsoft.Json;
using OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Create logger factory for agent logging
using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

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

Console.WriteLine("Starting model...");

var manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId: alias);
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


Console.WriteLine(await agent.RunAsync(prompt, options: runOptions));

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
