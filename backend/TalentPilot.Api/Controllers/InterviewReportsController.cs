using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// InterviewReports
/// </summary>
[ApiController]
[Route("api/interview-reports")]
[Authorize(Roles = "admin,hr")]
public class InterviewReportsController : ControllerBase
{
    private readonly InterviewReportService _reportService;
    private readonly OperationLogService _logService;
    private readonly IExportService _exportService;
    private readonly ILogger<InterviewReportsController> _logger;

    public InterviewReportsController(
        InterviewReportService reportService,
        OperationLogService logService,
        IExportService exportService,
        ILogger<InterviewReportsController> logger)
    {
        _reportService = reportService;
        _logService = logService;
        _exportService = exportService;
        _logger = logger;
    }

    [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> GetReports(
        [FromQuery] long? candidateId = null,
        [FromQuery] int? jobPostId = null,
        [FromQuery] string? recommendation = null,
        [FromQuery] decimal? minScore = null,
        [FromQuery] decimal? maxScore = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _reportService.GetAllAsync(
            candidateId, jobPostId, recommendation,
            minScore, maxScore, dateFrom, dateTo,
            page, pageSize);

        return Ok(new ApiResponse<object>(true, "获取成功", new
        {
            total,
            page,
            pageSize,
            items = items.Select(r => new InterviewReportListItem
            {
                Id = r.Id,
                CandidateName = r.Candidate?.Name,
                JobPostTitle = r.JobPost?.Title,
                OverallScore = r.OverallScore,
                ScoreText = r.ScoreText,
                Recommendation = r.Recommendation,
                CreatedAt = r.CreatedAt
            })
        }));
    }

    [HttpGet("{id}")]  // GET /api/interview-reports/{id} — 无int约束避免与子路由冲突
    [Authorize(Roles = "admin,hr,hiring_manager")]
    public async Task<ActionResult<ApiResponse<object>>> GetReport(int id)
    {
        var report = await _reportService.GetByIdAsync(id);
        if (report == null)
            return NotFound(new ApiResponse<object>(false, "报告不存在", null));

        return Ok(new ApiResponse<object>(true, "获取成功", ConvertToResponse(report)));
    }

    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult<ApiResponse<object>>> GetReportBySession(int sessionId)
    {
        var report = await _reportService.GetBySessionIdAsync(sessionId);
        if (report == null)
            return NotFound(new ApiResponse<object>(false, "该会话暂无报告", null));

        return Ok(new ApiResponse<object>(true, "获取成功", ConvertToResponse(report)));
    }

    [HttpPost("generate/{sessionId}")]
    [Authorize(Roles = "admin,hr")]
    public async Task<ActionResult<ApiResponse<object>>> GenerateReport(int sessionId)
    {
        try
        {
            var report = await _reportService.GenerateReportAsync(sessionId);

            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                await _logService.RecordLog(userId, "GENERATE", "InterviewReport", report.Id,
                    $"生成面试报告 {report.Id}", HttpContext);
            }

            return Ok(new ApiResponse<object>(true, "报告生成成功", ConvertToResponse(report)));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>(false, ex.Message, null));
        }
    }

    [HttpPut("{id}/notes")]
    [Authorize(Roles = "admin,hr")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateNotes(int id, [FromBody] UpdateHrNotesRequest request)
    {
        var report = await _reportService.UpdateHrNotesAsync(id, request.HrNotes);
        if (report == null)
            return NotFound(new ApiResponse<object>(false, "报告不存在", null));

        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            await _logService.RecordLog(userId, "UPDATE", "InterviewReport", id, $"更新HR备注", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "更新成功", new { report.Id }));
    }

    [HttpGet("{id}/export-pdf")]
    [Authorize(Roles = "admin,hr")]
    public IActionResult ExportPdf(int id)
    {
        try
        {
            var pdfBytes = _exportService.GeneratePdfReport(id);
            return File(pdfBytes, "application/pdf", $"面试报告_{id}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export PDF for report {ReportId}", id);
            return BadRequest(new ApiResponse<object>(false, "PDF导出失败", null));
        }
    }

    [HttpGet("{id}/export-excel")]
    [Authorize(Roles = "admin,hr")]
    public IActionResult ExportExcel(int id)
    {
        try
        {
            var excelBytes = _exportService.GenerateSingleExcelReport(id);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"面试报告_{id}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Excel for report {ReportId}", id);
            return BadRequest(new ApiResponse<object>(false, "Excel导出失败", null));
        }
    }

    [HttpPost("export-excel-batch")]
    [Authorize(Roles = "admin,hr")]
    public IActionResult ExportExcelBatch([FromBody] List<int> reportIds)
    {
        try
        {
            var excelBytes = _exportService.GenerateBatchExcelReport(reportIds);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"批量面试报告_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch export Excel, count={Count}", reportIds?.Count ?? 0);
            return BadRequest(new ApiResponse<object>(false, "批量Excel导出失败", null));
        }
    }

    private static InterviewReportResponse ConvertToResponse(Models.Entities.InterviewReport report)
    {
        Dictionary<string, decimal>? dimensionScores = null;
        List<string>? highlights = null;
        List<string>? concerns = null;

        try
        {
            if (!string.IsNullOrEmpty(report.DimensionScores))
                dimensionScores = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, decimal>>(report.DimensionScores);
            if (!string.IsNullOrEmpty(report.Highlights))
                highlights = System.Text.Json.JsonSerializer.Deserialize<List<string>>(report.Highlights);
            if (!string.IsNullOrEmpty(report.Concerns))
                concerns = System.Text.Json.JsonSerializer.Deserialize<List<string>>(report.Concerns);
        }
        catch { }

        return new InterviewReportResponse
        {
            Id = report.Id,
            AIInterviewSessionId = report.AIInterviewSessionId,
            CandidateId = report.CandidateId,
            CandidateName = report.Candidate?.Name,
            JobPostId = report.JobPostId,
            JobPostTitle = report.JobPost?.Title,
            OverallScore = report.OverallScore,
            ScoreText = report.ScoreText,
            DimensionScores = dimensionScores,
            AiComments = report.AiComments,
            Recommendation = report.Recommendation,
            Highlights = highlights,
            Concerns = concerns,
            HrNotes = report.HrNotes,
            CreatedAt = report.CreatedAt
        };
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
