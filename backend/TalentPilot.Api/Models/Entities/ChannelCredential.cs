using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("ChannelCredentials")]
public class ChannelCredential
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string ChannelType { get; set; }  // liepin | lagou | boss | linkedin | xiaohongshu | custom

    [Required]
    [MaxLength(50)]
    public string ChannelName { get; set; }

    [Required]
    [MaxLength(20)]
    public string AccessType { get; set; }  // api_key | browser_auto | custom_url

    public string? Credentials { get; set; }  // AES encrypted JSON

    [MaxLength(500)]
    public string? CustomUrl { get; set; }

    public bool IsEnabled { get; set; } = true;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
}
