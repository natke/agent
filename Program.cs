using System.ComponentModel;
using Microsoft.AI.Foundry.Local;
using Microsoft.Agents.AI;
using OpenAI;

using System.ClientModel;
using Microsoft.Extensions.AI;

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

AIAgent agent = new OpenAIClient(
  new ApiKeyCredential(manager.ApiKey),
  new OpenAIClientOptions { Endpoint = manager.Endpoint })
    .GetChatClient(model?.ModelId ?? "qwen2.5-7b")
    .CreateAIAgent(
        instructions: "You are a helpful assistant who responds in French.",
        tools: [weatherAgent.AsAIFunction()]);

Console.WriteLine(await agent.RunAsync("What is the weather like in Amsterdam?"));

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";

[Description("Given a phone number and a message send an SMS")]
static string SendSms([Description("The message to send")] string message, [Description("The number to send it to")] string phoneNumber)
    => $"SMS sent";

[Description("Given a string, return the reverse of that string")]
static string Reverse([Description("The string to be reversed")] string input)
    => $"String reversed";

