using System.ComponentModel.DataAnnotations;

namespace TalentPilot.Api.Models.Entities;

public class JobChannelContentDto
{
    public int Id { get; set; }
    public int JobPostId { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string? AdaptedTitle { get; set; }
    public string? AdaptedContent { get; set; }
    public int WordCount { get; set; }
    public List<string> SkillTags { get; set; } = new();
    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AdaptContentRequest
{
    [Required]
    public int JobPostId { get; set; }

    [Required]
    public List<string> ChannelTypes { get; set; } = new();
}

public class CreateChannelContentRequest
{
    [Required]
    public int JobPostId { get; set; }

    [Required]
    [MaxLength(20)]
    public string ChannelType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ChannelName { get; set; } = string.Empty;

    public string? AdaptedTitle { get; set; }
    public string? AdaptedContent { get; set; }
    public int? WordCount { get; set; }
    public List<string>? SkillTags { get; set; }
    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
}

public class UpdateChannelContentRequest
{
    public string? AdaptedTitle { get; set; }
    public string? AdaptedContent { get; set; }
    public int? WordCount { get; set; }
    public List<string>? SkillTags { get; set; }
    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
    public string? Status { get; set; }
}

// Alias for backwards compatibility
public class UpdateContentRequest : UpdateChannelContentRequest { }

public class AdaptContentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<JobChannelContentDto> Results { get; set; } = new();
}
