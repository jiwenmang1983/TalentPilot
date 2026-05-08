using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public interface IMatchingService
{
    Task<MatchResult?> CalculateMatchAsync(int resumeId, int jobPostId);
    Task<List<MatchResult>> BatchMatchAsync(int jobPostId);
    Task<(List<MatchResult> Items, int Total)> GetMatchResultsAsync(int? jobPostId, int page, int pageSize);
    Task<MatchResult?> UpdateMatchStatusAsync(int matchId, string status);
}

public class MatchingService : IMatchingService
{
    private readonly TalentPilotDbContext _dbContext;

    public MatchingService(TalentPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MatchResult?> CalculateMatchAsync(int resumeId, int jobPostId)
    {
        var resume = await _dbContext.Resumes.FindAsync(resumeId);
        var jobPost = await _dbContext.JobPosts.FindAsync(jobPostId);

        if (resume == null || jobPost == null)
            return null;

        // Check if already matched
        var existing = await _dbContext.MatchResults
            .FirstOrDefaultAsync(m => m.ResumeId == resumeId && m.JobPostId == jobPostId);

        // Calculate match score based on skills
        var resumeSkills = ParseSkills(resume.CandidateId.HasValue
            ? (await _dbContext.Candidates.FindAsync(resume.CandidateId))?.Skills
            : null);

        var matchedSkills = new List<string>();
        var missingSkills = new List<string>();

        // Simple keyword matching simulation
        var jobKeywords = ExtractKeywords(jobPost.Requirements ?? "");
        foreach (var keyword in jobKeywords)
        {
            if (resumeSkills.Any(s => s.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                matchedSkills.Add(keyword);
            }
            else
            {
                missingSkills.Add(keyword);
            }
        }

        var score = jobKeywords.Count > 0
            ? (decimal)matchedSkills.Count / jobKeywords.Count * 100
            : 70; // Default score if no keywords

        if (existing != null)
        {
            existing.Score = Math.Round(score, 2);
            existing.MatchedSkills = JsonSerializer.Serialize(matchedSkills);
            existing.MissingSkills = JsonSerializer.Serialize(missingSkills);
            existing.Summary = GenerateSummary(resume.CandidateName ?? "候选人", matchedSkills, missingSkills, score);
            existing.CreatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        var result = new MatchResult
        {
            ResumeId = resumeId,
            JobPostId = jobPostId,
            Score = Math.Round(score, 2),
            MatchedSkills = JsonSerializer.Serialize(matchedSkills),
            MissingSkills = JsonSerializer.Serialize(missingSkills),
            Summary = GenerateSummary(resume.CandidateName ?? "候选人", matchedSkills, missingSkills, score),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.MatchResults.Add(result);
        await _dbContext.SaveChangesAsync();
        return result;
    }

    public async Task<List<MatchResult>> BatchMatchAsync(int jobPostId)
    {
        var jobPost = await _dbContext.JobPosts.FindAsync(jobPostId);
        if (jobPost == null) return new List<MatchResult>();

        var resumes = await _dbContext.Resumes
            .Where(r => r.ParsedStatus == "Success" && r.CandidateId.HasValue)
            .ToListAsync();

        var results = new List<MatchResult>();
        foreach (var resume in resumes)
        {
            var result = await CalculateMatchAsync(resume.Id, jobPostId);
            if (result != null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    public async Task<(List<MatchResult> Items, int Total)> GetMatchResultsAsync(int? jobPostId, int page, int pageSize)
    {
        var query = _dbContext.MatchResults.AsQueryable();

        if (jobPostId.HasValue)
        {
            query = query.Where(m => m.JobPostId == jobPostId.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.Score)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<MatchResult?> UpdateMatchStatusAsync(int matchId, string status)
    {
        var match = await _dbContext.MatchResults.FindAsync(matchId);
        if (match == null) return null;

        match.Status = status;
        await _dbContext.SaveChangesAsync();
        return match;
    }

    private static List<string> ParseSkills(string? skillsJson)
    {
        if (string.IsNullOrEmpty(skillsJson)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(skillsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static List<string> ExtractKeywords(string text)
    {
        if (string.IsNullOrEmpty(text)) return new List<string>();

        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Java", "Spring", "MySQL", "Redis", "Docker", "Kubernetes",
            "Vue", "React", "Angular", "TypeScript", "Node.js",
            "Python", "Django", "Flask", "Machine Learning", "TensorFlow",
            "Go", "gRPC", "Microservices", "AWS", "Azure",
            "Product Management", "Agile", "Scrum", "Data Analysis"
        };

        return keywords.Where(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private static string GenerateSummary(string candidateName, List<string> matched, List<string> missing, decimal score)
    {
        return $"{candidateName}与职位匹配度{Math.Round(score, 0)}%，" +
               $"匹配技能：{string.Join(", ", matched.Take(3))}，" +
               $"缺失技能：{string.Join(", ", missing.Take(3))}";
    }
}
