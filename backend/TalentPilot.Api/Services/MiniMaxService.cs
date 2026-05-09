using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TalentPilot.Api.Services;

public interface IMiniMaxService
{
    Task<MiniMaxMessageResponse?> ChatAsync(string prompt, int? maxTokens = 1024);
}

// Anthropic /v1/messages response format
public class MiniMaxMessageResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public List<MiniMaxContentBlock>? Content { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("usage")]
    public MiniMaxUsage? Usage { get; set; }

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }
}

public class MiniMaxContentBlock
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    // For MiniMax thinking blocks: thinking is a plain string, not an object
    [JsonPropertyName("thinking")]
    public string? Thinking { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

public class MiniMaxUsage
{
    [JsonPropertyName("input_tokens")]
    public int? InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int? OutputTokens { get; set; }
}

// Legacy compatibility (kept for other code that might reference it)
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

    public async Task<MiniMaxMessageResponse?> ChatAsync(string prompt, int? maxTokens = 1024)
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

            var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri("https://api.minimaxi.com/anthropic/v1/messages"))
            {
                Content = content
            };
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MiniMax API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            return JsonSerializer.Deserialize<MiniMaxMessageResponse>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call MiniMax API");
            return null;
        }
    }
}