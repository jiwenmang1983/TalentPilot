using System.ComponentModel.DataAnnotations;

namespace TalentPilot.Api.Models.DTOs;

public class ChannelCredentialDto
{
    public int Id { get; set; }
    public string ChannelType { get; set; }
    public string ChannelName { get; set; }
    public string AccessType { get; set; }
    public string? MaskedCredentials { get; set; }  // e.g. "***apiKey"
    public string? CustomUrl { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateChannelCredentialRequest
{
    [Required]
    public string ChannelType { get; set; }

    [Required]
    public string ChannelName { get; set; }

    [Required]
    public string AccessType { get; set; }

    public string? Credentials { get; set; }  // plain JSON, will be encrypted

    public string? CustomUrl { get; set; }

    public bool IsEnabled { get; set; } = true;
}

public class UpdateChannelCredentialRequest
{
    public string? ChannelName { get; set; }
    public string? AccessType { get; set; }
    public string? Credentials { get; set; }
    public string? CustomUrl { get; set; }
    public bool? IsEnabled { get; set; }
}

public class ValidateChannelCredentialRequest
{
    public string? TestPayload { get; set; }
}
