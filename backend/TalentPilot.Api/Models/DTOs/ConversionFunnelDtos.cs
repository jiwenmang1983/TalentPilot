namespace TalentPilot.Api.Models.DTOs;

public record ConversionFunnelQueryRequest(int? JobPostId, DateTime? DateFrom, DateTime? DateTo, int Page = 1, int PageSize = 20);

public record ConversionFunnelItemDto(
    int Id,
    int JobPostId,
    string JobPostTitle,
    string Stage,
    int Count,
    decimal ConversionRate,
    decimal AvgTimeSpent,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime CreatedAt);

public record ConversionFunnelSummaryDto(
    string Stage,
    int TotalCount,
    decimal AvgConversionRate,
    decimal AvgDays);

public record FunnelChartDto(
    List<string> Stages,
    List<int> Counts,
    List<decimal> Rates);