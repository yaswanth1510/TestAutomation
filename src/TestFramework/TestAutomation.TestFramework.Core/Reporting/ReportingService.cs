using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TestAutomation.Domain.Models;
using TestAutomation.Domain.Models.Configuration;
using TestAutomation.Domain.Models.Reporting;
using TestAutomation.TestFramework.Core.Assets;

namespace TestAutomation.TestFramework.Core.Reporting;

/// <summary>
/// Comprehensive Test Reporting Service - Generates detailed reports with multiple formats
/// </summary>
public interface IReportingService
{
    Task<TestExecutionReport> GenerateReportAsync(TestRun testRun, ReportConfiguration configuration);
    Task<string> SaveReportAsync(TestExecutionReport report, string outputPath);
    Task<TrendAnalysisReport> GenerateTrendReportAsync(IEnumerable<TestRun> historicalRuns, TimeSpan period);
    Task<byte[]> ExportReportAsync(TestExecutionReport report, ReportFormat format);
    Task<string> GenerateReportUrlAsync(Guid reportId);
    Task<bool> DeleteReportAsync(Guid reportId);
    Task<IEnumerable<TestExecutionReport>> GetReportsAsync(DateTime? startDate = null, DateTime? endDate = null);
}

/// <summary>
/// Implementation of the Reporting Service
/// </summary>
public class ReportingService : IReportingService
{
    private readonly IAssetManager _assetManager;
    private readonly ILogger<ReportingService> _logger;
    private readonly Dictionary<ReportFormat, IReportFormatter> _formatters;

    public ReportingService(IAssetManager assetManager, ILogger<ReportingService> logger)
    {
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _formatters = new Dictionary<ReportFormat, IReportFormatter>
        {
            { ReportFormat.JSON, new JsonReportFormatter() },
            { ReportFormat.HTML, new HtmlReportFormatter() },
            { ReportFormat.XML, new XmlReportFormatter() },
            { ReportFormat.CSV, new CsvReportFormatter() },
            { ReportFormat.Markdown, new MarkdownReportFormatter() }
        };
    }

    public async Task<TestExecutionReport> GenerateReportAsync(TestRun testRun, ReportConfiguration configuration)
    {
        _logger.LogInformation("Generating test execution report for run: {TestRunId}", testRun.Id);

        var report = new TestExecutionReport
        {
            ReportName = $"Test Execution Report - {testRun.Name}",
            Description = $"Comprehensive report for test run executed on {testRun.StartedAt:yyyy-MM-dd HH:mm:ss}",
            Type = ReportType.Standard,
            Format = configuration.OutputFormat,
            ExecutionStartTime = testRun.StartedAt ?? DateTime.UtcNow,
            ExecutionEndTime = testRun.CompletedAt ?? DateTime.UtcNow,
            TotalDuration = testRun.Duration ?? TimeSpan.Zero,
            Configuration = configuration
        };

        // Generate test summary
        report.Summary = await GenerateTestSummaryAsync(testRun);

        // Generate environment information
        report.Environment = await GenerateEnvironmentInfoAsync();

        // Generate test results
        report.TestRuns.Add(await GenerateTestRunResultAsync(testRun));

        // Generate performance metrics
        report.Performance = await GeneratePerformanceMetricsAsync(testRun);

        // Collect assets
        if (configuration.IncludeScreenshots || configuration.IncludeStepDetails)
        {
            report.Assets.AddRange(await CollectReportAssetsAsync(testRun));
        }

        _logger.LogInformation("Test execution report generated successfully: {ReportId}", report.Id);

        return report;
    }

    public async Task<string> SaveReportAsync(TestExecutionReport report, string outputPath)
    {
        try
        {
            var formatter = _formatters.GetValueOrDefault(report.Format, _formatters[ReportFormat.JSON]);
            var content = await formatter.FormatAsync(report);
            
            var fileName = $"TestReport_{report.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{GetFileExtension(report.Format)}";
            var fullPath = Path.Combine(outputPath, fileName);
            
            Directory.CreateDirectory(outputPath);
            await File.WriteAllTextAsync(fullPath, content);
            
            _logger.LogInformation("Report saved to: {FilePath}", fullPath);
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving report: {ReportId}", report.Id);
            throw;
        }
    }

    public async Task<TrendAnalysisReport> GenerateTrendReportAsync(IEnumerable<TestRun> historicalRuns, TimeSpan period)
    {
        var runs = historicalRuns.ToList();
        var endDate = DateTime.UtcNow;
        var startDate = endDate - period;

        _logger.LogInformation("Generating trend analysis report for {RunCount} test runs over {Period}", 
            runs.Count, period);

        var trendReport = new TrendAnalysisReport
        {
            ReportName = $"Trend Analysis Report - {period.Days} days",
            AnalysisStartDate = startDate,
            AnalysisEndDate = endDate,
            AnalysisPeriod = period
        };

        // Generate trend data points
        var groupedRuns = runs
            .Where(r => r.StartedAt >= startDate && r.StartedAt <= endDate)
            .GroupBy(r => r.StartedAt?.Date ?? DateTime.UtcNow.Date)
            .OrderBy(g => g.Key);

        foreach (var group in groupedRuns)
        {
            var dayRuns = group.ToList();
            var dataPoint = new TrendDataPoint
            {
                Date = group.Key,
                TotalTests = dayRuns.Sum(r => r.TotalTests),
                PassedTests = dayRuns.Sum(r => r.PassedTests),
                FailedTests = dayRuns.Sum(r => r.FailedTests)
            };
            
            dataPoint.SuccessRate = dataPoint.TotalTests > 0 
                ? (decimal)dataPoint.PassedTests / dataPoint.TotalTests * 100 
                : 0;
            
            dataPoint.AverageDuration = dayRuns
                .Where(r => r.Duration.HasValue)
                .Select(r => r.Duration!.Value)
                .DefaultIfEmpty(TimeSpan.Zero)
                .Aggregate((t1, t2) => t1.Add(t2))
                .Divide(Math.Max(dayRuns.Count, 1));

            trendReport.TrendData.Add(dataPoint);
        }

        // Generate statistics
        trendReport.Statistics = await GenerateTrendStatisticsAsync(trendReport.TrendData);

        // Detect anomalies
        trendReport.Anomalies.AddRange(await DetectAnomaliesAsync(trendReport.TrendData));

        return trendReport;
    }

    public async Task<byte[]> ExportReportAsync(TestExecutionReport report, ReportFormat format)
    {
        try
        {
            var formatter = _formatters.GetValueOrDefault(format, _formatters[ReportFormat.JSON]);
            var content = await formatter.FormatAsync(report);
            
            return Encoding.UTF8.GetBytes(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report: {ReportId} to format: {Format}", report.Id, format);
            throw;
        }
    }

    public async Task<string> GenerateReportUrlAsync(Guid reportId)
    {
        await Task.CompletedTask;
        // In a real implementation, this would generate a URL to view the report
        return $"/reports/{reportId}";
    }

    public async Task<bool> DeleteReportAsync(Guid reportId)
    {
        try
        {
            await Task.CompletedTask;
            // In a real implementation, this would delete the report from storage
            _logger.LogInformation("Report deleted: {ReportId}", reportId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report: {ReportId}", reportId);
            return false;
        }
    }

    public async Task<IEnumerable<TestExecutionReport>> GetReportsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        await Task.CompletedTask;
        // In a real implementation, this would query reports from storage
        return new List<TestExecutionReport>();
    }

    private async Task<TestSummary> GenerateTestSummaryAsync(TestRun testRun)
    {
        await Task.CompletedTask;
        
        var summary = new TestSummary
        {
            TotalTests = testRun.TotalTests,
            PassedTests = testRun.PassedTests,
            FailedTests = testRun.FailedTests,
            SkippedTests = testRun.SkippedTests
        };

        if (testRun.TestResults.Any())
        {
            var durations = testRun.TestResults
                .Where(tr => tr.Duration.HasValue)
                .Select(tr => tr.Duration!.Value)
                .ToList();

            if (durations.Any())
            {
                summary.AverageTestDuration = new TimeSpan((long)durations.Average(d => d.Ticks));
                summary.ShortestTestDuration = durations.Min();
                summary.LongestTestDuration = durations.Max();
            }

            // Group by category
            var categories = testRun.TestResults
                .SelectMany(tr => tr.Tags)
                .Where(tag => !string.IsNullOrEmpty(tag))
                .GroupBy(tag => tag)
                .ToDictionary(g => g.Key, g => g.Count());

            summary.TestsByCategory = categories;

            // Top failure reasons
            var failureReasons = testRun.TestResults
                .Where(tr => tr.Status == TestResultStatus.Failed && !string.IsNullOrEmpty(tr.ErrorMessage))
                .GroupBy(tr => tr.ErrorMessage!)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            summary.TopFailureReasons = failureReasons;
        }

        return summary;
    }

    private async Task<TestEnvironment> GenerateEnvironmentInfoAsync()
    {
        await Task.CompletedTask;
        
        return new TestEnvironment
        {
            OperatingSystem = Environment.OSVersion.ToString(),
            DotNetVersion = Environment.Version.ToString(),
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            WorkingDirectory = Environment.CurrentDirectory,
            TotalMemory = GC.GetTotalMemory(false),
            ProcessorCount = Environment.ProcessorCount
        };
    }

    private async Task<TestRunResult> GenerateTestRunResultAsync(TestRun testRun)
    {
        await Task.CompletedTask;
        
        var runResult = new TestRunResult
        {
            TestRunId = testRun.Id,
            TestName = testRun.Name,
            Status = MapTestRunStatusToResultStatus(testRun.Status),
            StartTime = testRun.StartedAt ?? DateTime.UtcNow,
            EndTime = testRun.CompletedAt ?? DateTime.UtcNow,
            Duration = testRun.Duration ?? TimeSpan.Zero,
            ErrorMessage = testRun.ErrorMessage,
            StackTrace = testRun.StackTrace
        };

        runResult.StepResults.AddRange(
            testRun.TestResults.SelectMany(tr => tr.StepResults));

        return runResult;
    }

    private async Task<PerformanceMetrics> GeneratePerformanceMetricsAsync(TestRun testRun)
    {
        await Task.CompletedTask;
        
        var metrics = new PerformanceMetrics
        {
            TotalExecutionTime = testRun.Duration ?? TimeSpan.Zero,
            TestsPerSecond = testRun.Duration?.TotalSeconds > 0 
                ? testRun.TotalTests / testRun.Duration.Value.TotalSeconds 
                : 0,
            ThreadCount = Environment.ProcessorCount
        };

        if (testRun.TestResults.Any())
        {
            var validDurations = testRun.TestResults
                .Where(tr => tr.Duration.HasValue)
                .Select(tr => tr.Duration!.Value);

            if (validDurations.Any())
            {
                metrics.AverageTestTime = new TimeSpan((long)validDurations.Average(d => d.Ticks));
            }
        }

        // Capture current memory usage
        metrics.PeakMemoryUsage = GC.GetTotalMemory(false);
        metrics.AverageMemoryUsage = metrics.PeakMemoryUsage;

        return metrics;
    }

    private async Task<List<TestAsset>> CollectReportAssetsAsync(TestRun testRun)
    {
        var assets = new List<TestAsset>();

        try
        {
            foreach (var testResult in testRun.TestResults)
            {
                assets.AddRange(testResult.Assets);
                
                foreach (var stepResult in testResult.StepResults)
                {
                    assets.AddRange(stepResult.Assets);
                }
            }

            // Create a summary log asset
            var logContent = GenerateExecutionLogSummary(testRun);
            var logAsset = await _assetManager.SaveLogAsync(
                $"ExecutionSummary_{testRun.Id}", 
                logContent,
                "Execution summary log for the test run");
            
            assets.Add(logAsset);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error collecting report assets for test run: {TestRunId}", testRun.Id);
        }

        return assets;
    }

    private async Task<TrendStatistics> GenerateTrendStatisticsAsync(List<TrendDataPoint> trendData)
    {
        await Task.CompletedTask;
        
        var statistics = new TrendStatistics();

        if (trendData.Any())
        {
            statistics.AverageSuccessRate = trendData.Average(dp => dp.SuccessRate);
            statistics.AverageExecutionTime = new TimeSpan((long)trendData.Average(dp => dp.AverageDuration.Ticks));
            statistics.TotalExecutions = trendData.Sum(dp => dp.TotalTests);

            // Calculate trends (simple linear trend)
            if (trendData.Count >= 2)
            {
                var firstHalf = trendData.Take(trendData.Count / 2).Average(dp => dp.SuccessRate);
                var secondHalf = trendData.Skip(trendData.Count / 2).Average(dp => dp.SuccessRate);
                statistics.SuccessRateTrend = secondHalf - firstHalf;

                var firstHalfTime = trendData.Take(trendData.Count / 2).Average(dp => dp.AverageDuration.TotalSeconds);
                var secondHalfTime = trendData.Skip(trendData.Count / 2).Average(dp => dp.AverageDuration.TotalSeconds);
                statistics.ExecutionTimeTrend = (decimal)(secondHalfTime - firstHalfTime);
            }

            // Identify improving and declining areas
            if (statistics.SuccessRateTrend > 1.0m)
                statistics.ImprovingAreas.Add("Success Rate");
            else if (statistics.SuccessRateTrend < -1.0m)
                statistics.DecliningAreas.Add("Success Rate");

            if (statistics.ExecutionTimeTrend < -10.0m) // Execution time decreasing is good
                statistics.ImprovingAreas.Add("Execution Time");
            else if (statistics.ExecutionTimeTrend > 10.0m)
                statistics.DecliningAreas.Add("Execution Time");
        }

        return statistics;
    }

    private async Task<List<TrendAnomaly>> DetectAnomaliesAsync(List<TrendDataPoint> trendData)
    {
        await Task.CompletedTask;
        
        var anomalies = new List<TrendAnomaly>();

        if (trendData.Count < 3) return anomalies;

        var successRates = trendData.Select(dp => (double)dp.SuccessRate).ToArray();
        var durations = trendData.Select(dp => dp.AverageDuration.TotalMinutes).ToArray();

        // Detect success rate anomalies
        var successRateMean = successRates.Average();
        var successRateStdDev = Math.Sqrt(successRates.Average(x => Math.Pow(x - successRateMean, 2)));

        for (int i = 0; i < trendData.Count; i++)
        {
            var deviation = Math.Abs(successRates[i] - successRateMean) / successRateStdDev;
            if (deviation > 2.0) // 2 standard deviations
            {
                anomalies.Add(new TrendAnomaly
                {
                    DetectedAt = trendData[i].Date,
                    Type = AnomalyType.SuccessRate,
                    Severity = deviation > 3.0 ? AnomalySeverity.High : AnomalySeverity.Medium,
                    Description = $"Success rate anomaly detected: {successRates[i]:F2}% (normal range: {successRateMean - 2 * successRateStdDev:F2}% - {successRateMean + 2 * successRateStdDev:F2}%)",
                    DeviationScore = (decimal)deviation
                });
            }
        }

        // Detect duration anomalies
        var durationMean = durations.Average();
        var durationStdDev = Math.Sqrt(durations.Average(x => Math.Pow(x - durationMean, 2)));

        for (int i = 0; i < trendData.Count; i++)
        {
            var deviation = Math.Abs(durations[i] - durationMean) / durationStdDev;
            if (deviation > 2.0)
            {
                anomalies.Add(new TrendAnomaly
                {
                    DetectedAt = trendData[i].Date,
                    Type = AnomalyType.Duration,
                    Severity = deviation > 3.0 ? AnomalySeverity.High : AnomalySeverity.Medium,
                    Description = $"Duration anomaly detected: {durations[i]:F2} minutes (normal range: {durationMean - 2 * durationStdDev:F2} - {durationMean + 2 * durationStdDev:F2} minutes)",
                    DeviationScore = (decimal)deviation
                });
            }
        }

        return anomalies;
    }

    private string GenerateExecutionLogSummary(TestRun testRun)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Test Execution Summary - {testRun.Name}");
        sb.AppendLine($"Execution ID: {testRun.Id}");
        sb.AppendLine($"Start Time: {testRun.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"End Time: {testRun.CompletedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Duration: {testRun.Duration}");
        sb.AppendLine($"Status: {testRun.Status}");
        sb.AppendLine();
        sb.AppendLine("Results Summary:");
        sb.AppendLine($"  Total Tests: {testRun.TotalTests}");
        sb.AppendLine($"  Passed: {testRun.PassedTests}");
        sb.AppendLine($"  Failed: {testRun.FailedTests}");
        sb.AppendLine($"  Skipped: {testRun.SkippedTests}");
        sb.AppendLine($"  Success Rate: {(testRun.TotalTests > 0 ? (decimal)testRun.PassedTests / testRun.TotalTests * 100 : 0):F2}%");

        if (!string.IsNullOrEmpty(testRun.ErrorMessage))
        {
            sb.AppendLine();
            sb.AppendLine("Execution Error:");
            sb.AppendLine(testRun.ErrorMessage);
        }

        return sb.ToString();
    }

    private static TestResultStatus MapTestRunStatusToResultStatus(TestRunStatus status)
    {
        return status switch
        {
            TestRunStatus.Completed => TestResultStatus.Passed,
            TestRunStatus.Failed => TestResultStatus.Failed,
            TestRunStatus.Cancelled => TestResultStatus.Skipped,
            _ => TestResultStatus.NotRun
        };
    }

    private static string GetFileExtension(ReportFormat format)
    {
        return format switch
        {
            ReportFormat.JSON => "json",
            ReportFormat.HTML => "html",
            ReportFormat.XML => "xml",
            ReportFormat.CSV => "csv",
            ReportFormat.Markdown => "md",
            ReportFormat.PDF => "pdf",
            ReportFormat.Excel => "xlsx",
            _ => "txt"
        };
    }
}

// Report Formatters

public interface IReportFormatter
{
    Task<string> FormatAsync(TestExecutionReport report);
}

public class JsonReportFormatter : IReportFormatter
{
    public async Task<string> FormatAsync(TestExecutionReport report)
    {
        await Task.CompletedTask;
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(report, options);
    }
}

public class HtmlReportFormatter : IReportFormatter
{
    public async Task<string> FormatAsync(TestExecutionReport report)
    {
        await Task.CompletedTask;
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{report.ReportName}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(GetDefaultCss());
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"  <header><h1>{report.ReportName}</h1></header>");
        sb.AppendLine("  <main>");
        
        // Summary section
        sb.AppendLine("    <section class=\"summary\">");
        sb.AppendLine("      <h2>Executive Summary</h2>");
        sb.AppendLine($"      <p>Execution Period: {report.ExecutionStartTime:yyyy-MM-dd HH:mm:ss} - {report.ExecutionEndTime:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine($"      <p>Total Duration: {report.TotalDuration}</p>");
        sb.AppendLine($"      <p>Success Rate: {report.Summary.SuccessRate:F2}%</p>");
        sb.AppendLine("      <div class=\"metrics\">");
        sb.AppendLine($"        <div class=\"metric passed\">Passed: {report.Summary.PassedTests}</div>");
        sb.AppendLine($"        <div class=\"metric failed\">Failed: {report.Summary.FailedTests}</div>");
        sb.AppendLine($"        <div class=\"metric skipped\">Skipped: {report.Summary.SkippedTests}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </section>");
        
        sb.AppendLine("  </main>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }

    private static string GetDefaultCss()
    {
        return @"
            body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }
            header { background-color: #2c3e50; color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; }
            h1 { margin: 0; }
            .summary { background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); margin-bottom: 20px; }
            .metrics { display: flex; gap: 15px; margin-top: 15px; }
            .metric { padding: 10px 15px; border-radius: 6px; font-weight: bold; }
            .metric.passed { background-color: #27ae60; color: white; }
            .metric.failed { background-color: #e74c3c; color: white; }
            .metric.skipped { background-color: #f39c12; color: white; }
        ";
    }
}

public class XmlReportFormatter : IReportFormatter
{
    public async Task<string> FormatAsync(TestExecutionReport report)
    {
        await Task.CompletedTask;
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<TestExecutionReport id=\"{report.Id}\" generated=\"{report.GeneratedAt:yyyy-MM-ddTHH:mm:ss}\">");
        sb.AppendLine($"  <Name>{report.ReportName}</Name>");
        sb.AppendLine($"  <Description>{report.Description}</Description>");
        sb.AppendLine($"  <ExecutionTime start=\"{report.ExecutionStartTime:yyyy-MM-ddTHH:mm:ss}\" end=\"{report.ExecutionEndTime:yyyy-MM-ddTHH:mm:ss}\" duration=\"{report.TotalDuration}\" />");
        sb.AppendLine("  <Summary>");
        sb.AppendLine($"    <TotalTests>{report.Summary.TotalTests}</TotalTests>");
        sb.AppendLine($"    <PassedTests>{report.Summary.PassedTests}</PassedTests>");
        sb.AppendLine($"    <FailedTests>{report.Summary.FailedTests}</FailedTests>");
        sb.AppendLine($"    <SkippedTests>{report.Summary.SkippedTests}</SkippedTests>");
        sb.AppendLine($"    <SuccessRate>{report.Summary.SuccessRate:F2}</SuccessRate>");
        sb.AppendLine("  </Summary>");
        sb.AppendLine("</TestExecutionReport>");
        return sb.ToString();
    }
}

public class CsvReportFormatter : IReportFormatter
{
    public async Task<string> FormatAsync(TestExecutionReport report)
    {
        await Task.CompletedTask;
        var sb = new StringBuilder();
        sb.AppendLine("TestName,TestClass,Status,StartTime,EndTime,Duration,ErrorMessage");
        
        foreach (var testRun in report.TestRuns)
        {
            sb.AppendLine($"\"{testRun.TestName}\",\"{testRun.TestClass}\",{testRun.Status},{testRun.StartTime:yyyy-MM-dd HH:mm:ss},{testRun.EndTime:yyyy-MM-dd HH:mm:ss},{testRun.Duration},\"{testRun.ErrorMessage?.Replace("\"", "\"\"")}\"");
        }
        
        return sb.ToString();
    }
}

public class MarkdownReportFormatter : IReportFormatter
{
    public async Task<string> FormatAsync(TestExecutionReport report)
    {
        await Task.CompletedTask;
        var sb = new StringBuilder();
        
        sb.AppendLine($"# {report.ReportName}");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Execution Period:** {report.ExecutionStartTime:yyyy-MM-dd HH:mm:ss} - {report.ExecutionEndTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Total Duration:** {report.TotalDuration}");
        sb.AppendLine();
        
        sb.AppendLine("## Executive Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Total Tests | {report.Summary.TotalTests} |");
        sb.AppendLine($"| Passed | {report.Summary.PassedTests} |");
        sb.AppendLine($"| Failed | {report.Summary.FailedTests} |");
        sb.AppendLine($"| Skipped | {report.Summary.SkippedTests} |");
        sb.AppendLine($"| Success Rate | {report.Summary.SuccessRate:F2}% |");
        sb.AppendLine();
        
        if (report.Summary.TopFailureReasons.Any())
        {
            sb.AppendLine("## Top Failure Reasons");
            sb.AppendLine();
            foreach (var reason in report.Summary.TopFailureReasons)
            {
                sb.AppendLine($"- {reason}");
            }
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
}