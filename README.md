# Run Microsoft Agent Factory on device

This sample is adapted from the Microsoft Agent Framework [tutorial](https://learn.microsoft.com/en-us/agent-framework/tutorials/agents/agent-as-function-tool?pivots=programming-language-csharp).

It uses the OpenAI Client instread of the AzureOpenAIClient. This is because the AzureOpenAIClient seems to add `openai/deployments` to the chatcompletion endpoint and Foundry Local does not serve that endpoint.

## Setup

1. Clone this repo

2. Install Foundry Local (version > 0.8.96)

## Run

1. dotnet run

## Configuration

The application uses a configuration file to store API keys and settings securely.

### Setup Configuration

1. Copy `appsettings.sample.json` to `appsettings.json`
2. Replace `YOUR_VISUAL_CROSSING_API_KEY_HERE` with your actual Visual Crossing Weather API key
3. Optionally, modify other settings as needed

### Configuration Settings

#### WeatherApi Section

- **ApiKey**: Your Visual Crossing Weather API key (required)
- **BaseUrl**: Base URL for the weather API (optional, has default)
- **UnitGroup**: Temperature unit system - "metric", "us", or "uk" (optional, defaults to "metric")
- **ContentType**: Response format - "json" or "csv" (optional, defaults to "json")

### Getting an API Key

1. Visit [Visual Crossing Weather](https://www.visualcrossing.com/weather-api)
2. Sign up for a free account
3. Copy your API key from the account dashboard
4. Paste it into the `appsettings.json` file

### Security Note

Never commit `appsettings.json` with real API keys to version control. Use `appsettings.sample.json` as a template.

## Output

The current sample will output the tool call along with the final output e.g.

```
<tool_call>
{"name": "WeatherAgent", "arguments": {"query": "Amsterdam"}}
</tool_call></tool_call>
Le temps à Amsterdam est nuageux avec une température maximale de 15°C..
```