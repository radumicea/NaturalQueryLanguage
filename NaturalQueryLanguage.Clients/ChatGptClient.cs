using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Tiktoken;

namespace NaturalQueryLanguage.Business;

public class ChatGptClient(IConfiguration configuration)
{
    private readonly OpenAIClient _client = new(configuration["ChatGpt:ApiKey"]!);

    public async Task<ChatGptResponse> DoRequestAsync(ChatGptRequest request)
    {
        if (request.UserMessages.Any(string.IsNullOrWhiteSpace) ||
            request.AssistantMessages.Any(string.IsNullOrWhiteSpace))
        {
            return new ChatGptResponse { Message = "Messages can not be empty.", StatusCode = 400 };
        }

        if (request.UserMessages.Count() != request.AssistantMessages.Count() + 1)
        {
            return new ChatGptResponse { Message = "No user message.", StatusCode = 400 };
        }

        var deploymentName = request.Model.GetDeploymentName();
        var contextWindow = request.Model.GetContextWindow();
        var maxOutputTokens = request.Model.GetMaxOutputTokens();
        var encodings = ModelToEncoder.For(deploymentName);

        var totalMaxNrTokens = request.SystemMessage is null
            ? 0
            : encodings.CountTokens(request.SystemMessage);
        totalMaxNrTokens += request.AssistantMessages.Sum(encodings.CountTokens);
        totalMaxNrTokens += request.UserMessages.Sum(encodings.CountTokens);
        totalMaxNrTokens += maxOutputTokens;

        var userMessages = new LinkedList<string>(request.UserMessages);
        var assistantMessages = new LinkedList<string>(request.AssistantMessages);

        while (assistantMessages.Count > 0 && totalMaxNrTokens > contextWindow)
        {
            totalMaxNrTokens -= encodings.CountTokens(userMessages.First!.Value);
            totalMaxNrTokens -= encodings.CountTokens(assistantMessages.First!.Value);

            userMessages.RemoveFirst();
            assistantMessages.RemoveFirst();
        }

        if (totalMaxNrTokens > contextWindow)
        {
            return new ChatGptResponse { Message = "Input size larger than context window.", StatusCode = 400 };
        }

        var options = new ChatCompletionsOptions
        {
            DeploymentName = deploymentName,
        };

        if (!string.IsNullOrWhiteSpace(request.SystemMessage))
        {
            options.Messages.Add(new ChatRequestSystemMessage(request.SystemMessage));
        }

        foreach (var (u, a) in userMessages.Zip(assistantMessages))
        {
            options.Messages.Add(new ChatRequestUserMessage(u));
            options.Messages.Add(new ChatRequestAssistantMessage(a));
        }

        options.Messages.Add(new ChatRequestUserMessage(request.UserMessages.Last()));

        var response = await _client.GetChatCompletionsAsync(options);
        var message = response.Value.Choices[0].Message.Content;

        return new ChatGptResponse
        {
            TokensUsed = totalMaxNrTokens - maxOutputTokens + encodings.CountTokens(message),
            StatusCode = response.GetRawResponse().Status,
            Message = message
        };
    }
}

public class ChatGptRequest
{
    public GptModel Model { get; set; } = GptModel.GPT_4o_Mini;
    public string? SystemMessage { get; set; }
    public IEnumerable<string> UserMessages { get; set; } = [];
    public IEnumerable<string> AssistantMessages { get; set; } = [];
}

public class ChatGptResponse
{
    public int TokensUsed { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = null!;
}

public enum GptModel
{
    GPT_4o,
    GPT_4o_Mini,
}

public static class GptModelExtensions
{
    private static readonly IReadOnlyDictionary<GptModel, string> _deploymentNames = new Dictionary<GptModel, string>
    {
        [GptModel.GPT_4o] = "gpt-4o",
        [GptModel.GPT_4o_Mini] = "gpt-4o-mini",
    };

    private static readonly IReadOnlyDictionary<GptModel, int> _contextWindows = new Dictionary<GptModel, int>
    {
        [GptModel.GPT_4o] = 128_000,
        [GptModel.GPT_4o_Mini] = 128_000,
    };

    private static IReadOnlyDictionary<GptModel, int> _maxOutputTokens = new Dictionary<GptModel, int>
    {
        [GptModel.GPT_4o] = 4_096,
        [GptModel.GPT_4o_Mini] = 4_096
    };

    public static string GetDeploymentName(this GptModel model)
    {
        return _deploymentNames[model];
    }

    public static int GetContextWindow(this GptModel model)
    {
        return _contextWindows[model];
    }

    public static int GetMaxOutputTokens(this GptModel model)
    {
        return _maxOutputTokens[model];
    }
}