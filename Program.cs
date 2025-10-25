using System;
using Microsoft.AI.Foundry.Local;
using Microsoft.Agents.AI;
using OpenAI;

using System.ClientModel;
using Microsoft.Extensions.AI;
using Azure.Core.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Tracing;

var alias = "qwen2.5-7b";

Console.WriteLine("Starting model...");

var manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId: alias);
var model = await manager.GetModelInfoAsync(aliasOrModelId: alias);

// Debug the manager and model details
Console.WriteLine($"Manager endpoint: {manager.Endpoint}");
Console.WriteLine($"Manager API key: {(string.IsNullOrEmpty(manager.ApiKey) ? "NOT SET" : "SET")}");
Console.WriteLine($"Model info: {model?.ModelId}");

Console.WriteLine($"Creating OpenAI client with endpoint: {manager.Endpoint}");
Console.WriteLine($"Using model: {model?.ModelId}");

AIAgent agent = new OpenAIClient(
  new ApiKeyCredential(manager.ApiKey),
  new OpenAIClientOptions { Endpoint = manager.Endpoint })
    .GetChatClient(model?.ModelId ?? "qwen2.5-7b")
    .CreateAIAgent(instructions: "You are good at telling jokes.");

Console.WriteLine("Sending request to OpenAI API...");

try
{
    var response = await agent.RunAsync("Tell me a joke about a pirate.");
    
    Console.WriteLine("Response received:");
    Console.WriteLine(response);
}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
    }
    
    // Print full exception details for debugging
    Console.WriteLine($"Full exception: {ex}");
}

