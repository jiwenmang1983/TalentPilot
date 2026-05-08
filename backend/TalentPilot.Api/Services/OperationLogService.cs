using TalentPilot.Api.Data;
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
}
