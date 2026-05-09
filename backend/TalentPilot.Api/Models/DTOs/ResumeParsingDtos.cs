namespace TalentPilot.Api.Models.DTOs;

public class ResumeParseRequest
{
    public string ResumeText { get; set; } = string.Empty;
}

public class ResumeParseResponse
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public List<WorkExperienceItem> WorkExperience { get; set; } = new();
    public List<EducationItem> Education { get; set; } = new();
    public List<string> SkillTags { get; set; } = new();
    public string? Summary { get; set; }
    public int? TotalWorkYears { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public int MatchScore { get; set; }
    public int MinimaxTokens { get; set; }
}

public class WorkExperienceItem
{
    public string? Company { get; set; }
    public string? Position { get; set; }
    public string? Duration { get; set; }
    public string? Description { get; set; }
}

public class EducationItem
{
    public string? School { get; set; }
    public string? Degree { get; set; }
    public string? Major { get; set; }
    public string? GraduationYear { get; set; }
}