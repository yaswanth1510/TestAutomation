namespace TestAutomation.Domain.Models;

/// <summary>
/// Represents a test suite in the system
/// </summary>
public class TestSuite : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public TestSuiteStatus Status { get; set; } = TestSuiteStatus.Draft;
    public List<TestCase> TestCases { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Represents an individual test case
/// </summary>
public class TestCase : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public TestCasePriority Priority { get; set; } = TestCasePriority.Medium;
    public TimeSpan EstimatedDuration { get; set; } = TimeSpan.Zero;
    public List<TestStep> Steps { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Guid TestSuiteId { get; set; }
    public TestSuite? TestSuite { get; set; }
}

/// <summary>
/// Represents a test step within a test case
/// </summary>
public class TestStep : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public TestStepType Type { get; set; } = TestStepType.Action;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string ExpectedResult { get; set; } = string.Empty;
    public Guid TestCaseId { get; set; }
    public TestCase? TestCase { get; set; }
}

/// <summary>
/// Represents a test execution run
/// </summary>
public class TestRun : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public TestRunStatus Status { get; set; } = TestRunStatus.NotStarted;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
    public List<TestResult> TestResults { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
}

/// <summary>
/// Represents the result of a test execution
/// </summary>
public class TestResult : BaseEntity
{
    public string TestName { get; set; } = string.Empty;
    public string TestClass { get; set; } = string.Empty;
    public TestResultStatus Status { get; set; } = TestResultStatus.NotRun;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public List<TestStepResult> StepResults { get; set; } = new();
    public List<TestAsset> Assets { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Guid TestRunId { get; set; }
    public TestRun? TestRun { get; set; }
    public Guid TestCaseId { get; set; }
    public TestCase? TestCase { get; set; }
}

/// <summary>
/// Represents the result of a test step execution
/// </summary>
public class TestStepResult : BaseEntity
{
    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public TestStepResultStatus Status { get; set; } = TestStepResultStatus.NotRun;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ActualResult { get; set; }
    public string? ExpectedResult { get; set; }
    public string? ErrorMessage { get; set; }
    public List<TestAsset> Assets { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
    public Guid TestResultId { get; set; }
    public TestResult? TestResult { get; set; }
}

/// <summary>
/// Represents test assets like screenshots, logs, files
/// </summary>
public class TestAsset : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public TestAssetType Type { get; set; } = TestAssetType.File;
    public string? Description { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

// Enums
public enum TestSuiteStatus
{
    Draft,
    Active,
    Archived,
    Disabled
}

public enum TestCasePriority
{
    Critical,
    High,
    Medium,
    Low
}

public enum TestStepType
{
    Action,
    Verification,
    Setup,
    Cleanup,
    Navigation
}

public enum TestRunStatus
{
    NotStarted,
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
    TimedOut
}

public enum TestResultStatus
{
    NotRun,
    Passed,
    Failed,
    Skipped,
    Inconclusive
}

public enum TestStepResultStatus
{
    NotRun,
    Passed,
    Failed,
    Skipped,
    Warning
}

public enum TestAssetType
{
    Screenshot,
    Video,
    Log,
    File,
    Report,
    Data
}