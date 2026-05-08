using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public interface IResumeParsingService
{
    Task<ParsedResumeResult?> ParseResumeAsync(int resumeId);
    Task<List<Resume>> GetResumesByCandidateIdAsync(int candidateId);
    Task<ResumeParseResponse?> ParseResumeTextAsync(string resumeText);
    Task<Candidate?> CreateCandidateFromParsedAsync(ResumeParseResponse parsed);
}

public class ParsedResumeResult
{
    public int ResumeId { get; set; }
    public string? CandidateName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int? WorkExperienceYears { get; set; }
    public string? WorkExperienceSummary { get; set; }
    public string? Education { get; set; }
    public string? SkillsJson { get; set; }
    public List<string> Skills { get; set; } = new();
    public decimal? ExpectedSalary { get; set; }
    public int MatchScore { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class ResumeParsingService : IResumeParsingService
{
    private readonly TalentPilotDbContext _dbContext;
    private readonly IMiniMaxService _miniMaxService;
    private readonly ILogger<ResumeParsingService> _logger;

    public ResumeParsingService(
        TalentPilotDbContext dbContext,
        IMiniMaxService miniMaxService,
        ILogger<ResumeParsingService> logger)
    {
        _dbContext = dbContext;
        _miniMaxService = miniMaxService;
        _logger = logger;
    }

    public async Task<ParsedResumeResult?> ParseResumeAsync(int resumeId)
    {
        var resume = await _dbContext.Resumes.FindAsync(resumeId);
        if (resume == null) return null;

        resume.ParsedStatus = "Parsing";
        await _dbContext.SaveChangesAsync();

        try
        {
            var result = await CallMiniMaxToParseAsync(resume);

            resume.ParsedStatus = "Success";

            var existingCandidate = await _dbContext.Candidates
                .FirstOrDefaultAsync(c => c.Email == resume.Email);

            if (existingCandidate != null)
            {
                UpdateCandidateFromParsed(existingCandidate, result);
                resume.CandidateId = (int)existingCandidate.Id;
            }
            else
            {
                var newCandidate = CreateCandidateFromParsed(resume, result);
                _dbContext.Candidates.Add(newCandidate);
                await _dbContext.SaveChangesAsync();
                resume.CandidateId = (int)newCandidate.Id;
            }

            await _dbContext.SaveChangesAsync();
            return result;
        }
        catch (Exception)
        {
            resume.ParsedStatus = "Failed";
            await _dbContext.SaveChangesAsync();
            throw;
        }
    }

    public async Task<ResumeParseResponse?> ParseResumeTextAsync(string resumeText)
    {
        var result = await CallMiniMaxForStructuredParseAsync(resumeText);

        if (result != null)
        {
            await SaveParsedRecordAsync(resumeText, result);
        }

        return result;
    }

    public async Task<List<Resume>> GetResumesByCandidateIdAsync(int candidateId)
    {
        return await _dbContext.Resumes
            .Where(r => r.CandidateId == candidateId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    private async Task<ParsedResumeResult> CallMiniMaxToParseAsync(Resume resume)
    {
        var resumeText = $"姓名: {resume.CandidateName ?? "未知"}\n电话: {resume.Phone ?? "未知"}\n邮箱: {resume.Email ?? "未知"}";
        return await Task.FromResult(MockParseResume(resume));
    }

    private async Task<ResumeParseResponse?> CallMiniMaxForStructuredParseAsync(string resumeText)
    {
        var prompt = $@"请从以下简历文本中提取结构化信息，并以JSON格式返回。必须严格按照以下JSON结构返回，不要包含任何其他内容：

{{
  ""name"": ""姓名"",
  ""phone"": ""手机号"",
  ""email"": ""邮箱"",
  ""workExperience"": [
    {{
      ""company"": ""公司名称"",
      ""position"": ""职位"",
      ""duration"": ""时间段"",
      ""description"": ""工作描述""
    }}
  ],
  ""education"": [
    {{
      ""school"": ""学校名称"",
      ""degree"": ""学历"",
      ""major"": ""专业"",
      ""graduationYear"": ""毕业年份""
    }}
  ],
  ""skillTags"": [""技能1"", ""技能2""],
  ""summary"": ""个人简介"",
  ""totalWorkYears"": 总工作年限数字
}}

简历文本如下：
{resumeText}

请直接返回JSON，不要有任何其他文字。";

        var response = await _miniMaxService.ChatAsync(prompt, 2048);

        if (response?.Choices == null || response.Choices.Count == 0)
        {
            _logger.LogError("MiniMax API returned no choices");
            return null;
        }

        var content = response.Choices[0].Messages?[0]?.Content;
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogError("MiniMax API returned empty content");
            return null;
        }

        // Extract JSON from the response
        var jsonMatch = Regex.Match(content, @"\{[\s\S]*\}");
        if (!jsonMatch.Success)
        {
            _logger.LogError("Failed to extract JSON from MiniMax response: {Content}", content);
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<ResumeParseResponse>(jsonMatch.Value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result != null && response.Usage != null)
            {
                result.MinimaxTokens = response.Usage.Tokens ?? 0;
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON from MiniMax response: {Content}", content);
            return null;
        }
    }

    private async Task SaveParsedRecordAsync(string rawText, ResumeParseResponse result)
    {
        var record = new ResumeParsedRecord
        {
            RawText = rawText,
            ParsedJson = JsonSerializer.Serialize(result),
            MinimaxTokens = result.MinimaxTokens,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ResumeParsedRecords.Add(record);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Candidate?> CreateCandidateFromParsedAsync(ResumeParseResponse parsed)
    {
        if (parsed == null || string.IsNullOrEmpty(parsed.Name))
            return null;

        var candidate = new Candidate
        {
            Name = parsed.Name,
            Email = parsed.Email,
            Phone = parsed.Phone,
            Skills = parsed.SkillTags != null ? JsonSerializer.Serialize(parsed.SkillTags) : null,
            Education = parsed.Education?.FirstOrDefault() != null
                ? $"{parsed.Education[0].School} - {parsed.Education[0].Major}"
                : null,
            WorkExperience = parsed.TotalWorkYears,
            Source = "AI Parsed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Candidates.Add(candidate);
        await _dbContext.SaveChangesAsync();
        return candidate;
    }

    private void UpdateCandidateFromParsed(Candidate candidate, ParsedResumeResult result)
    {
        if (!string.IsNullOrEmpty(result.SkillsJson))
        {
            candidate.Skills = result.SkillsJson;
        }
        if (result.WorkExperienceYears.HasValue)
        {
            candidate.WorkExperience = result.WorkExperienceYears;
        }
        if (!string.IsNullOrEmpty(result.Education))
        {
            candidate.Education = result.Education;
        }
        if (result.ExpectedSalary.HasValue)
        {
            candidate.ExpectedSalary = result.ExpectedSalary;
        }
    }

    private Candidate CreateCandidateFromParsed(Resume resume, ParsedResumeResult result)
    {
        return new Candidate
        {
            Name = resume.CandidateName ?? "Unknown",
            Email = resume.Email,
            Phone = resume.Phone,
            Skills = result.SkillsJson,
            WorkExperience = result.WorkExperienceYears,
            Education = result.Education,
            ExpectedSalary = result.ExpectedSalary,
            Source = resume.Source,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static ParsedResumeResult MockParseResume(Resume resume)
    {
        var random = new Random();
        var skillSets = new List<string>[]
        {
            new List<string> { "Java", "Spring Boot", "MySQL", "Redis", "Docker" },
            new List<string> { "Vue.js", "React", "TypeScript", "Node.js", "Webpack" },
            new List<string> { "Python", "Django", "Flask", "Machine Learning", "TensorFlow" },
            new List<string> { "Go", "gRPC", "Kubernetes", "Prometheus", "gRPC" },
            new List<string> { "Product Management", "Agile", "Scrum", "User Research", "Data Analysis" }
        };

        var experiences = new (int Years, string Summary)[]
        {
            (5, "5年互联网产品经验，曾负责DAU 1000万产品"),
            (3, "3年前端开发经验，精通Vue和React生态"),
            (4, "4年后端开发经验，擅长高并发系统设计"),
            (2, "2年数据分析师，熟悉Python和SQL"),
            (6, "6年全栈开发经验，精通移动端和Web开发")
        };

        var educations = new[] { "本科 - 计算机科学", "硕士 - 软件工程", "本科 - 信息管理", "博士 - 人工智能" };

        var skills = skillSets[random.Next(skillSets.Length)];
        var skillJson = JsonSerializer.Serialize(skills);
        var exp = experiences[random.Next(experiences.Length)];

        var result = new ParsedResumeResult
        {
            ResumeId = resume.Id,
            CandidateName = resume.CandidateName ?? "候选人",
            Phone = resume.Phone,
            Email = resume.Email,
            WorkExperienceYears = exp.Years,
            WorkExperienceSummary = exp.Summary,
            Education = educations[random.Next(educations.Length)],
            Skills = skills,
            SkillsJson = skillJson,
            ExpectedSalary = (random.Next(15, 50) * 1000),
            MatchScore = random.Next(60, 95),
            Summary = $"候选人{resume.CandidateName}具有{skills.Count}项核心技能，与职位匹配度较高"
        };

        return result;
    }
}