using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public class OperationLogService
{
    private readonly TalentPilotDbContext _dbContext;

    public OperationLogService(TalentPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordLog(long userId, string action, string entityType, long entityId, string? detail, HttpContext httpContext)
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var log = new OperationLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.OperationLogs.Add(log);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<(List<OperationLog> Items, int Total)> QueryLogs(OperationLogQuery query)
    {
        var queryable = _dbContext.OperationLogs
            .Include(l => l.User)
            .AsQueryable();

        if (query.UserId.HasValue)
            queryable = queryable.Where(l => l.UserId == query.UserId.Value);

        if (!string.IsNullOrEmpty(query.Action))
            queryable = queryable.Where(l => l.Action.Contains(query.Action));

        if (!string.IsNullOrEmpty(query.EntityType))
            queryable = queryable.Where(l => l.EntityType.Contains(query.EntityType));

        if (query.StartDate.HasValue)
            queryable = queryable.Where(l => l.CreatedAt >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            queryable = queryable.Where(l => l.CreatedAt <= query.EndDate.Value);

        var total = await queryable.CountAsync();

        var items = await queryable
            .OrderByDescending(l => l.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<OperationLog?> GetLogById(long id)
    {
        return await _dbContext.OperationLogs
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == id);
    }
}
