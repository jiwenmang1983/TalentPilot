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
}

public class UpdateJobPostStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
