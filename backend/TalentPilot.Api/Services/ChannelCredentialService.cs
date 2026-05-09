using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Models.DTOs;

namespace TalentPilot.Api.Services;

public class ChannelCredentialService
{
    private readonly TalentPilotDbContext _db;
    private readonly IConfiguration _config;
    private static readonly string[] ValidChannelTypes = { "liepin", "lagou", "boss", "linkedin", "xiaohongshu", "custom" };
    private static readonly string[] ValidAccessTypes = { "api_key", "browser_auto", "custom_url" };

    public ChannelCredentialService(TalentPilotDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<List<ChannelCredentialDto>> GetAllAsync()
    {
        var creds = await _db.ChannelCredentials
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Id)
            .ToListAsync();

        return creds.Select(MapToDto).ToList();
    }

    public async Task<ChannelCredentialDto?> GetByChannelTypeAsync(string channelType)
    {
        var cred = await _db.ChannelCredentials
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ChannelType == channelType && !c.IsDeleted);

        return cred == null ? null : MapToDto(cred);
    }

    public async Task<(ChannelCredential? entity, string? error)> CreateAsync(CreateChannelCredentialRequest req, string createdBy)
    {
        // Validate channel type
        if (!ValidChannelTypes.Contains(req.ChannelType))
            return (null, $"无效的渠道类型。可选: {string.Join(", ", ValidChannelTypes)}");

        // Validate access type
        if (!ValidAccessTypes.Contains(req.AccessType))
            return (null, $"无效的接入方式。可选: {string.Join(", ", ValidAccessTypes)}");

        // Check duplicate
        var existing = await _db.ChannelCredentials
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ChannelType == req.ChannelType && !c.IsDeleted);

        if (existing != null)
            return (null, $"渠道 [{req.ChannelType}] 已存在，请使用更新接口");

        var cred = new ChannelCredential
        {
            ChannelType = req.ChannelType,
            ChannelName = req.ChannelName,
            AccessType = req.AccessType,
            Credentials = Encrypt(req.Credentials ?? "{}"),
            CustomUrl = req.CustomUrl,
            IsEnabled = req.IsEnabled,
            CreatedBy = createdBy,
            CreatedAt = DateTime.Now
        };

        _db.ChannelCredentials.Add(cred);
        await _db.SaveChangesAsync();

        return (cred, null);
    }

    public async Task<(ChannelCredential? entity, string? error)> UpdateAsync(string channelType, UpdateChannelCredentialRequest req)
    {
        var cred = await _db.ChannelCredentials
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ChannelType == channelType && !c.IsDeleted);

        if (cred == null)
            return (null, "渠道凭证不存在");

        if (req.ChannelName != null) cred.ChannelName = req.ChannelName;
        if (req.AccessType != null)
        {
            if (!ValidAccessTypes.Contains(req.AccessType))
                return (null, $"无效的接入方式。可选: {string.Join(", ", ValidAccessTypes)}");
            cred.AccessType = req.AccessType;
        }
        if (req.Credentials != null) cred.Credentials = Encrypt(req.Credentials);
        if (req.CustomUrl != null) cred.CustomUrl = req.CustomUrl;
        if (req.IsEnabled.HasValue) cred.IsEnabled = req.IsEnabled.Value;
        cred.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (cred, null);
    }

    public async Task<bool> DeleteAsync(string channelType)
    {
        var cred = await _db.ChannelCredentials
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ChannelType == channelType && !c.IsDeleted);

        if (cred == null) return false;

        cred.IsDeleted = true;
        cred.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool success, string message)> ValidateAsync(string channelType)
    {
        var cred = await _db.ChannelCredentials
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ChannelType == channelType && !c.IsDeleted);

        if (cred == null)
            return (false, "渠道凭证不存在");

        if (!cred.IsEnabled)
            return (false, "渠道未启用");

        var decrypted = Decrypt(cred.Credentials ?? "{}");
        var credJson = JsonDocument.Parse(decrypted);

        // Mock validation - in production, call actual channel API
        // For now, just check if the credentials are well-formed JSON
        switch (channelType)
        {
            case "liepin":
            case "lagou":
                var hasApiKey = credJson.RootElement.TryGetProperty("apiKey", out var apiKey) && !string.IsNullOrEmpty(apiKey.GetString());
                var hasApiSecret = credJson.RootElement.TryGetProperty("apiSecret", out var apiSecret) && !string.IsNullOrEmpty(apiSecret.GetString());
                if (!hasApiKey || !hasApiSecret)
                    return (false, "API Key 或 API Secret 为空");
                break;

            case "boss":
            case "linkedin":
            case "xiaohongshu":
                var hasCookie = credJson.RootElement.TryGetProperty("cookie", out var cookie) && !string.IsNullOrEmpty(cookie.GetString());
                if (!hasCookie)
                    return (false, "Cookie 为空");
                break;

            case "custom":
                var hasUrl = credJson.RootElement.TryGetProperty("url", out var url) && !string.IsNullOrEmpty(url.GetString());
                if (!hasUrl)
                    return (false, "URL 为空");
                break;
        }

        return (true, "连接成功");
    }

    // Get raw decrypted credentials for internal use (e.g., JobDistributionAgent)
    public async Task<string?> GetDecryptedCredentialsAsync(string channelType)
    {
        var cred = await _db.ChannelCredentials
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ChannelType == channelType && !c.IsDeleted && c.IsEnabled);

        if (cred == null) return null;
        return Decrypt(cred.Credentials ?? "{}");
    }

    private ChannelCredentialDto MapToDto(ChannelCredential cred)
    {
        var masked = MaskCredentials(cred.Credentials ?? "{}", cred.AccessType);
        return new ChannelCredentialDto
        {
            Id = cred.Id,
            ChannelType = cred.ChannelType,
            ChannelName = cred.ChannelName,
            AccessType = cred.AccessType,
            MaskedCredentials = masked,
            CustomUrl = cred.CustomUrl,
            IsEnabled = cred.IsEnabled,
            CreatedAt = cred.CreatedAt
        };
    }

    private static string MaskCredentials(string encryptedJson, string accessType)
    {
        try
        {
            // Try to parse as JSON even if encrypted (for display purposes)
            var json = encryptedJson; // May be AES-encrypted, skip masking for now
            return $"*** [{accessType}]";
        }
        catch
        {
            return "*** [已加密]";
        }
    }

    private string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";

        var key = _config["Encryption:AesKey"] ?? "TalentPilot2026AESKey!@#$%^&*";
        var iv = _config["Encryption:AesIv"] ?? "TalentPilotAESIV!";

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        aes.IV = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }

    private string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return "{}";

        try
        {
            var key = _config["Encryption:AesKey"] ?? "TalentPilot2026AESKey!@#$%^&*";
            var iv = _config["Encryption:AesIv"] ?? "TalentPilotAESIV!";

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.IV = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var cipherBytes = Convert.FromBase64String(cipherText);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return "{}"; // Return empty JSON on decryption failure
        }
    }
}
