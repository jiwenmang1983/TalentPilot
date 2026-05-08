namespace TalentPilot.Api.Models.DTOs;

public class CreateInterviewInvitationRequest
{
    public int CandidateId { get; set; }
    public int JobPostId { get; set; }
    public DateTime TimeSlotStart { get; set; }
    public DateTime TimeSlotEnd { get; set; }
    public string? Notes { get; set; }
    public DateTime? InterviewTime { get; set; }
}

public class UpdateInterviewInvitationRequest
{
    public DateTime? InterviewTime { get; set; }
    public string? Notes { get; set; }
}

public class InvitationStatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
}

public class InterviewInvitationResponse
{
    public int Id { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string? CandidatePhone { get; set; }
    public string? CandidateEmail { get; set; }
    public string JobPostTitle { get; set; } = string.Empty;
    public string InvitedByUserName { get; set; } = string.Empty;
    public DateTime? InterviewTime { get; set; }
    public DateTime TimeSlotStart { get; set; }
    public DateTime TimeSlotEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? RefusedAt { get; set; }
    public string? Notes { get; set; }
    public string InviteToken { get; set; } = string.Empty;
}

public class InterviewInvitationTokenResponse
{
    public int Id { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string? CandidatePhone { get; set; }
    public string? CandidateEmail { get; set; }
    public string JobPostTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime? InterviewTime { get; set; }
    public DateTime TimeSlotStart { get; set; }
    public DateTime TimeSlotEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? InviteSentAt { get; set; }
}
