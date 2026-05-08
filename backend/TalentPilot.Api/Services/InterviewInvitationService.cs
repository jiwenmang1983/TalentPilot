using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public interface IInterviewInvitationService
{
    Task<(List<InterviewInvitation> Items, int Total)> GetAllAsync(string? status, int page, int pageSize, int userId);
    Task<InterviewInvitation?> GetByIdAsync(int id);
    Task<InterviewInvitation?> GetByTokenAsync(string token);
    Task<InterviewInvitationResponse?> CreateAsync(CreateInterviewInvitationRequest request, int userId);
    Task<InterviewInvitation?> UpdateAsync(int id, UpdateInterviewInvitationRequest request);
    Task<bool> DeleteAsync(int id);
    Task<InterviewInvitation?> SendInviteAsync(int id);
    Task<InterviewInvitation?> ConfirmAsync(int id, DateTime? interviewTime);
    Task<InterviewInvitation?> RefuseAsync(int id);
    Task<InterviewInvitation?> CancelAsync(int id);
}

public class InterviewInvitationService : IInterviewInvitationService
{
    private readonly TalentPilotDbContext _dbContext;

    public InterviewInvitationService(TalentPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(List<InterviewInvitation> Items, int Total)> GetAllAsync(string? status, int page, int pageSize, int userId)
    {
        var query = _dbContext.InterviewInvitations
            .Include(i => i.Candidate)
            .Include(i => i.JobPost)
            .Include(i => i.InvitedByUser)
            .Where(i => i.InvitedByUserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(i => i.Status == status);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<InterviewInvitation?> GetByIdAsync(int id)
    {
        return await _dbContext.InterviewInvitations
            .Include(i => i.Candidate)
            .Include(i => i.JobPost)
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<InterviewInvitation?> GetByTokenAsync(string token)
    {
        return await _dbContext.InterviewInvitations
            .Include(i => i.Candidate)
            .Include(i => i.JobPost)
            .FirstOrDefaultAsync(i => i.InviteToken == token);
    }

    public async Task<InterviewInvitationResponse?> CreateAsync(CreateInterviewInvitationRequest request, int userId)
    {
        var invitation = new InterviewInvitation
        {
            CandidateId = request.CandidateId,
            JobPostId = request.JobPostId,
            InvitedByUserId = userId,
            TimeSlotStart = request.TimeSlotStart,
            TimeSlotEnd = request.TimeSlotEnd,
            InterviewTime = request.InterviewTime,
            Notes = request.Notes,
            Status = InterviewInvitationStatus.PendingConfirmation.ToString(),
            InviteToken = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.InterviewInvitations.Add(invitation);
        await _dbContext.SaveChangesAsync();

        return await GetResponseAsync(invitation.Id);
    }

    public async Task<InterviewInvitation?> UpdateAsync(int id, UpdateInterviewInvitationRequest request)
    {
        var invitation = await _dbContext.InterviewInvitations.FindAsync(id);
        if (invitation == null) return null;

        if (request.InterviewTime.HasValue)
            invitation.InterviewTime = request.InterviewTime;

        if (request.Notes != null)
            invitation.Notes = request.Notes;

        await _dbContext.SaveChangesAsync();
        return invitation;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var invitation = await _dbContext.InterviewInvitations.FindAsync(id);
        if (invitation == null) return false;

        if (invitation.Status != InterviewInvitationStatus.PendingConfirmation.ToString())
            return false;

        _dbContext.InterviewInvitations.Remove(invitation);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<InterviewInvitation?> SendInviteAsync(int id)
    {
        var invitation = await _dbContext.InterviewInvitations.FindAsync(id);
        if (invitation == null) return null;

        if (invitation.Status != InterviewInvitationStatus.PendingConfirmation.ToString())
            return null;

        invitation.InviteSentAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return invitation;
    }

    public async Task<InterviewInvitation?> ConfirmAsync(int id, DateTime? interviewTime)
    {
        var invitation = await _dbContext.InterviewInvitations.FindAsync(id);
        if (invitation == null) return null;

        if (!CanTransitionTo(invitation.Status, InterviewInvitationStatus.Confirmed.ToString()))
            return null;

        invitation.Status = InterviewInvitationStatus.Confirmed.ToString();
        invitation.ConfirmedAt = DateTime.UtcNow;
        if (interviewTime.HasValue)
            invitation.InterviewTime = interviewTime.Value;

        await _dbContext.SaveChangesAsync();
        return invitation;
    }

    public async Task<InterviewInvitation?> RefuseAsync(int id)
    {
        var invitation = await _dbContext.InterviewInvitations.FindAsync(id);
        if (invitation == null) return null;

        if (!CanTransitionTo(invitation.Status, InterviewInvitationStatus.Refused.ToString()))
            return null;

        invitation.Status = InterviewInvitationStatus.Refused.ToString();
        invitation.RefusedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return invitation;
    }

    public async Task<InterviewInvitation?> CancelAsync(int id)
    {
        var invitation = await _dbContext.InterviewInvitations.FindAsync(id);
        if (invitation == null) return null;

        if (!CanTransitionTo(invitation.Status, InterviewInvitationStatus.Cancelled.ToString()))
            return null;

        if (invitation.Status == InterviewInvitationStatus.Confirmed.ToString())
        {
            if (invitation.InterviewTime.HasValue &&
                invitation.InterviewTime.Value <= DateTime.UtcNow.AddHours(24))
                return null;
        }

        invitation.Status = InterviewInvitationStatus.Cancelled.ToString();

        await _dbContext.SaveChangesAsync();
        return invitation;
    }

    private static bool CanTransitionTo(string currentStatus, string targetStatus)
    {
        return (currentStatus, targetStatus) switch
        {
            (var s, var t) when s == InterviewInvitationStatus.PendingConfirmation.ToString() &&
                               t == InterviewInvitationStatus.Confirmed.ToString() => true,
            (var s, var t) when s == InterviewInvitationStatus.PendingConfirmation.ToString() &&
                               t == InterviewInvitationStatus.Refused.ToString() => true,
            (var s, var t) when s == InterviewInvitationStatus.PendingConfirmation.ToString() &&
                               t == InterviewInvitationStatus.Cancelled.ToString() => true,
            (var s, var t) when s == InterviewInvitationStatus.Confirmed.ToString() &&
                               t == InterviewInvitationStatus.Cancelled.ToString() => true,
            _ => false
        };
    }

    private async Task<InterviewInvitationResponse?> GetResponseAsync(int id)
    {
        var invitation = await GetByIdAsync(id);
        if (invitation == null) return null;

        return new InterviewInvitationResponse
        {
            Id = invitation.Id,
            CandidateName = invitation.Candidate?.Name ?? string.Empty,
            CandidatePhone = invitation.Candidate?.Phone,
            CandidateEmail = invitation.Candidate?.Email,
            JobPostTitle = invitation.JobPost?.Title ?? string.Empty,
            InvitedByUserName = invitation.InvitedByUser?.Username ?? string.Empty,
            InterviewTime = invitation.InterviewTime,
            TimeSlotStart = invitation.TimeSlotStart,
            TimeSlotEnd = invitation.TimeSlotEnd,
            Status = invitation.Status,
            CreatedAt = invitation.CreatedAt,
            ConfirmedAt = invitation.ConfirmedAt,
            RefusedAt = invitation.RefusedAt,
            Notes = invitation.Notes,
            InviteToken = invitation.InviteToken
        };
    }
}
