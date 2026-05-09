using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public interface INotificationService
{
    Task<(List<NotificationLogDto> Items, int Total)> GetAllAsync(string? candidateName, string? notificationType, string? status, int page, int pageSize);
    Task<bool> SendNotificationAsync(SendNotificationRequest request);
    Task<List<NotificationTemplateDto>> GetTemplatesAsync();
    Task<bool> UpdateTemplateAsync(int id, UpdateNotificationTemplateRequest request);
}

public class NotificationService : INotificationService
{
    private readonly TalentPilotDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly bool _isDevelopment;

    public NotificationService(TalentPilotDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _isDevelopment = _configuration.GetValue<bool>("IsDevelopment");
    }

    public async Task<(List<NotificationLogDto> Items, int Total)> GetAllAsync(
        string? candidateName, string? notificationType, string? status, int page, int pageSize)
    {
        var query = _dbContext.NotificationLogs
            .Include(n => n.Candidate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(candidateName))
        {
            query = query.Where(n => n.Candidate != null && n.Candidate.Name.Contains(candidateName));
        }

        if (!string.IsNullOrWhiteSpace(notificationType))
        {
            query = query.Where(n => n.NotificationType == notificationType);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(n => n.Status == status);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationLogDto
            {
                Id = n.Id,
                CandidateId = n.CandidateId,
                CandidateName = n.Candidate != null ? n.Candidate.Name : null,
                NotificationType = n.NotificationType,
                Channel = n.Channel,
                Recipient = n.Recipient,
                Subject = n.Subject,
                Content = n.Content,
                Status = n.Status,
                SentAt = n.SentAt,
                CreatedAt = n.CreatedAt,
                ErrorMessage = n.ErrorMessage
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> SendNotificationAsync(SendNotificationRequest request)
    {
        var candidate = await _dbContext.Candidates.FindAsync(request.CandidateId);
        if (candidate == null) return false;

        var template = GetTemplate(request.NotificationType, request.Channel);
        var variables = request.TemplateVariables ?? new Dictionary<string, string>();

        variables.TryAdd("candidate_name", candidate.Name ?? "");
        variables.TryAdd("company_name", "TalentPilot");

        var recipient = request.Channel == NotificationChannel.Email.ToString()
            ? candidate.Email
            : candidate.Phone;

        if (string.IsNullOrEmpty(recipient))
            return false;

        var subject = ReplaceTemplateVariables(template.Subject, variables);
        var content = ReplaceTemplateVariables(template.Content, variables);

        var notificationLog = new NotificationLog
        {
            CandidateId = request.CandidateId,
            NotificationType = request.NotificationType,
            Channel = request.Channel,
            Recipient = recipient,
            Subject = subject,
            Content = content,
            Status = NotificationStatus.Pending.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.NotificationLogs.Add(notificationLog);
        await _dbContext.SaveChangesAsync();

        try
        {
            if (_isDevelopment)
            {
                Console.WriteLine($"[DEV] Sending {request.Channel} notification to {recipient}");
                Console.WriteLine($"[DEV] Subject: {subject}");
                Console.WriteLine($"[DEV] Content: {content}");
            }
            else
            {
                await SendEmailAsync(recipient, subject, content);
            }

            notificationLog.Status = NotificationStatus.Sent.ToString();
            notificationLog.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            notificationLog.Status = NotificationStatus.Failed.ToString();
            notificationLog.ErrorMessage = ex.Message;
        }

        await _dbContext.SaveChangesAsync();
        return notificationLog.Status == NotificationStatus.Sent.ToString();
    }

    public Task<List<NotificationTemplateDto>> GetTemplatesAsync()
    {
        var templates = new List<NotificationTemplateDto>
        {
            new()
            {
                Id = 1,
                Name = "Interview Invitation",
                NotificationType = NotificationType.InterviewInvitation.ToString(),
                Channel = NotificationChannel.Email.ToString(),
                Subject = "Interview Invitation - {{job_title}}",
                Content = "Dear {{candidate_name}},\n\nYou have been invited for an interview for the position of {{job_title}}.\n\nInterview Time: {{interview_time}}\nInterview Mode: {{interview_mode}}\n\nBest regards,\n{{company_name}}",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Name = "Interview Reminder",
                NotificationType = NotificationType.InterviewReminder.ToString(),
                Channel = NotificationChannel.Email.ToString(),
                Subject = "Reminder: Interview Tomorrow - {{job_title}}",
                Content = "Dear {{candidate_name}},\n\nThis is a reminder about your upcoming interview for {{job_title}}.\n\nInterview Time: {{interview_time}}\nInterview Mode: {{interview_mode}}\n\nBest regards,\n{{company_name}}",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                Name = "Offer",
                NotificationType = NotificationType.Offer.ToString(),
                Channel = NotificationChannel.Email.ToString(),
                Subject = "Offer Letter - {{job_title}}",
                Content = "Dear {{candidate_name}},\n\nCongratulations! We are pleased to offer you the position of {{job_title}}.\n\nPlease review the details and let us know your decision.\n\nBest regards,\n{{company_name}}",
                CreatedAt = DateTime.UtcNow
            }
        };

        return Task.FromResult(templates);
    }

    public Task<bool> UpdateTemplateAsync(int id, UpdateNotificationTemplateRequest request)
    {
        // Templates are in-memory for this implementation
        // In production, these would be stored in a database
        return Task.FromResult(id > 0 && id <= 3);
    }

    private static (string Subject, string Content) GetTemplate(string notificationType, string channel)
    {
        var templates = new Dictionary<string, (string Subject, string Content)>
        {
            [NotificationType.InterviewInvitation.ToString()] = (
                "Interview Invitation - {{job_title}}",
                "Dear {{candidate_name}},\n\nYou have been invited for an interview for the position of {{job_title}}.\n\nInterview Time: {{interview_time}}\nInterview Mode: {{interview_mode}}\n\nBest regards,\n{{company_name}}"
            ),
            [NotificationType.InterviewReminder.ToString()] = (
                "Reminder: Interview Tomorrow - {{job_title}}",
                "Dear {{candidate_name}},\n\nThis is a reminder about your upcoming interview for {{job_title}}.\n\nInterview Time: {{interview_time}}\nInterview Mode: {{interview_mode}}\n\nBest regards,\n{{company_name}}"
            ),
            [NotificationType.Offer.ToString()] = (
                "Offer Letter - {{job_title}}",
                "Dear {{candidate_name}},\n\nCongratulations! We are pleased to offer you the position of {{job_title}}.\n\nPlease review the details and let us know your decision.\n\nBest regards,\n{{company_name}}"
            )
        };

        var key = $"{notificationType}_{channel}";
        return templates.TryGetValue(notificationType, out var template)
            ? template
            : ("Notification", "You have a new notification from {{company_name}}.");
    }

    private static string ReplaceTemplateVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }
        return result;
    }

    private Task SendEmailAsync(string to, string subject, string content)
    {
        // Email sending implementation would go here
        // In production, integrate with email service (SendGrid, SMTP, etc.)
        return Task.CompletedTask;
    }
}