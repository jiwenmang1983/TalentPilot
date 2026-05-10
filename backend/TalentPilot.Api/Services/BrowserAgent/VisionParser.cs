using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TalentPilot.Api.Services.BrowserAgent;

/// <summary>
/// MiniMax Vision API 截图解析器 - 从简历截图中提取结构化数据
/// </summary>
public class VisionParser
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VisionParser> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.minimaxi.com/anthropic/v1/messages";

    public VisionParser(IHttpClientFactory httpClientFactory, ILogger<VisionParser> logger, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["MiniMax:ApiKey"] ?? throw new InvalidOperationException("MiniMax ApiKey not configured");
    }

    /// <summary>
    /// 解析简历详情页截图
    /// </summary>
    public async Task<ResumeScreenshotResult?> ParseResumeDetailAsync(byte[] screenshotBytes)
    {
        var base64Image = Convert.ToBase64String(screenshotBytes);

        var requestBody = new
        {
            model = "MiniMax-4k-Vision",
            max_tokens = 2048,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = @"你是一个专业的简历解析AI。请从截图中提取以下结构化信息，以JSON格式返回：
{
  ""姓名"": ""候选人姓名"",
  ""性别"": ""男/女"",
  ""年龄"": ""25岁"",
  ""学历"": ""本科/硕士/博士"",
  ""工作年限"": ""3年"",
  ""当前公司"": ""公司名称"",
  ""当前职位"": ""职位名称"",
  ""期望职位"": ""期望的职位"",
  ""期望薪资"": ""15K-20K"",
  ""工作经历"": [{""公司"": """", ""职位"": """", ""时间"": """", ""描述"": """"}],
  ""项目经历"": [{""项目名"": """", ""角色"": """", ""描述"": """"}],
  ""技能标签"": [""技能1"", ""技能2""],
  ""自我介绍"": ""自我介绍内容""
}
如果某字段无法从截图中获取，请返回null。只返回JSON，不要其他文字。" },
                        new { type = "image", source = new { type = "base64", media_type = "image/png", data = base64Image } }
                    }
                }
            }
        };

        var client = _httpClientFactory.CreateClient("MiniMax");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(_baseUrl, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MiniMax Vision API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
            return null;
        }

        try
        {
            var respObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var contentArray = respObj.TryGetProperty("content", out var ca) ? ca : default;

            string? extractedText = null;
            if (contentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in contentArray.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var type) && type.GetString() == "text")
                    {
                        extractedText = item.GetProperty("text").GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("MiniMax Vision returned no text content");
                return null;
            }

            extractedText = extractedText.Trim();
            if (extractedText.StartsWith("```"))
            {
                var firstNewline = extractedText.IndexOf('\n');
                if (firstNewline > 0)
                    extractedText = extractedText[(firstNewline + 1)..];
            }
            if (extractedText.EndsWith("```"))
                extractedText = extractedText[..^3];

            var result = JsonSerializer.Deserialize<ResumeScreenshotResult>(extractedText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Vision API response: {Content}", responseContent);
            return null;
        }
    }

    /// <summary>
    /// 解析简历列表页截图 - 提取候选人卡片列表
    /// </summary>
    public async Task<List<ResumeListItem>> ParseResumeListAsync(byte[] screenshotBytes)
    {
        var base64Image = Convert.ToBase64String(screenshotBytes);

        var requestBody = new
        {
            model = "MiniMax-4k-Vision",
            max_tokens = 1024,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = @"你是一个专业的招聘HR。请从截图中识别所有候选人卡片列表，以JSON数组格式返回：
[
  {
    ""姓名"": ""姓名"",
    ""性别"": ""男/女"",
    ""年龄"": ""25岁"",
    ""学历"": ""本科/硕士"",
    ""工作年限"": ""3年"",
    ""当前公司"": ""公司名称"",
    ""期望职位"": ""职位名称"",
    ""标签"": [""技能标签1"", ""标签2""]
  }
]
只返回JSON数组，不要其他文字。如果页面没有候选人列表，返回空数组[]。" },
                        new { type = "image", source = new { type = "base64", media_type = "image/png", data = base64Image } }
                    }
                }
            }
        };

        var client = _httpClientFactory.CreateClient("MiniMax");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(_baseUrl, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MiniMax Vision API error for list parsing: {StatusCode}", response.StatusCode);
            return new List<ResumeListItem>();
        }

        try
        {
            var respObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
            string? extractedText = null;
            var contentArray = respObj.TryGetProperty("content", out var ca) ? ca : default;
            if (contentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in contentArray.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var type) && type.GetString() == "text")
                    {
                        extractedText = item.GetProperty("text").GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(extractedText))
                return new List<ResumeListItem>();

            extractedText = extractedText.Trim();
            if (extractedText.StartsWith("```"))
            {
                var firstNewline = extractedText.IndexOf('\n');
                if (firstNewline > 0) extractedText = extractedText[(firstNewline + 1)..];
            }
            if (extractedText.EndsWith("```"))
                extractedText = extractedText[..^3];

            return JsonSerializer.Deserialize<List<ResumeListItem>>(extractedText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }) ?? new List<ResumeListItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse resume list from Vision API");
            return new List<ResumeListItem>();
        }
    }

    /// <summary>
    /// 评估候选人与职位的匹配度（MiniMax LLM 判断）
    /// </summary>
    public async Task<CandidateRelevanceResult?> EvaluateCandidateRelevanceAsync(
        ResumeListItem candidate,
        string jobTitle,
        string jobDescription,
        CancellationToken ct = default)
    {
        var prompt = $@"你是一个专业的招聘HR。请根据以下信息，判断候选人是否符合职位要求。

**职位信息：**
- 职位名称：{jobTitle}
- 职位描述：{jobDescription}

**候选人信息：**
- 姓名：{candidate.姓名 ?? "未知"}
- 当前公司：{candidate.当前公司 ?? "未知"}
- 期望职位：{candidate.期望职位 ?? "未知"}
- 学历：{candidate.学历 ?? "未知"}
- 工作年限：{candidate.工作年限 ?? "未知"}
- 技能标签：{string.Join(", ", candidate.标签 ?? new List<string>())}

请以JSON格式返回评估结果：
{{
  ""符合职位"": true或false,
  ""匹配度"": 0-100的整数,
  ""简要理由"": ""一句话说明原因""
}}

只返回JSON，不要其他文字。";

        var requestBody = new
        {
            model = "MiniMax-M2.7",
            max_tokens = 512,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt }
                    }
                }
            }
        };

        var client = _httpClientFactory.CreateClient("MiniMax");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(_baseUrl, httpContent, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MiniMax relevance evaluation API error: {StatusCode}", response.StatusCode);
            return null;
        }

        try
        {
            var respObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var contentArray = respObj.TryGetProperty("content", out var ca) ? ca : default;

            string? extractedText = null;
            if (contentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in contentArray.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var type) && type.GetString() == "text")
                    {
                        extractedText = item.GetProperty("text").GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(extractedText))
                return null;

            extractedText = extractedText.Trim();
            if (extractedText.StartsWith("```"))
            {
                var firstNewline = extractedText.IndexOf('\n');
                if (firstNewline > 0) extractedText = extractedText[(firstNewline + 1)..];
            }
            if (extractedText.EndsWith("```"))
                extractedText = extractedText[..^3];

            return JsonSerializer.Deserialize<CandidateRelevanceResult>(extractedText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse relevance evaluation response");
            return null;
        }
    }

    /// <summary>
    /// 根据 JD 关键词构建 Boss 搜索 URL
    /// </summary>
    public string BuildSearchUrl(string jobTitle, string? city = null, int page = 1)
    {
        var baseUrl = "https://www.zhipin.com/web/geek/jobs";
        var query = Uri.EscapeDataString(jobTitle);
        var pageParam = $"&page={page}";
        var cityParam = !string.IsNullOrEmpty(city) ? $"&city={Uri.EscapeDataString(city)}" : "";
        return $"{baseUrl}?query={query}{cityParam}{pageParam}";
    }
}

public class ResumeScreenshotResult
{
    public string? 姓名 { get; set; }
    public string? 性别 { get; set; }
    public string? 年龄 { get; set; }
    public string? 学历 { get; set; }
    public string? 工作年限 { get; set; }
    public string? 当前公司 { get; set; }
    public string? 当前职位 { get; set; }
    public string? 期望职位 { get; set; }
    public string? 期望薪资 { get; set; }
    public List<WorkExperience>? 工作经历 { get; set; }
    public List<ProjectExperience>? 项目经历 { get; set; }
    public List<string>? 技能标签 { get; set; }
    public string? 自我介绍 { get; set; }
}

public class WorkExperience
{
    public string? 公司 { get; set; }
    public string? 职位 { get; set; }
    public string? 时间 { get; set; }
    public string? 描述 { get; set; }
}

public class ProjectExperience
{
    public string? 项目名 { get; set; }
    public string? 角色 { get; set; }
    public string? 描述 { get; set; }
}

public class ResumeListItem
{
    public string? 姓名 { get; set; }
    public string? 性别 { get; set; }
    public string? 年龄 { get; set; }
    public string? 学历 { get; set; }
    public string? 工作年限 { get; set; }
    public string? 当前公司 { get; set; }
    public string? 期望职位 { get; set; }
    public List<string>? 标签 { get; set; }
}

public class CandidateRelevanceResult
{
    public bool 符合职位 { get; set; }
    public int 匹配度 { get; set; }
    public string? 简要理由 { get; set; }
}
