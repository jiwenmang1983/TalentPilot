namespace TalentPilot.Api.Models.DTOs.Auth;

// 登录请求
public record LoginRequest(string Username, string Password);

// 登录响应
public record LoginResponse(long UserId, string Username, string Email, string FullName, string RoleKey, string AccessToken, string RefreshToken, DateTime ExpiresAt);

// 刷新Token请求
public record RefreshTokenRequest(string RefreshToken);

// 注册请求（供种子数据内部调用）
public record RegisterRequest(string Username, string Email, string Password, string FullName, long DepartmentId, long RoleId);

// API响应包装
public record ApiResponse<T>(bool Success, string Message, T? Data);

// 用户列表响应
public record UserListItem(long Id, string Username, string Email, string FullName, string? Phone, string? AvatarUrl,
    long DepartmentId, string DepartmentName, long RoleId, string RoleName, bool IsActive, DateTime? LastLoginAt, DateTime CreatedAt);

// 创建用户请求
public record CreateUserRequest(string Username, string Email, string Password, string? FullName, string? Phone, long DepartmentId, long RoleId);

// 更新用户请求
public record UpdateUserRequest(string? Email, string? FullName, string? Phone, long? DepartmentId, long? RoleId);

// 重置密码请求
public record ResetPasswordRequest(string NewPassword);
