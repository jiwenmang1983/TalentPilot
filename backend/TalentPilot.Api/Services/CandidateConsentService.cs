using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public class CandidateConsentService
{
    private readonly TalentPilotDbContext _dbContext;

    public CandidateConsentService(TalentPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CandidateConsent?> GetConsentRecord(long candidateId)
    {
        return await _dbContext.CandidateConsents
            .Where(c => c.CandidateId == candidateId && c.IsActive)
            .OrderByDescending(c => c.ConsentDate)
            .FirstOrDefaultAsync();
    }

    public async Task<CandidateConsent> RecordConsent(long candidateId, string consentType, string? ipAddress)
    {
        var consent = new CandidateConsent
        {
            CandidateId = candidateId,
            ConsentType = consentType,
            IpAddress = ipAddress,
            ConsentDate = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.CandidateConsents.Add(consent);
        await _dbContext.SaveChangesAsync();
        return consent;
    }

    public async Task<bool> RevokeConsent(long candidateId)
    {
        var activeConsent = await _dbContext.CandidateConsents
            .Where(c => c.CandidateId == candidateId && c.IsActive)
            .ToListAsync();

        if (activeConsent.Count == 0)
        {
            return false;
        }

        foreach (var consent in activeConsent)
        {
            consent.IsActive = false;
            consent.RevokedDate = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasActiveConsent(long candidateId)
    {
        return await _dbContext.CandidateConsents
            .AnyAsync(c => c.CandidateId == candidateId && c.IsActive);
    }
}