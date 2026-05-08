using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("ResumeParsedRecords")]
public class ResumeParsedRecord
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Column("ParsedResumeId")]
    public int ParsedResumeId { get; set; }

    [Column("CandidateId")]
    public long? CandidateId { get; set; }

    [Column("RawText")]
    public string? RawText { get; set; }

    [Column("ParsedJson")]
    public string? ParsedJson { get; set; }

    [Column("MinimaxiTokens")]
    public int MinimaxTokens { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
}