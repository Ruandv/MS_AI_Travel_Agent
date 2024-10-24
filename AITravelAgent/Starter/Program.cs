using System.Text;
using AITravelAgent;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
#pragma warning disable SKEXP0050 
#pragma warning disable SKEXP0060


// Build configuration
var configBuilder = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string yourDeploymentName = configBuilder["DeploymentName"];
string yourEndpoint = configBuilder["EndpointUrl"];
string yourApiKey = configBuilder["AzureAiApiKey"];
string yourAiModel = configBuilder["AiModel"];

var builder = Kernel.CreateBuilder();
builder.Services.AddAzureOpenAIChatCompletion(
    yourDeploymentName,
    yourEndpoint,
    yourApiKey,
    yourAiModel);
var kernel = builder.Build();

// Note: ChatHistory isn't working correctly as of SemanticKernel v 1.4.0
StringBuilder chatHistory = new();
OpenAIPromptExecutionSettings settings = new()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};
kernel.ImportPluginFromType<ConversationSummaryPlugin>();
kernel.ImportPluginFromType<CurrencyConverter>();
var prompts = kernel.ImportPluginFromPromptDirectory("Prompts");

do
{
    Console.WriteLine("What would you like to do?");
    var input = Console.ReadLine();

    var intent = await kernel.InvokeAsync<string>(
        prompts["GetIntent"],
        new() { { "input", input } }
    );

    switch (intent)
    {
        case "ConvertCurrency":
            var currencyText = await kernel.InvokeAsync<string>(
                prompts["GetTargetCurrencies"],
                new() { { "input", input } }
            );
            var currencyInfo = currencyText!.Split("|");
            var result = await kernel.InvokeAsync("CurrencyConverter",
                "ConvertAmount",
                new() {
                {"targetCurrencyCode", currencyInfo[0]},
                {"baseCurrencyCode", currencyInfo[1]},
                {"amount", currencyInfo[2]},
                }
            );
            Console.WriteLine(result);
            break;

        case "SuggestDestinations":
            chatHistory.AppendLine("User:" + input);
            var recommendations = await kernel.InvokePromptAsync(input!);
            Console.WriteLine(recommendations);
            break;
        case "SuggestActivities":
            var chatSummary = await kernel.InvokeAsync(
                "ConversationSummaryPlugin",
                "SummarizeConversation",
                new() { { "input", chatHistory.ToString() } });
            var activities = await kernel.InvokePromptAsync(input,
                        new() {
                    {"input", input},
                    {"history", chatSummary},
                    {"ToolCallBehavior", ToolCallBehavior.AutoInvokeKernelFunctions} });

            chatHistory.AppendLine("User:" + input);
            chatHistory.AppendLine("Assistant:" + activities.ToString());

            Console.WriteLine(activities);
            break;
        case "HelpfulPhrases":
        case "Translate":
            chatHistory.AppendLine("User:" + input);
            var autoInvokeResult = await kernel.InvokePromptAsync(input!, new(settings));
            Console.WriteLine(autoInvokeResult);
            break;
        default:
            Console.WriteLine("Other intent detected");
            break;
    }

} while ("prompt" != "exit");

Console.ReadLine();