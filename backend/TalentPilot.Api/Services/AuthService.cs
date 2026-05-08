using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public class AuthService
{
    private readonly TalentPilotDbContext _dbContext;
    private readonly JwtService _jwtService;
    private readonly OperationLogService _logService;
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 30;

    public AuthService(TalentPilotDbContext dbContext, JwtService jwtService, OperationLogService logService)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _logService = logService;
    }

    public async Task<(bool Success, string Message, LoginResponse? Response)> Login(LoginRequest request, HttpContext httpContext)
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

        var user = await _dbContext.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        var userIdForQuery = user == null ? 0 : user.Id;
        var failedAttempts = await _dbContext.UserLoginAttempts
            .Where(a => a.UserId == userIdForQuery && a.AttemptResult == "FAILED")
            .Where(a => a.CreatedAt > DateTime.UtcNow.AddMinutes(-LockoutMinutes))
            .CountAsync();

        if (failedAttempts >= MaxFailedAttempts)
        {
            await RecordLoginAttempt(user?.Id, ipAddress, "FAILED", "LOCKED");
            return (false, "账号已锁定，请30分钟后再试", null);
        }

        if (user == null)
        {
            await RecordLoginAttempt(null, ipAddress, "FAILED", "USER_NOT_FOUND");
            return (false, "用户名或密码错误", null);
        }

        if (!user.IsActive)
        {
            await RecordLoginAttempt(user.Id, ipAddress, "FAILED", "INACTIVE");
            return (false, "账号已被禁用", null);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await RecordLoginAttempt(user.Id, ipAddress, "FAILED", "WRONG_PASSWORD");
            var remainingAttempts = MaxFailedAttempts - failedAttempts - 1;
            if (remainingAttempts > 0)
            {
                return (false, $"用户名或密码错误，剩余{remainingAttempts}次尝试机会", null);
            }
            return (false, "用户名或密码错误", null);
        }

        await RecordLoginAttempt(user.Id, ipAddress, "SUCCESS", null);

        var (accessToken, refreshToken, expiresAt) = _jwtService.GenerateTokens(
            user.Id, user.Username, user.Role?.RoleKey ?? "", user.DepartmentId);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtService.GetRefreshTokenExpirationDays());
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;
        await _dbContext.SaveChangesAsync();

        var response = new LoginResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FullName ?? "",
            user.Role?.RoleKey ?? "",
            accessToken,
            refreshToken,
            expiresAt
        );

        return (true, "登录成功", response);
    }

    public async Task<(bool Success, string Message, LoginResponse? Response)> Refresh(RefreshTokenRequest request)
    {
        var user = await _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user == null)
        {
            return (false, "无效的刷新令牌", null);
        }

        if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            return (false, "刷新令牌已过期，请重新登录", null);
        }

        var (accessToken, _, expiresAt) = _jwtService.GenerateTokens(
            user.Id, user.Username, user.Role?.RoleKey ?? "", user.DepartmentId);

        var response = new LoginResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FullName ?? "",
            user.Role?.RoleKey ?? "",
            accessToken,
            user.RefreshToken!,
            expiresAt
        );

        return (true, "刷新成功", response);
    }

    public async Task Logout(long userId, HttpContext httpContext)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _dbContext.SaveChangesAsync();
        }

        await _logService.RecordLog(userId, "LOGOUT", "User", userId, "用户登出", httpContext);
    }

    public async Task<User?> GetCurrentUser(long userId)
    {
        return await _dbContext.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    private async Task RecordLoginAttempt(long? userId, string? ipAddress, string result, string? failedReason)
    {
        var attempt = new UserLoginAttempt
        {
            UserId = userId,
            IpAddress = ipAddress,
            AttemptResult = result,
            FailedReason = failedReason,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserLoginAttempts.Add(attempt);
        await _dbContext.SaveChangesAsync();
    }
}
