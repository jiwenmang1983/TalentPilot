using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Models.DTOs;

namespace TalentPilot.Api.Services;

public interface IJobPostService
{
    Task<(List<JobPost> Items, int Total)> GetAllAsync(string? status, int page, int pageSize);
    Task<JobPost?> GetByIdAsync(int id);
    Task<JobPost> CreateAsync(CreateJobPostRequest request, string createdBy);
    Task<JobPost?> UpdateAsync(int id, UpdateJobPostRequest request);
    Task<JobPost?> UpdateStatusAsync(int id, string status);
    Task<JobPost?> UpdateMatchThresholdAsync(int id, decimal threshold);
    Task<JobPost?> UpdateMatchWeightsAsync(int id, string weights);
    Task<bool> DeleteAsync(int id);
}

public class JobPostService : IJobPostService
{
    private readonly TalentPilotDbContext _dbContext;

    public JobPostService(TalentPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(List<JobPost> Items, int Total)> GetAllAsync(string? status, int page, int pageSize)
    {
        var query = _dbContext.JobPosts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(j => j.Status == status);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<JobPost?> GetByIdAsync(int id)
    {
        return await _dbContext.JobPosts.FindAsync(id);
    }

    public async Task<JobPost> CreateAsync(CreateJobPostRequest request, string createdBy)
    {
        var jobPost = new JobPost
        {
            Title = request.Title,
            Department = request.Department,
            Description = request.Description,
            Requirements = request.Requirements,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            Experience = request.Experience,
            Education = request.Education,
            Status = request.Status,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
            InterviewQuestions = request.InterviewQuestions,
            InterviewDuration = request.InterviewDuration ?? 20
        };

        _dbContext.JobPosts.Add(jobPost);
        await _dbContext.SaveChangesAsync();

        return jobPost;
    }

    public async Task<JobPost?> UpdateAsync(int id, UpdateJobPostRequest request)
    {
        var jobPost = await _dbContext.JobPosts.FindAsync(id);
        if (jobPost == null) return null;

        jobPost.Title = request.Title;
        jobPost.Department = request.Department;
        jobPost.Description = request.Description;
        jobPost.Requirements = request.Requirements;
        jobPost.SalaryMin = request.SalaryMin;
        jobPost.SalaryMax = request.SalaryMax;
        jobPost.Experience = request.Experience;
        jobPost.Education = request.Education;
        jobPost.Status = request.Status;
        if (request.InterviewQuestions != null)
            jobPost.InterviewQuestions = request.InterviewQuestions;
        if (request.InterviewDuration.HasValue)
            jobPost.InterviewDuration = request.InterviewDuration.Value;
        jobPost.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return jobPost;
    }

    public async Task<JobPost?> UpdateStatusAsync(int id, string status)
    {
        var jobPost = await _dbContext.JobPosts.FindAsync(id);
        if (jobPost == null) return null;

        jobPost.Status = status;
        jobPost.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return jobPost;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var jobPost = await _dbContext.JobPosts.FindAsync(id);
        if (jobPost == null) return false;

        jobPost.IsDeleted = true;
        jobPost.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<JobPost?> UpdateMatchThresholdAsync(int id, decimal threshold)
    {
        var jobPost = await _dbContext.JobPosts.FindAsync(id);
        if (jobPost == null) return null;

        jobPost.MatchThreshold = threshold;
        jobPost.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return jobPost;
    }

    public async Task<JobPost?> UpdateMatchWeightsAsync(int id, string weights)
    {
        var jobPost = await _dbContext.JobPosts.FindAsync(id);
        if (jobPost == null) return null;

        jobPost.MatchWeights = weights;
        jobPost.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return jobPost;
    }
}
