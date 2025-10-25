# Run Microsoft Agent Factory on device

This sample is adapted from the Microsoft Agent Framework [getting started sample](https://learn.microsoft.com/en-us/agent-framework/tutorials/quick-start?pivots=programming-language-csharp).

It uses the OpenAI Client instread of the AzureOpenAIClient. This is because the AzureOpenAIClient adds a `v1/openai/deployments` to the chatcompletion endpoint and Foundry Local does not serve that endpoint.

## Setup

1. Clone this repo

2. Install Foundry Local (version > 0.8.94)

## Run

1. dotnet run