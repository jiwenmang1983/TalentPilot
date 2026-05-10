using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TalentPilot.Api.Services;

public interface IVoiceService
{
    Task<byte[]?> GenerateSpeechAsync(string text);
}

public class VoiceService : IVoiceService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<VoiceService> _logger;

    public VoiceService(IHttpClientFactory httpClientFactory, ILogger<VoiceService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MiniMax");
        _apiKey = "sk-cp-AUnpds5ndMyU9JT_zzPyUr81Dg_BFOmzKVHXisUMgvqRE9Zqdb7zFb-elTi9liCZINk0BTEk8Sq9etMkPJgXausjqEOdYwe1VlQIhey2tRROq08nROkSWD8";
        _model = "speech-02-hd";
        _logger = logger;
    }

    public async Task<byte[]?> GenerateSpeechAsync(string text)
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = $"请用中文朗读以下内容：{text}" }
                }
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
                _logger.LogWarning("MiniMax TTS API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            using var doc = JsonDocument.Parse(responseContent);
            if (doc.RootElement.TryGetProperty("audio", out var audioElement))
            {
                var audioData = audioElement.GetString();
                if (!string.IsNullOrEmpty(audioData))
                {
                    return Convert.FromBase64String(audioData);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate speech, continuing without audio");
            return null;
        }
    }
}