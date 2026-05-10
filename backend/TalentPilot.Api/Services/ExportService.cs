using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public interface IExportService
{
    byte[] GeneratePdfReport(int reportId);
    byte[] GenerateSingleExcelReport(int reportId);
    byte[] GenerateBatchExcelReport(List<int> reportIds);
}

public class ExportService : IExportService
{
    private readonly TalentPilotDbContext _context;
    private readonly ILogger<ExportService> _logger;

    public ExportService(TalentPilotDbContext context, ILogger<ExportService> logger)
    {
        _context = context;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdfReport(int reportId)
    {
        var report = _context.InterviewReports
            .Include(r => r.Candidate)
            .Include(r => r.JobPost)
            .FirstOrDefault(r => r.Id == reportId);

        if (report == null)
            throw new InvalidOperationException($"报告 {reportId} 不存在");

        var dimensionScores = DeserializeJson<Dictionary<string, decimal>>(report.DimensionScores);
        var highlights = DeserializeJson<List<string>>(report.Highlights) ?? new();
        var concerns = DeserializeJson<List<string>>(report.Concerns) ?? new();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(c => ComposeHeader(c, report));
                page.Content().Element(c => ComposeContent(c, report, dimensionScores, highlights, concerns));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, InterviewReport report)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("TalentPilot").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                col.Item().Text("面试评估报告").FontSize(14).FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(80).Height(40).Placeholder();
        });
    }

    private void ComposeContent(IContainer container, InterviewReport report,
        Dictionary<string, decimal>? dimensionScores,
        List<string> highlights, List<string> concerns)
    {
        container.PaddingVertical(20).Column(col =>
        {
            col.Spacing(16);

            // Candidate Info Section
            col.Item().Background(Colors.Grey.Lighten4).Padding(15).Column(section =>
            {
                section.Spacing(8);
                section.Item().Text("候选人信息").FontSize(13).Bold();
                section.Item().Row(row =>
                {
                    row.RelativeItem().Text($"姓名：{report.Candidate?.Name ?? "未知"}");
                    row.RelativeItem().Text($"应聘职位：{report.JobPost?.Title ?? "未知"}");
                });
                section.Item().Row(row =>
                {
                    row.RelativeItem().Text($"报告生成时间：{report.CreatedAt:yyyy-MM-dd HH:mm}");
                });
            });

            // Score Section
            col.Item().Column(section =>
            {
                section.Spacing(8);
                section.Item().Text("【综合评分】").FontSize(13).Bold();
                section.Item().Padding(10).Background(Colors.Blue.Lighten5).Row(row =>
                {
                    row.RelativeItem().Text($"{report.OverallScore}").FontSize(32).Bold().FontColor(Colors.Blue.Medium);
                    row.RelativeItem().AlignMiddle().Text($"/ 100").FontSize(14).FontColor(Colors.Grey.Medium);
                    row.RelativeItem().AlignMiddle().Text(GetRecommendationBadge(report.Recommendation)).FontSize(12);
                });
                section.Item().Text($"评级：{report.ScoreText}").FontSize(11).FontColor(Colors.Grey.Medium);
            });

            // Dimension Scores
            if (dimensionScores != null && dimensionScores.Count > 0)
            {
                col.Item().Column(section =>
                {
                    section.Spacing(8);
                    section.Item().Text("【维度评分】").FontSize(13).Bold();
                    foreach (var dim in dimensionScores)
                    {
                        section.Item().Row(row =>
                        {
                            row.RelativeItem().Text(dim.Key);
                            row.ConstantItem(200).Background(Colors.Grey.Lighten3).Height(16)
                                .Row(r => r.RelativeItem((float)(dim.Value / 100.0m)).Background(GetScoreColor(dim.Value)).Height(16));
                            row.ConstantItem(40).AlignRight().Text($"{dim.Value}").FontSize(11);
                        });
                    }
                });
            }

            // Strengths
            if (highlights.Count > 0)
            {
                col.Item().Column(section =>
                {
                    section.Spacing(8);
                    section.Item().Text("【优势】").FontSize(13).Bold();
                    foreach (var h in highlights)
                    {
                        section.Item().Padding(4).Text($"✓ {h}").FontSize(11);
                    }
                });
            }

            // Concerns
            if (concerns.Count > 0)
            {
                col.Item().Column(section =>
                {
                    section.Spacing(8);
                    section.Item().Text("【风险点】").FontSize(13).Bold();
                    foreach (var c in concerns)
                    {
                        section.Item().Padding(4).Text($"⚠ {c}").FontSize(11);
                    }
                });
            }

            // AI Comments
            if (!string.IsNullOrWhiteSpace(report.AiComments))
            {
                col.Item().Column(section =>
                {
                    section.Spacing(8);
                    section.Item().Text("【AI评语】").FontSize(13).Bold();
                    section.Item().Padding(10).Background(Colors.Grey.Lighten4).Text(report.AiComments).FontSize(11);
                });
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("TalentPilot 面试系统 - 第 ");
            text.CurrentPageNumber();
            text.Span(" 页 / 共 ");
            text.TotalPages();
            text.Span(" 页");
        });
    }

    private static string GetRecommendationBadge(string recommendation)
    {
        return recommendation switch
        {
            "StrongHire" => "⭐ 强烈推荐",
            "Hire" => "✓ 建议录用",
            "Hold" => "◐ 观望",
            "Reject" => "✗ 不推荐",
            _ => recommendation
        };
    }

    private static string GetScoreColor(decimal score)
    {
        if (score >= 80) return Colors.Green.Medium;
        if (score >= 60) return Colors.Orange.Medium;
        return Colors.Red.Medium;
    }

    public byte[] GenerateSingleExcelReport(int reportId)
    {
        var reports = _context.InterviewReports
            .Include(r => r.Candidate)
            .Include(r => r.JobPost)
            .Where(r => r.Id == reportId)
            .ToList();

        if (!reports.Any())
            throw new InvalidOperationException($"报告 {reportId} 不存在");

        return GenerateExcelWorkbook(reports, true);
    }

    public byte[] GenerateBatchExcelReport(List<int> reportIds)
    {
        var reports = _context.InterviewReports
            .Include(r => r.Candidate)
            .Include(r => r.JobPost)
            .Where(r => reportIds.Contains(r.Id))
            .ToList();

        if (!reports.Any())
            throw new InvalidOperationException("未找到指定的报告");

        return GenerateExcelWorkbook(reports, false);
    }

    private byte[] GenerateExcelWorkbook(List<InterviewReport> reports, bool singleReport)
    {
        using var workbook = new XLWorkbook();
        var summarySheet = workbook.Worksheets.Add("汇总表");

        // Summary sheet headers
        var summaryHeaders = new[] { "报告ID", "候选人", "应聘职位", "综合评分", "评级", "推荐等级", "生成时间" };
        for (int i = 0; i < summaryHeaders.Length; i++)
        {
            summarySheet.Cell(1, i + 1).Value = summaryHeaders[i];
            summarySheet.Cell(1, i + 1).Style.Font.Bold = true;
            summarySheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Summary sheet data
        for (int row = 0; row < reports.Count; row++)
        {
            var r = reports[row];
            var dimensionScores = DeserializeJson<Dictionary<string, decimal>>(r.DimensionScores);
            var highlights = DeserializeJson<List<string>>(r.Highlights) ?? new();
            var concerns = DeserializeJson<List<string>>(r.Concerns) ?? new();

            summarySheet.Cell(row + 2, 1).Value = r.Id;
            summarySheet.Cell(row + 2, 2).Value = r.Candidate?.Name ?? "未知";
            summarySheet.Cell(row + 2, 3).Value = r.JobPost?.Title ?? "未知";
            summarySheet.Cell(row + 2, 4).Value = (double)r.OverallScore;
            summarySheet.Cell(row + 2, 5).Value = r.ScoreText;
            summarySheet.Cell(row + 2, 6).Value = GetRecommendationText(r.Recommendation);
            summarySheet.Cell(row + 2, 7).Value = r.CreatedAt.ToString("yyyy-MM-dd HH:mm");

            // Create detail sheet for each report
            var detailSheet = workbook.Worksheets.Add($"报告_{r.Id}");

            var detailHeaders = new[] { "字段", "内容" };
            detailSheet.Cell(1, 1).Value = detailHeaders[0];
            detailSheet.Cell(1, 2).Value = detailHeaders[1];
            detailSheet.Cell(1, 1).Style.Font.Bold = true;
            detailSheet.Cell(1, 2).Style.Font.Bold = true;
            detailSheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            detailSheet.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.LightBlue;

            var detailData = new List<(string Field, string Content)>
            {
                ("报告ID", r.Id.ToString()),
                ("候选人", r.Candidate?.Name ?? "未知"),
                ("应聘职位", r.JobPost?.Title ?? "未知"),
                ("综合评分", r.OverallScore.ToString()),
                ("评级", r.ScoreText),
                ("推荐等级", GetRecommendationText(r.Recommendation)),
                ("生成时间", r.CreatedAt.ToString("yyyy-MM-dd HH:mm"))
            };

            if (dimensionScores != null)
            {
                foreach (var dim in dimensionScores)
                {
                    detailData.Add((dim.Key, dim.Value.ToString()));
                }
            }

            if (highlights.Count > 0)
                detailData.Add(("优势", string.Join("; ", highlights)));

            if (concerns.Count > 0)
                detailData.Add(("风险点", string.Join("; ", concerns)));

            if (!string.IsNullOrWhiteSpace(r.AiComments))
                detailData.Add(("AI评语", r.AiComments));

            if (!string.IsNullOrWhiteSpace(r.HrNotes))
                detailData.Add(("HR备注", r.HrNotes));

            for (int d = 0; d < detailData.Count; d++)
            {
                detailSheet.Cell(d + 2, 1).Value = detailData[d].Field;
                detailSheet.Cell(d + 2, 2).Value = detailData[d].Content;
            }

            // Auto-fit columns for detail sheet
            detailSheet.Columns().AdjustToContents();
        }

        // Auto-fit columns for summary sheet
        summarySheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string GetRecommendationText(string recommendation)
    {
        return recommendation switch
        {
            "StrongHire" => "强烈推荐",
            "Hire" => "建议录用",
            "Hold" => "观望",
            "Reject" => "不推荐",
            _ => recommendation
        };
    }

    private static T? DeserializeJson<T>(string? json) where T : class
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }
}