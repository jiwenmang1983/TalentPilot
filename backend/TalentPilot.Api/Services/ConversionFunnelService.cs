using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public interface IConversionFunnelService
{
    Task<(List<ConversionFunnelItemDto> Items, int Total)> GetAllAsync(ConversionFunnelQueryRequest query);
    Task<List<ConversionFunnelSummaryDto>> GetSummaryAsync(DateTime? dateFrom, DateTime? dateTo);
    Task<FunnelChartDto> GetFunnelChartDataAsync(int? jobPostId, DateTime? dateFrom, DateTime? dateTo);
    Task SeedDemoDataAsync();
}

public class ConversionFunnelService : IConversionFunnelService
{
    private readonly TalentPilotDbContext _dbContext;

    private static readonly string[] Stages = { "Posted", "Applied", "Matched", "Interviewed", "Hired" };

    public ConversionFunnelService(TalentPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(List<ConversionFunnelItemDto> Items, int Total)> GetAllAsync(ConversionFunnelQueryRequest query)
    {
        var queryable = _dbContext.ConversionFunnels
            .Include(f => f.JobPost)
            .AsQueryable();

        if (query.JobPostId.HasValue)
        {
            queryable = queryable.Where(f => f.JobPostId == query.JobPostId.Value);
        }

        if (query.DateFrom.HasValue)
        {
            queryable = queryable.Where(f => f.PeriodStart >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            queryable = queryable.Where(f => f.PeriodEnd <= query.DateTo.Value);
        }

        var total = await queryable.CountAsync();

        var items = await queryable
            .OrderByDescending(f => f.PeriodStart)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(f => new ConversionFunnelItemDto(
                f.Id,
                f.JobPostId,
                f.JobPost != null ? f.JobPost.Title : "",
                f.Stage,
                f.Count,
                f.ConversionRate,
                f.AvgTimeSpent,
                f.PeriodStart,
                f.PeriodEnd,
                f.CreatedAt))
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<ConversionFunnelSummaryDto>> GetSummaryAsync(DateTime? dateFrom, DateTime? dateTo)
    {
        var queryable = _dbContext.ConversionFunnels.AsQueryable();

        if (dateFrom.HasValue)
        {
            queryable = queryable.Where(f => f.PeriodStart >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            queryable = queryable.Where(f => f.PeriodEnd <= dateTo.Value);
        }

        var summary = await queryable
            .GroupBy(f => f.Stage)
            .Select(g => new ConversionFunnelSummaryDto(
                g.Key,
                g.Sum(x => x.Count),
                g.Average(x => x.ConversionRate),
                g.Average(x => x.AvgTimeSpent)))
            .ToListAsync();

        return summary;
    }

    public async Task<FunnelChartDto> GetFunnelChartDataAsync(int? jobPostId, DateTime? dateFrom, DateTime? dateTo)
    {
        var queryable = _dbContext.ConversionFunnels.AsQueryable();

        if (jobPostId.HasValue)
        {
            queryable = queryable.Where(f => f.JobPostId == jobPostId.Value);
        }

        if (dateFrom.HasValue)
        {
            queryable = queryable.Where(f => f.PeriodStart >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            queryable = queryable.Where(f => f.PeriodEnd <= dateTo.Value);
        }

        var funnelData = await queryable
            .GroupBy(f => f.Stage)
            .Select(g => new { Stage = g.Key, Count = g.Sum(x => x.Count) })
            .ToListAsync();

        var stages = new List<string>();
        var counts = new List<int>();
        var rates = new List<decimal>();

        foreach (var stage in Stages)
        {
            var data = funnelData.FirstOrDefault(f => f.Stage == stage);
            stages.Add(stage);
            counts.Add(data?.Count ?? 0);
            rates.Add(data?.Count > 0 ? Math.Round(100m * data.Count / funnelData.First(f => f.Stage == "Posted").Count, 2) : 0);
        }

        return new FunnelChartDto(stages, counts, rates);
    }

    public async Task SeedDemoDataAsync()
    {
        var existingCount = await _dbContext.ConversionFunnels.CountAsync();
        if (existingCount > 0) return;

        var jobPosts = await _dbContext.JobPosts.ToListAsync();
        if (jobPosts.Count == 0)
        {
            jobPosts = Enumerable.Range(1, 10).Select(i => new JobPost
            {
                Id = i,
                Title = $"Demo Job Position {i}",
                Status = "Published",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                IsDeleted = false
            }).ToList();
            _dbContext.JobPosts.AddRange(jobPosts);
            await _dbContext.SaveChangesAsync();
        }

        var rand = new Random(42);
        var now = DateTime.UtcNow;
        var startDate = now.AddDays(-90);

        foreach (var job in jobPosts)
        {
            var baseCount = rand.Next(500, 1500);
            var counts = new[] {
                baseCount,
                (int)(baseCount * rand.NextDouble() * 0.4 + 0.2),
                (int)(baseCount * rand.NextDouble() * 0.15 + 0.08),
                (int)(baseCount * rand.NextDouble() * 0.06 + 0.03),
                (int)(baseCount * rand.NextDouble() * 0.03 + 0.01)
            };

            for (int i = 0; i < Stages.Length; i++)
            {
                var conversionRate = i == 0 ? 100m : Math.Round(100m * counts[i] / counts[0], 2);
                var avgDays = i switch
                {
                    0 => rand.Next(1, 7),
                    1 => rand.Next(3, 14),
                    2 => rand.Next(5, 21),
                    3 => rand.Next(7, 30),
                    4 => rand.Next(14, 60),
                    _ => 0
                };

                var funnel = new ConversionFunnel
                {
                    JobPostId = job.Id,
                    PeriodStart = startDate.AddDays(rand.Next(0, 30)),
                    PeriodEnd = startDate.AddDays(rand.Next(30, 90)),
                    Stage = Stages[i],
                    Count = counts[i],
                    ConversionRate = conversionRate,
                    AvgTimeSpent = avgDays,
                    CreatedAt = now
                };

                _dbContext.ConversionFunnels.Add(funnel);
            }
        }

        await _dbContext.SaveChangesAsync();
    }
}