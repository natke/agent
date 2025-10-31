using System.ComponentModel;
using System.ClientModel;
using Newtonsoft.Json;
using OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Configuration;

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var alias = "qwen2.5-7b";

Console.WriteLine("Starting model...");

var manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId: alias);
var model = await manager.GetModelInfoAsync(aliasOrModelId: alias);

IList<AITool> tools = [
    AIFunctionFactory.Create(Reverse),
    AIFunctionFactory.Create(SendSms),
    AIFunctionFactory.Create(GetWeather)];

AIAgent weatherAgent = new OpenAIClient(
  new ApiKeyCredential(manager.ApiKey),
  new OpenAIClientOptions { Endpoint = manager.Endpoint })
    .GetChatClient(model?.ModelId ?? "qwen2.5-7b")
    .CreateAIAgent(
        instructions: "You answer questions about the weather.",
        name: "WeatherAgent",
        description: "An agent that answers questions about the weather.",
        tools: [AIFunctionFactory.Create(GetWeather)]);

AIAgent messageAgent = new OpenAIClient(
  new ApiKeyCredential(manager.ApiKey),
  new OpenAIClientOptions { Endpoint = manager.Endpoint })
    .GetChatClient(model?.ModelId ?? "qwen2.5-7b")
    .CreateAIAgent(
        instructions: "You send text messages.",
        name: "MessageAgent",
        description: "An agent that sends messages.",
        tools: [AIFunctionFactory.Create(SendSms), weatherAgent.AsAIFunction()]);

AIAgent agent = new OpenAIClient(
  new ApiKeyCredential(manager.ApiKey),
  new OpenAIClientOptions { Endpoint = manager.Endpoint })
    .GetChatClient(model?.ModelId ?? "qwen2.5-7b")
    .CreateAIAgent(
        instructions: "You are a helpful assistant who responds in French.",
        tools: [messageAgent.AsAIFunction()]);

Console.WriteLine(await agent.RunAsync("Find out what is the weather is like in Sydney and send it via sms to (123) 234-3456?"));

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
        return "Error: Weather API key not configured";
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
