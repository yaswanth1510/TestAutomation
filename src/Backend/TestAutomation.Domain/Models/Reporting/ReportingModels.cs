namespace TestAutomation.Domain.Models.Reporting;

/// <summary>
/// Represents a comprehensive test execution report
/// </summary>
public class TestExecutionReport : BaseEntity
{
    public string ReportName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReportType Type { get; set; } = ReportType.Standard;
    public ReportFormat Format { get; set; } = ReportFormat.JSON;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExecutionStartTime { get; set; }
    public DateTime ExecutionEndTime { get; set; }
    public TimeSpan TotalDuration { get; set; }
    
    // Test Summary
    public TestSummary Summary { get; set; } = new();
    
    // Environment Information
    public TestEnvironment Environment { get; set; } = new();
    
    // Test Results
    public List<TestRunResult> TestRuns { get; set; } = new();
    
    // Performance Metrics
    public PerformanceMetrics Performance { get; set; } = new();
    
    // Assets and Attachments
    public List<TestAsset> Assets { get; set; } = new();
    
    // Custom Data
    public Dictionary<string, object> CustomData { get; set; } = new();
    
    // Report Configuration
    public ReportConfiguration Configuration { get; set; } = new();
}

/// <summary>
/// Represents test execution summary
/// </summary>
public class TestSummary : BaseEntity
{
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public int InconclusiveTests { get; set; }
    public decimal SuccessRate => TotalTests > 0 ? (decimal)PassedTests / TotalTests * 100 : 0;
    public TimeSpan AverageTestDuration { get; set; }
    public TimeSpan ShortestTestDuration { get; set; }
    public TimeSpan LongestTestDuration { get; set; }
    public Dictionary<string, int> TestsByCategory { get; set; } = new();
    public Dictionary<string, int> TestsByPriority { get; set; } = new();
    public List<string> TopFailureReasons { get; set; } = new();
}

/// <summary>
/// Represents test environment information
/// </summary>
public class TestEnvironment : BaseEntity
{
    public string OperatingSystem { get; set; } = string.Empty;
    public string DotNetVersion { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public long TotalMemory { get; set; }
    public long AvailableMemory { get; set; }
    public int ProcessorCount { get; set; }
    public string BrowserVersion { get; set; } = string.Empty;
    public string DatabaseVersion { get; set; } = string.Empty;
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public Dictionary<string, string> ApplicationSettings { get; set; } = new();
    public List<InstalledPackage> Dependencies { get; set; } = new();
}

/// <summary>
/// Represents installed packages and dependencies
/// </summary>
public class InstalledPackage : BaseEntity
{
    public string PackageName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? InstallDate { get; set; }
}

/// <summary>
/// Represents test run results with detailed information
/// </summary>
public class TestRunResult : BaseEntity
{
    public Guid TestRunId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string TestClass { get; set; } = string.Empty;
    public string TestMethod { get; set; } = string.Empty;
    public TestResultStatus Status { get; set; } = TestResultStatus.NotRun;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public List<string> Tags { get; set; } = new();
    public TestCasePriority Priority { get; set; } = TestCasePriority.Medium;
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<TestStepResult> StepResults { get; set; } = new();
    public List<TestAsset> TestAssets { get; set; } = new();
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Represents performance metrics for test execution
/// </summary>
public class PerformanceMetrics : BaseEntity
{
    public TimeSpan TotalExecutionTime { get; set; }
    public TimeSpan AverageTestTime { get; set; }
    public double TestsPerSecond { get; set; }
    public long PeakMemoryUsage { get; set; }
    public long AverageMemoryUsage { get; set; }
    public double AverageCpuUsage { get; set; }
    public double PeakCpuUsage { get; set; }
    public int ThreadCount { get; set; }
    public int ParallelExecutionCount { get; set; }
    public Dictionary<string, TimeSpan> PhaseTimings { get; set; } = new();
    public List<ResourceUsageSample> ResourceSamples { get; set; } = new();
}

/// <summary>
/// Represents resource usage samples during test execution
/// </summary>
public class ResourceUsageSample : BaseEntity
{
    public DateTime Timestamp { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public int ThreadCount { get; set; }
    public long DiskUsage { get; set; }
    public long NetworkBytesReceived { get; set; }
    public long NetworkBytesSent { get; set; }
}

/// <summary>
/// Represents report configuration and formatting options
/// </summary>
public class ReportConfiguration : BaseEntity
{
    public string TemplateName { get; set; } = string.Empty;
    public ReportFormat OutputFormat { get; set; } = ReportFormat.HTML;
    public bool IncludeEnvironmentDetails { get; set; } = true;
    public bool IncludeStepDetails { get; set; } = true;
    public bool IncludeStackTraces { get; set; } = true;
    public bool IncludeScreenshots { get; set; } = true;
    public bool IncludePerformanceMetrics { get; set; } = true;
    public bool GroupByCategory { get; set; } = true;
    public bool ShowOnlyFailures { get; set; } = false;
    public int MaxLogLines { get; set; } = 1000;
    public List<string> CustomSections { get; set; } = new();
    public Dictionary<string, object> TemplateVariables { get; set; } = new();
    public ReportTheme Theme { get; set; } = ReportTheme.Default;
}

/// <summary>
/// Represents detailed logging information for test execution
/// </summary>
public class TestExecutionLog : BaseEntity
{
    public Guid TestRunId { get; set; }
    public Guid? TestResultId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LogLevel Level { get; set; } = LogLevel.Information;
    public string Logger { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public int ThreadId { get; set; }
    public string? StackTrace { get; set; }
}

/// <summary>
/// Represents structured log entry for complex scenarios
/// </summary>
public class StructuredLogEntry : BaseEntity
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public LogLevel Level { get; set; } = LogLevel.Information;
    public string Template { get; set; } = string.Empty;
    public string RenderedMessage { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
    public string? Exception { get; set; }
    public LogContext Context { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Represents logging context information
/// </summary>
public class LogContext : BaseEntity
{
    public string TestName { get; set; } = string.Empty;
    public string TestClass { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalContext { get; set; } = new();
}

/// <summary>
/// Represents a trend analysis report for test results over time
/// </summary>
public class TrendAnalysisReport : BaseEntity
{
    public string ReportName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime AnalysisStartDate { get; set; }
    public DateTime AnalysisEndDate { get; set; }
    public TimeSpan AnalysisPeriod { get; set; }
    public List<TrendDataPoint> TrendData { get; set; } = new();
    public TrendStatistics Statistics { get; set; } = new();
    public List<TrendAnomaly> Anomalies { get; set; } = new();
    public Dictionary<string, TrendMetric> Metrics { get; set; } = new();
}

/// <summary>
/// Represents a single data point in trend analysis
/// </summary>
public class TrendDataPoint : BaseEntity
{
    public DateTime Date { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public decimal SuccessRate { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Represents trend statistics and analysis
/// </summary>
public class TrendStatistics : BaseEntity
{
    public decimal AverageSuccessRate { get; set; }
    public decimal SuccessRateTrend { get; set; } // Positive = improving, Negative = declining
    public TimeSpan AverageExecutionTime { get; set; }
    public decimal ExecutionTimeTrend { get; set; }
    public int TotalExecutions { get; set; }
    public Dictionary<string, decimal> CategoryTrends { get; set; } = new();
    public List<string> ImprovingAreas { get; set; } = new();
    public List<string> DecliningAreas { get; set; } = new();
}

/// <summary>
/// Represents anomalies detected in trend analysis
/// </summary>
public class TrendAnomaly : BaseEntity
{
    public DateTime DetectedAt { get; set; }
    public AnomalyType Type { get; set; } = AnomalyType.Performance;
    public AnomalySeverity Severity { get; set; } = AnomalySeverity.Medium;
    public string Description { get; set; } = string.Empty;
    public decimal DeviationScore { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>
/// Represents custom trend metrics
/// </summary>
public class TrendMetric : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MetricType Type { get; set; } = MetricType.Counter;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal Change { get; set; }
    public decimal PercentageChange { get; set; }
    public TrendDirection Direction { get; set; } = TrendDirection.Stable;
    public List<MetricDataPoint> History { get; set; } = new();
}

/// <summary>
/// Represents a data point for metric history
/// </summary>
public class MetricDataPoint : BaseEntity
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

// Enums for Reporting Domain

public enum ReportType
{
    Standard,
    Detailed,
    Summary,
    Executive,
    Technical,
    Trend,
    Comparison,
    Custom
}

public enum ReportFormat
{
    JSON,
    XML,
    HTML,
    PDF,
    CSV,
    Excel,
    Markdown
}

public enum ReportTheme
{
    Default,
    Light,
    Dark,
    HighContrast,
    Corporate,
    Minimal
}

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

public enum AnomalyType
{
    Performance,
    SuccessRate,
    Duration,
    ErrorRate,
    Resource,
    Pattern
}

public enum AnomalySeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum MetricType
{
    Counter,
    Gauge,
    Histogram,
    Timer,
    Rate
}

public enum TrendDirection
{
    Improving,
    Stable,
    Declining,
    Volatile
}