using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TalentPilot.Api.Services;

public interface IMiniMaxService
{
    Task<MiniMaxChatResponse?> ChatAsync(string prompt, int? maxTokens = 1024);
}

public class MiniMaxChatResponse
{
    [JsonPropertyName("choices")]
    public List<MiniMaxChoice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public MiniMaxUsage? Usage { get; set; }
}

public class MiniMaxChoice
{
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("messages")]
    public List<MiniMaxMessage>? Messages { get; set; }
}

public class MiniMaxMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public class MiniMaxUsage
{
    [JsonPropertyName("tokens")]
    public int? Tokens { get; set; }

    [JsonPropertyName("prompt_tokens")]
    public int? PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int? CompletionTokens { get; set; }
}

public class MiniMaxService : IMiniMaxService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<MiniMaxService> _logger;

    public MiniMaxService(IHttpClientFactory httpClientFactory, ILogger<MiniMaxService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MiniMax");
        _apiKey = "sk-cp-AUnpds5ndMyU9JT_zzPyUr81Dg_BFOmzKVHXisUMgvqRE9Zqdb7zFb-elTi9liCZINk0BTEk8Sq9etMkPJgXausjqEOdYwe1VlQIhey2tRROq08nROkSWD8";
        _model = "MiniMax-M2.7";
        _logger = logger;
    }

    public async Task<MiniMaxChatResponse?> ChatAsync(string prompt, int? maxTokens = 1024)
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = maxTokens
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/text/chatcompletion_free")
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MiniMax API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            return JsonSerializer.Deserialize<MiniMaxChatResponse>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call MiniMax API");
            return null;
        }
    }
}