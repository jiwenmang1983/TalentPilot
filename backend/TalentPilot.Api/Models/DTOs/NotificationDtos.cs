namespace TalentPilot.Api.Models.DTOs;

public class NotificationLogDto
{
    public int Id { get; set; }
    public long CandidateId { get; set; }
    public string? CandidateName { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SendNotificationRequest
{
    public long CandidateId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = "Email";
    public Dictionary<string, string>? TemplateVariables { get; set; }
}

public class NotificationTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateNotificationTemplateRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class NotificationTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}