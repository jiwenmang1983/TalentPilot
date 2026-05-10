# T-61: F-22 报告导出PDF/Excel

## 需求来源
PRD §3.7 F-22：报告查看与导出

## 需求描述
HR/管理员可在线查看报告，支持导出PDF（带公司logo）和Excel（批量，周度汇报用）。

## 现状
- InterviewReport实体完整（OverallScore/Strengths/Concerns/Recommendation等字段）
- InterviewReportService已有GetByIdAsync/GetAllAsync
- InterviewReportsController已有GET端点
- 前端InterviewReports.vue已有表格展示
- 无导出功能，无PDF/Excel库

## 实现方案

### 1. 添加NuGet依赖（Program.cs或直接.csproj）
```xml
<!-- PDF生成 -->
<PackageReference Include="QuestPDF" Version="2024.3.0" />
<!-- Excel生成 -->
<PackageReference Include="ClosedXML" Version="0.102.3" />
```

### 2. 新建 ExportService.cs
```csharp
namespace TalentPilot.Api.Services;

public interface IExportService
{
    byte[] GeneratePdfReport(int reportId);
    byte[] GenerateExcelReport(List<int> reportIds);
    byte[] GenerateSingleExcelReport(int reportId);
}

public class ExportService : IExportService
{
    // GeneratePdfReport: 使用QuestPDF生成专业PDF报告
    // GenerateExcelReport: 批量导出多份报告到单个Excel（多Sheet）
    // GenerateSingleExcelReport: 导出单份报告到Excel
}
```

### 3. PDF报告内容（QuestPDF）
```
┌─────────────────────────────────────────────┐
│  TalentPilot 面试评估报告         [Logo]    │
├─────────────────────────────────────────────┤
│  候选人：张三    应聘职位：产品经理           │
│  面试时间：2026-05-10  报告生成时间：xxx    │
├─────────────────────────────────────────────┤
│  【综合评分】                                │
│  85/100  ⭐⭐⭐⭐☆ 推荐：强烈推荐           │
├─────────────────────────────────────────────┤
│  【优势】                                    │
│  ✓ 5年产品经验，具备良好的项目管理能力       │
│  ✓ 逻辑清晰，表达流畅                        │
├─────────────────────────────────────────────┤
│  【风险点】                                  │
│  ! 缺乏大规模团队管理经验                    │
│  ! 行业经验相对单一                          │
├─────────────────────────────────────────────┤
│  【AI评语】                                  │
│  候选人在面试中表现良好，具备良好的沟通能力...│
└─────────────────────────────────────────────┘
```

### 4. Excel报告内容（ClosedXML）
- Sheet1: 汇总表（候选人姓名、职位、评分、推荐等级、面试时间）
- Sheet2+: 每份报告详细数据

### 5. Controller端点
```csharp
// 单份PDF导出
[HttpGet("{id}/export-pdf")]
[Authorize(Roles = "admin,hr")]
public IActionResult ExportPdf(int id)

// 单份Excel导出
[HttpGet("{id}/export-excel")]
[Authorize(Roles = "admin,hr")]
public IActionResult ExportExcel(int id)

// 批量Excel导出
[HttpPost("export-excel-batch")]
[Authorize(Roles = "admin,hr")]
public IActionResult ExportExcelBatch([FromBody] List<int> reportIds)
```

### 6. 前端（InterviewReports.vue）
- 每行报告添加"导出PDF"和"导出Excel"按钮
- 批量选择checkbox + "批量导出Excel"按钮

## 验收标准
- [ ] dotnet add package QuestPDF 和 ClosedXML 成功
- [ ] dotnet build 0 errors
- [ ] PDF导出：下载的PDF包含完整报告信息，格式专业
- [ ] Excel导出：下载的xlsx可正常用Excel打开
- [ ] commit message: "feat: F-22 report export PDF/Excel"
