using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public interface IResumeCollectionService
{
    Task<(List<Resume> Items, int Total)> ListResumesAsync(string? source, int page, int pageSize);
    Task<Resume?> GetResumeByIdAsync(int id);
    Task<Resume> CreateResumeAsync(string? candidateName, string? phone, string? email, string? source, string? rawFilePath, int? sourceJobPostId);
    Task<List<Resume>> MockCollectResumesAsync(string channel, int count = 5);
    Task<bool> UpdateResumeCandidateIdAsync(int resumeId, int candidateId);
    Task<(List<object> Items, int Total)> ListResumesWithMatchAsync(int? jobPostId, string? channel, decimal? minScore, decimal? maxScore, int page, int pageSize);
    Task CollectAllActiveChannelsAsync();
    Task TriggerImmediateCollectionAsync(int? jobPostId);
    Task<List<ResumeSource>> GetSourcesAsync();
}

public class ResumeCollectionService : IResumeCollectionService
{
    private readonly TalentPilotDbContext _dbContext;
    private readonly ILogger<ResumeCollectionService> _logger;

    public ResumeCollectionService(TalentPilotDbContext dbContext, ILogger<ResumeCollectionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(List<Resume> Items, int Total)> ListResumesAsync(string? source, int page, int pageSize)
    {
        var query = _dbContext.Resumes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(r => r.Source == source);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Resume?> GetResumeByIdAsync(int id)
    {
        return await _dbContext.Resumes.FindAsync(id);
    }

    public async Task<Resume> CreateResumeAsync(string? candidateName, string? phone, string? email, string? source, string? rawFilePath, int? sourceJobPostId)
    {
        var resume = new Resume
        {
            CandidateName = candidateName,
            Phone = phone,
            Email = email,
            Source = source ?? "Manual",
            RawFilePath = rawFilePath,
            SourceJobPostId = sourceJobPostId,
            ParsedStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Resumes.Add(resume);
        await _dbContext.SaveChangesAsync();

        return resume;
    }

    public async Task<List<Resume>> MockCollectResumesAsync(string channel, int count = 5)
    {
        var mockData = GenerateMockResumes(channel, count);
        _dbContext.Resumes.AddRange(mockData);
        await _dbContext.SaveChangesAsync();

        // Update sync time for the source
        var resumeSource = await _dbContext.ResumeSources.FirstOrDefaultAsync(s => s.Channel == channel);
        if (resumeSource != null)
        {
            resumeSource.LastSyncAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.ResumeSources.Add(new ResumeSource
            {
                Channel = channel,
                IsActive = true,
                LastSyncAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();

        return mockData;
    }

    public async Task<bool> UpdateResumeCandidateIdAsync(int resumeId, int candidateId)
    {
        var resume = await _dbContext.Resumes.FindAsync(resumeId);
        if (resume == null) return false;

        resume.CandidateId = candidateId;
        resume.ParsedStatus = "Success";
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<(List<object> Items, int Total)> ListResumesWithMatchAsync(int? jobPostId, string? channel, decimal? minScore, decimal? maxScore, int page, int pageSize)
    {
        var query = from r in _dbContext.Resumes
                    join m in _dbContext.MatchResults on r.Id equals m.ResumeId into matchGroup
                    from m in matchGroup.DefaultIfEmpty()
                    where (string.IsNullOrEmpty(channel) || r.Source == channel)
                          && (!jobPostId.HasValue || m.JobPostId == jobPostId.Value)
                          && (!minScore.HasValue || (m != null && m.Score >= minScore.Value))
                          && (!maxScore.HasValue || (m != null && m.Score <= maxScore.Value))
                    select new
                    {
                        Resume = r,
                        Match = m
                    };

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Match != null ? x.Match.Score : 0)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => (object)new
            {
                x.Resume.Id,
                x.Resume.CandidateName,
                x.Resume.Phone,
                x.Resume.Email,
                x.Resume.Source,
                x.Resume.ParsedStatus,
                x.Resume.CandidateId,
                x.Resume.CreatedAt,
                JobPostId = x.Match != null ? x.Match.JobPostId : (int?)null,
                matchScore = x.Match != null ? x.Match.Score : (decimal?)null,
                matchStatus = x.Match != null ? x.Match.Status : (string?)null,
                matchThreshold = x.Match != null ? x.Match.MatchThreshold : (decimal?)null,
                dimensionScores = x.Match != null ? x.Match.DimensionScores : (string?)null
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task CollectAllActiveChannelsAsync()
    {
        var activeSources = await _dbContext.ResumeSources
            .Where(s => s.IsActive)
            .ToListAsync();

        foreach (var source in activeSources)
        {
            await CollectChannelAsync(source.Channel);
        }
    }

    public async Task CollectChannelAsync(string channel)
    {
        // Simulate collection from channel (mock implementation)
        _logger.LogInformation("开始采集渠道: {Channel}", channel);

        // Update last sync time
        var resumeSource = await _dbContext.ResumeSources.FirstOrDefaultAsync(s => s.Channel == channel);
        if (resumeSource != null)
        {
            resumeSource.LastSyncAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.ResumeSources.Add(new ResumeSource
            {
                Channel = channel,
                IsActive = true,
                LastSyncAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("渠道 {Channel} 采集完成", channel);
    }

    public async Task TriggerImmediateCollectionAsync(int? jobPostId)
    {
        _logger.LogInformation("触发立即采集任务, JobPostId: {JobPostId}", jobPostId);

        if (jobPostId.HasValue)
        {
            // Collect for specific job post
            _logger.LogInformation("执行指定职位采集: {JobPostId}", jobPostId.Value);
        }
        else
        {
            // Collect from all active channels
            await CollectAllActiveChannelsAsync();
        }
    }

    public async Task<List<ResumeSource>> GetSourcesAsync()
    {
        return await _dbContext.ResumeSources
            .OrderBy(s => s.Channel)
            .ToListAsync();
    }

    private static List<Resume> GenerateMockResumes(string channel, int count)
    {
        var random = new Random();
        var firstNames = new[] { "张", "李", "王", "刘", "陈", "杨", "赵", "黄", "周", "吴" };
        var lastNames = new[] { "伟", "芳", "娜", "敏", "静", "丽", "强", "磊", "军", "洋" };
        var positions = new[] { "Java开发工程师", "前端开发工程师", "产品经理", "数据分析师", "UI设计师" };
        var companies = new[] { "字节跳动", "阿里巴巴", "腾讯", "百度", "美团", "京东", "拼多多" };

        var resumes = new List<Resume>();
        for (int i = 0; i < count; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            resumes.Add(new Resume
            {
                CandidateName = firstName + lastName,
                Phone = $"138{random.Next(10000000, 99999999)}",
                Email = $"{firstName.ToLower()}{lastName.ToLower()}{random.Next(1, 99)}@example.com",
                Source = channel,
                ParsedStatus = "Pending",
                CreatedAt = DateTime.UtcNow.AddHours(-random.Next(1, 48))
            });
        }

        return resumes;
    }
}
