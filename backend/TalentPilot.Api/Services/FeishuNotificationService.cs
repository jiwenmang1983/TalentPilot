using System.Text.Json;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public interface IFeishuNotificationService
{
    Task SendInterviewStartedNotificationAsync(int sessionId, string candidateName, string jobTitle, DateTime startTime);
}

public class FeishuNotificationService : IFeishuNotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FeishuNotificationService> _logger;

    public FeishuNotificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FeishuNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendInterviewStartedNotificationAsync(int sessionId, string candidateName, string jobTitle, DateTime startTime)
    {
        var webhookUrl = _configuration["Feishu:WebhookUrl"] ?? "";
        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogWarning("Feishu webhook URL not configured, skipping notification");
            return;
        }

        var card = new
        {
            msg_type = "interactive",
            card = new
            {
                header = new
                {
                    title = new { tag = "plain_text", content = "AI面试已开始 🔔" },
                    style = 1
                },
                elements = new object[]
                {
                    new { tag = "div", text = new { tag = "lark_md", content = $"**候选人**: {candidateName}" } },
                    new { tag = "div", text = new { tag = "lark_md", content = $"**职位**: {jobTitle}" } },
                    new { tag = "div", text = new { tag = "lark_md", content = $"**开始时间**: {startTime:yyyy-MM-dd HH:mm:ss}" } },
                    new { tag = "div", text = new { tag = "lark_md", content = $"**会话ID**: {sessionId}" } },
                    new { tag = "hr" },
                    new { tag = "note", elements = new[] { new { tag = "plain_text", content = $"通知时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" } } }
                }
            }
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(card);
            var response = await client.PostAsJsonAsync(webhookUrl, card);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Feishu notification sent for session {SessionId}", sessionId);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Feishu notification failed for session {SessionId}: {Error}", sessionId, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Feishu notification for session {SessionId}", sessionId);
        }
    }
}