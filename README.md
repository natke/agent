# Run Microsoft Agent Factory on device

This sample is adapted from the Microsoft Agent Framework [tutorial](https://learn.microsoft.com/en-us/agent-framework/tutorials/agents/agent-as-function-tool?pivots=programming-language-csharp).

It uses the OpenAI Client instread of the AzureOpenAIClient. This is because the AzureOpenAIClient seems to a `v1/openai/deployments` to the chatcompletion endpoint and Foundry Local does not serve that endpoint.

## Setup

1. Clone this repo

2. Install Foundry Local (version > 0.8.94)

## Run

1. dotnet run

## Output

The current sample will output the tool call along with the final output e.g.

```
<tool_call>
{"name": "WeatherAgent", "arguments": {"query": "Amsterdam"}}
</tool_call></tool_call>
Le temps à Amsterdam est nuageux avec une température maximale de 15°C..
```