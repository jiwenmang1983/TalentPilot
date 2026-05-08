using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

[ApiController]
[Route("api/conversion-funnels")]
[Authorize(Roles = "admin,hr")]
public class ConversionFunnelsController : ControllerBase
{
    private readonly IConversionFunnelService _funnelService;

    public ConversionFunnelsController(IConversionFunnelService funnelService)
    {
        _funnelService = funnelService;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetAll(
        [FromQuery] int? jobPostId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new ConversionFunnelQueryRequest(jobPostId, dateFrom, dateTo, page, pageSize);
        var (items, total) = await _funnelService.GetAllAsync(query);

        return Ok(new { success = true, message = "获取成功", data = new { total, page, pageSize, items } });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetSummary(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var summary = await _funnelService.GetSummaryAsync(dateFrom, dateTo);
        return Ok(new { success = true, message = "获取成功", data = summary });
    }

    [HttpGet("chart")]
    public async Task<ActionResult<object>> GetChart(
        [FromQuery] int? jobPostId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var chartData = await _funnelService.GetFunnelChartDataAsync(jobPostId, dateFrom, dateTo);
        return Ok(new { success = true, message = "获取成功", data = chartData });
    }

    [HttpPost("seed")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<object>> SeedDemoData()
    {
        await _funnelService.SeedDemoDataAsync();
        return Ok(new { success = true, message = "演示数据生成成功", data = (object?)null });
    }
}

