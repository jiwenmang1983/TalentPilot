namespace TalentPilot.Api.Models.DTOs;

public class CreateJobPostRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? Experience { get; set; }
    public string? Education { get; set; }
    public string Status { get; set; } = "Draft";
    public string? InterviewQuestions { get; set; }
    public int? InterviewDuration { get; set; }
}

public class UpdateJobPostRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? Experience { get; set; }
    public string? Education { get; set; }
    public string Status { get; set; } = "Draft";
    public string? InterviewQuestions { get; set; }
    public int? InterviewDuration { get; set; }
}

public class JobPostResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? Experience { get; set; }
    public string? Education { get; set; }
    public string Status { get; set; } = "Draft";
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal? MatchThreshold { get; set; }
    public string? MatchWeights { get; set; }
    public string? InterviewQuestions { get; set; }
    public int InterviewDuration { get; set; } = 20;
}

public class UpdateJobPostStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
