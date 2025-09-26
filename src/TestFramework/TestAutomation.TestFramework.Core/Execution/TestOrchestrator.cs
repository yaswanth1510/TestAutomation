using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TestAutomation.Domain.Models;
using TestAutomation.Domain.Models.Configuration;
using TestAutomation.Domain.Models.Reporting;
using TestAutomation.TestFramework.Core.Assets;
using TestAutomation.TestFramework.Core.Discovery;

namespace TestAutomation.TestFramework.Core.Execution;

/// <summary>
/// Advanced Test Execution Orchestrator - Fully asynchronous test execution with parallel support
/// </summary>
public interface ITestOrchestrator
{
    Task<TestRun> ExecuteTestSuiteAsync(TestSuite testSuite, TestExecutionConfiguration configuration, CancellationToken cancellationToken = default);
    Task<TestRun> ExecuteTestCasesAsync(IEnumerable<TestCase> testCases, TestExecutionConfiguration configuration, CancellationToken cancellationToken = default);
    Task<TestResult> ExecuteTestCaseAsync(TestCase testCase, TestExecutionConfiguration configuration, CancellationToken cancellationToken = default);
    Task<bool> PauseExecutionAsync(Guid testRunId);
    Task<bool> ResumeExecutionAsync(Guid testRunId);
    Task<bool> CancelExecutionAsync(Guid testRunId);
    Task<TestRunStatus> GetExecutionStatusAsync(Guid testRunId);
    IAsyncEnumerable<TestExecutionEvent> SubscribeToExecutionEventsAsync(Guid testRunId);
}

/// <summary>
/// Implementation of the Test Execution Orchestrator
/// </summary>
public class TestOrchestrator : ITestOrchestrator, IDisposable
{
    private readonly IStepManager _stepManager;
    private readonly IAssetManager _assetManager;
    private readonly ILogger<TestOrchestrator> _logger;
    private readonly ConcurrentDictionary<Guid, TestRunContext> _activeRuns = new();
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly Channel<TestExecutionEvent> _eventChannel;
    private readonly ChannelWriter<TestExecutionEvent> _eventWriter;
    private readonly ChannelReader<TestExecutionEvent> _eventReader;
    private bool _disposed = false;

    public TestOrchestrator(
        IStepManager stepManager,
        IAssetManager assetManager,
        ILogger<TestOrchestrator> logger)
    {
        _stepManager = stepManager ?? throw new ArgumentNullException(nameof(stepManager));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _concurrencyLimiter = new SemaphoreSlim(Environment.ProcessorCount * 2);
        
        var channelOptions = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        
        var channel = Channel.CreateBounded<TestExecutionEvent>(channelOptions);
        _eventChannel = channel;
        _eventWriter = channel.Writer;
        _eventReader = channel.Reader;
    }

    public async Task<TestRun> ExecuteTestSuiteAsync(TestSuite testSuite, TestExecutionConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var testRun = new TestRun
        {
            Name = $"Execution of {testSuite.Name}",
            StartedAt = DateTime.UtcNow,
            Status = TestRunStatus.Running,
            TotalTests = testSuite.TestCases.Count,
            Configuration = configuration.CustomParameters
        };

        var context = new TestRunContext(testRun, configuration, cancellationToken);
        _activeRuns[testRun.Id] = context;

        try
        {
            _logger.LogInformation("Starting test suite execution: {TestSuite} with {TestCount} tests", 
                testSuite.Name, testSuite.TestCases.Count);

            await PublishEventAsync(new TestExecutionEvent
            {
                Type = TestExecutionEventType.TestRunStarted,
                TestRunId = testRun.Id,
                Timestamp = DateTime.UtcNow,
                Data = new { TestSuite = testSuite.Name, TestCount = testSuite.TestCases.Count }
            });

            var stopwatch = Stopwatch.StartNew();

            if (configuration.Mode == ExecutionMode.Sequential)
            {
                await ExecuteSequentialAsync(testSuite.TestCases, context);
            }
            else if (configuration.Mode == ExecutionMode.Parallel)
            {
                await ExecuteParallelAsync(testSuite.TestCases, context, configuration.MaxParallelTests);
            }
            else
            {
                throw new NotSupportedException($"Execution mode {configuration.Mode} is not yet supported");
            }

            stopwatch.Stop();

            testRun.CompletedAt = DateTime.UtcNow;
            testRun.Duration = stopwatch.Elapsed;
            testRun.Status = context.IsCancelled ? TestRunStatus.Cancelled : 
                           testRun.FailedTests > 0 ? TestRunStatus.Failed : TestRunStatus.Completed;

            _logger.LogInformation("Test suite execution completed: {TestSuite} - {Status} in {Duration}", 
                testSuite.Name, testRun.Status, testRun.Duration);

            await PublishEventAsync(new TestExecutionEvent
            {
                Type = TestExecutionEventType.TestRunCompleted,
                TestRunId = testRun.Id,
                Timestamp = DateTime.UtcNow,
                Data = new { Status = testRun.Status, Duration = testRun.Duration, Results = new { testRun.PassedTests, testRun.FailedTests, testRun.SkippedTests } }
            });
        }
        catch (OperationCanceledException)
        {
            testRun.Status = TestRunStatus.Cancelled;
            testRun.CompletedAt = DateTime.UtcNow;
            _logger.LogWarning("Test suite execution was cancelled: {TestSuite}", testSuite.Name);
        }
        catch (Exception ex)
        {
            testRun.Status = TestRunStatus.Failed;
            testRun.CompletedAt = DateTime.UtcNow;
            testRun.ErrorMessage = ex.Message;
            testRun.StackTrace = ex.StackTrace;
            
            _logger.LogError(ex, "Test suite execution failed: {TestSuite}", testSuite.Name);

            await PublishEventAsync(new TestExecutionEvent
            {
                Type = TestExecutionEventType.TestRunFailed,
                TestRunId = testRun.Id,
                Timestamp = DateTime.UtcNow,
                Data = new { Error = ex.Message, StackTrace = ex.StackTrace }
            });
        }
        finally
        {
            _activeRuns.TryRemove(testRun.Id, out _);
        }

        return testRun;
    }

    public async Task<TestRun> ExecuteTestCasesAsync(IEnumerable<TestCase> testCases, TestExecutionConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var testCaseList = testCases.ToList();
        var testRun = new TestRun
        {
            Name = "Custom Test Execution",
            StartedAt = DateTime.UtcNow,
            Status = TestRunStatus.Running,
            TotalTests = testCaseList.Count,
            Configuration = configuration.CustomParameters
        };

        var context = new TestRunContext(testRun, configuration, cancellationToken);
        _activeRuns[testRun.Id] = context;

        try
        {
            if (configuration.Mode == ExecutionMode.Sequential)
            {
                await ExecuteSequentialAsync(testCaseList, context);
            }
            else
            {
                await ExecuteParallelAsync(testCaseList, context, configuration.MaxParallelTests);
            }

            testRun.Status = context.IsCancelled ? TestRunStatus.Cancelled : 
                           testRun.FailedTests > 0 ? TestRunStatus.Failed : TestRunStatus.Completed;
        }
        catch (Exception ex)
        {
            testRun.Status = TestRunStatus.Failed;
            testRun.ErrorMessage = ex.Message;
            testRun.StackTrace = ex.StackTrace;
        }
        finally
        {
            testRun.CompletedAt = DateTime.UtcNow;
            _activeRuns.TryRemove(testRun.Id, out _);
        }

        return testRun;
    }

    public async Task<TestResult> ExecuteTestCaseAsync(TestCase testCase, TestExecutionConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var testResult = new TestResult
        {
            TestName = testCase.Name,
            TestClass = testCase.ClassName,
            StartedAt = DateTime.UtcNow,
            Status = TestResultStatus.NotRun,
            TestCaseId = testCase.Id
        };

        try
        {
            _logger.LogInformation("Executing test case: {TestName}", testCase.Name);

            var stopwatch = Stopwatch.StartNew();

            // Execute test steps
            var context = new Dictionary<string, object>(testCase.Parameters);
            var stepResults = await _stepManager.ExecuteStepsAsync(testCase.Steps, context);
            
            testResult.StepResults.AddRange(stepResults);

            stopwatch.Stop();
            testResult.CompletedAt = DateTime.UtcNow;
            testResult.Duration = stopwatch.Elapsed;

            // Determine test result status
            var failedSteps = stepResults.Where(sr => sr.Status == TestStepResultStatus.Failed).ToList();
            if (failedSteps.Any())
            {
                testResult.Status = TestResultStatus.Failed;
                testResult.ErrorMessage = string.Join("; ", failedSteps.Select(fs => fs.ErrorMessage));
            }
            else
            {
                testResult.Status = TestResultStatus.Passed;
            }

            // Capture assets if configured
            if (configuration.CaptureScreenshots)
            {
                try
                {
                    var screenshot = await _assetManager.CaptureScreenshotAsync(
                        $"{testCase.Name}_result", 
                        $"Final screenshot for test: {testCase.Name}");
                    testResult.Assets.Add(screenshot);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to capture screenshot for test: {TestName}", testCase.Name);
                }
            }

            _logger.LogInformation("Test case completed: {TestName} - {Status} in {Duration}", 
                testCase.Name, testResult.Status, testResult.Duration);
        }
        catch (OperationCanceledException)
        {
            testResult.Status = TestResultStatus.Skipped;
            testResult.CompletedAt = DateTime.UtcNow;
            _logger.LogWarning("Test case execution was cancelled: {TestName}", testCase.Name);
        }
        catch (Exception ex)
        {
            testResult.Status = TestResultStatus.Failed;
            testResult.CompletedAt = DateTime.UtcNow;
            testResult.ErrorMessage = ex.Message;
            testResult.StackTrace = ex.StackTrace;
            
            _logger.LogError(ex, "Test case execution failed: {TestName}", testCase.Name);
        }

        return testResult;
    }

    public async Task<bool> PauseExecutionAsync(Guid testRunId)
    {
        if (_activeRuns.TryGetValue(testRunId, out var context))
        {
            context.IsPaused = true;
            await PublishEventAsync(new TestExecutionEvent
            {
                Type = TestExecutionEventType.TestRunPaused,
                TestRunId = testRunId,
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogInformation("Test run paused: {TestRunId}", testRunId);
            return true;
        }
        return false;
    }

    public async Task<bool> ResumeExecutionAsync(Guid testRunId)
    {
        if (_activeRuns.TryGetValue(testRunId, out var context))
        {
            context.IsPaused = false;
            await PublishEventAsync(new TestExecutionEvent
            {
                Type = TestExecutionEventType.TestRunResumed,
                TestRunId = testRunId,
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogInformation("Test run resumed: {TestRunId}", testRunId);
            return true;
        }
        return false;
    }

    public async Task<bool> CancelExecutionAsync(Guid testRunId)
    {
        if (_activeRuns.TryGetValue(testRunId, out var context))
        {
            context.CancellationTokenSource.Cancel();
            await PublishEventAsync(new TestExecutionEvent
            {
                Type = TestExecutionEventType.TestRunCancelled,
                TestRunId = testRunId,
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogInformation("Test run cancelled: {TestRunId}", testRunId);
            return true;
        }
        return false;
    }

    public async Task<TestRunStatus> GetExecutionStatusAsync(Guid testRunId)
    {
        await Task.CompletedTask;
        if (_activeRuns.TryGetValue(testRunId, out var context))
        {
            if (context.IsCancelled) return TestRunStatus.Cancelled;
            if (context.IsPaused) return TestRunStatus.Running; // Still running but paused
            return TestRunStatus.Running;
        }
        return TestRunStatus.NotStarted;
    }

    public async IAsyncEnumerable<TestExecutionEvent> SubscribeToExecutionEventsAsync(Guid testRunId)
    {
        await foreach (var eventItem in _eventReader.ReadAllAsync())
        {
            if (eventItem.TestRunId == testRunId)
            {
                yield return eventItem;
                
                // Stop if test run is completed
                if (eventItem.Type == TestExecutionEventType.TestRunCompleted ||
                    eventItem.Type == TestExecutionEventType.TestRunFailed ||
                    eventItem.Type == TestExecutionEventType.TestRunCancelled)
                {
                    break;
                }
            }
        }
    }

    private async Task ExecuteSequentialAsync(IList<TestCase> testCases, TestRunContext context)
    {
        foreach (var testCase in testCases)
        {
            if (context.IsCancelled) break;

            // Wait if paused
            while (context.IsPaused && !context.IsCancelled)
            {
                await Task.Delay(100, context.CancellationToken);
            }

            if (context.IsCancelled) break;

            var result = await ExecuteTestCaseAsync(testCase, context.Configuration, context.CancellationToken);
            context.TestRun.TestResults.Add(result);
            
            UpdateTestRunCounts(context.TestRun, result);

            await PublishEventAsync(new TestExecutionEvent
            {
                Type = TestExecutionEventType.TestCaseCompleted,
                TestRunId = context.TestRun.Id,
                Timestamp = DateTime.UtcNow,
                Data = new { TestName = testCase.Name, Status = result.Status, Duration = result.Duration }
            });
        }
    }

    private async Task ExecuteParallelAsync(IList<TestCase> testCases, TestRunContext context, int maxParallelism)
    {
        var semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);
        var tasks = new List<Task>();

        foreach (var testCase in testCases)
        {
            if (context.IsCancelled) break;

            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync(context.CancellationToken);
                try
                {
                    // Wait if paused
                    while (context.IsPaused && !context.IsCancelled)
                    {
                        await Task.Delay(100, context.CancellationToken);
                    }

                    if (context.IsCancelled) return;

                    var result = await ExecuteTestCaseAsync(testCase, context.Configuration, context.CancellationToken);
                    
                    lock (context.TestRun)
                    {
                        context.TestRun.TestResults.Add(result);
                        UpdateTestRunCounts(context.TestRun, result);
                    }

                    await PublishEventAsync(new TestExecutionEvent
                    {
                        Type = TestExecutionEventType.TestCaseCompleted,
                        TestRunId = context.TestRun.Id,
                        Timestamp = DateTime.UtcNow,
                        Data = new { TestName = testCase.Name, Status = result.Status, Duration = result.Duration }
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }, context.CancellationToken);

            tasks.Add(task);
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    private static void UpdateTestRunCounts(TestRun testRun, TestResult result)
    {
        switch (result.Status)
        {
            case TestResultStatus.Passed:
                testRun.PassedTests++;
                break;
            case TestResultStatus.Failed:
                testRun.FailedTests++;
                break;
            case TestResultStatus.Skipped:
                testRun.SkippedTests++;
                break;
        }
    }

    private async Task PublishEventAsync(TestExecutionEvent eventItem)
    {
        try
        {
            await _eventWriter.WriteAsync(eventItem);
        }
        catch (InvalidOperationException)
        {
            // Channel might be closed
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _concurrencyLimiter?.Dispose();
            _eventWriter?.Complete();
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents the context of a running test execution
/// </summary>
public class TestRunContext
{
    public TestRun TestRun { get; }
    public TestExecutionConfiguration Configuration { get; }
    public CancellationTokenSource CancellationTokenSource { get; }
    public CancellationToken CancellationToken { get; }
    public bool IsPaused { get; set; }
    public bool IsCancelled => CancellationTokenSource.Token.IsCancellationRequested;

    public TestRunContext(TestRun testRun, TestExecutionConfiguration configuration, CancellationToken parentToken)
    {
        TestRun = testRun;
        Configuration = configuration;
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        CancellationToken = CancellationTokenSource.Token;
    }
}

/// <summary>
/// Represents events that occur during test execution
/// </summary>
public class TestExecutionEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TestExecutionEventType Type { get; set; }
    public Guid TestRunId { get; set; }
    public DateTime Timestamp { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// Types of test execution events
/// </summary>
public enum TestExecutionEventType
{
    TestRunStarted,
    TestRunCompleted,
    TestRunFailed,
    TestRunPaused,
    TestRunResumed,
    TestRunCancelled,
    TestCaseStarted,
    TestCaseCompleted,
    TestCaseFailed,
    TestCaseSkipped,
    TestStepStarted,
    TestStepCompleted,
    TestStepFailed,
    AssetGenerated,
    ErrorOccurred
}