using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TestAutomation.Domain.Models;
using TestAutomation.Domain.Models.Reporting;

namespace TestAutomation.TestFramework.Core.Logging;

/// <summary>
/// Advanced Structured Logging Service for Test Automation
/// </summary>
public interface IAdvancedLoggingService
{
    Task LogAsync(Domain.Models.Reporting.LogLevel level, string message, object? state = null, Exception? exception = null);
    Task LogTestStartAsync(string testName, string testClass, Dictionary<string, object>? parameters = null);
    Task LogTestEndAsync(string testName, string testClass, TestResultStatus status, TimeSpan duration, string? errorMessage = null);
    Task LogStepAsync(string stepName, string stepType, string action, object? data = null);
    Task LogPerformanceAsync(string operation, TimeSpan duration, Dictionary<string, object>? metrics = null);
    Task LogExceptionAsync(Exception exception, string? context = null, Dictionary<string, object>? additionalData = null);
    Task<IEnumerable<StructuredLogEntry>> GetLogsAsync(DateTime? startTime = null, DateTime? endTime = null, Domain.Models.Reporting.LogLevel? minLevel = null);
    Task<IEnumerable<StructuredLogEntry>> SearchLogsAsync(string searchTerm, int maxResults = 100);
    Task PurgeOldLogsAsync(TimeSpan maxAge);
    IDisposable BeginScope(string name, Dictionary<string, object>? state = null);
}

/// <summary>
/// Implementation of Advanced Logging Service
/// </summary>
public class AdvancedLoggingService : IAdvancedLoggingService
{
    private readonly ILogger<AdvancedLoggingService> _logger;
    private readonly ConcurrentQueue<StructuredLogEntry> _logBuffer = new();
    private readonly ConcurrentStack<LogContext> _contextStack = new();
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
    private readonly Timer _flushTimer;
    private const int MAX_BUFFER_SIZE = 10000;
    private const int FLUSH_INTERVAL_MS = 5000;

    public AdvancedLoggingService(ILogger<AdvancedLoggingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _flushTimer = new Timer(FlushLogs, null, FLUSH_INTERVAL_MS, FLUSH_INTERVAL_MS);
    }

    public async Task LogAsync(Domain.Models.Reporting.LogLevel level, string message, object? state = null, Exception? exception = null)
    {
        var logEntry = new StructuredLogEntry
        {
            Level = level,
            Template = message,
            RenderedMessage = message,
            Source = GetCallingMethod(),
            Exception = exception?.ToString(),
            Context = GetCurrentContext()
        };

        if (state != null)
        {
            try
            {
                var stateDict = ConvertStateToProperties(state);
                foreach (var kvp in stateDict)
                {
                    logEntry.Properties[kvp.Key] = kvp.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to serialize log state");
            }
        }

        await AddLogEntryAsync(logEntry);
        
        // Also log to underlying logger
        _logger.LogInformation(exception, message, state);
    }

    public async Task LogTestStartAsync(string testName, string testClass, Dictionary<string, object>? parameters = null)
    {
        var logEntry = new StructuredLogEntry
        {
            Level = Domain.Models.Reporting.LogLevel.Information,
            Template = "Test {TestName} started in class {TestClass}",
            RenderedMessage = $"Test {testName} started in class {testClass}",
            Source = "TestExecution",
            Context = GetCurrentContext()
        };

        logEntry.Context.TestName = testName;
        logEntry.Context.TestClass = testClass;
        logEntry.Properties["TestName"] = testName;
        logEntry.Properties["TestClass"] = testClass;
        logEntry.Properties["EventType"] = "TestStart";
        
        logEntry.Tags.Add("TestExecution");
        logEntry.Tags.Add("TestStart");

        if (parameters != null)
        {
            logEntry.Properties["Parameters"] = parameters;
        }

        await AddLogEntryAsync(logEntry);
    }

    public async Task LogTestEndAsync(string testName, string testClass, TestResultStatus status, TimeSpan duration, string? errorMessage = null)
    {
        var logEntry = new StructuredLogEntry
        {
            Level = status == TestResultStatus.Failed ? Domain.Models.Reporting.LogLevel.Error : Domain.Models.Reporting.LogLevel.Information,
            Template = "Test {TestName} completed with status {Status} in {Duration}",
            RenderedMessage = $"Test {testName} completed with status {status} in {duration}",
            Source = "TestExecution",
            Context = GetCurrentContext()
        };

        logEntry.Context.TestName = testName;
        logEntry.Context.TestClass = testClass;
        logEntry.Properties["TestName"] = testName;
        logEntry.Properties["TestClass"] = testClass;
        logEntry.Properties["Status"] = status.ToString();
        logEntry.Properties["Duration"] = duration.ToString();
        logEntry.Properties["EventType"] = "TestEnd";

        logEntry.Tags.Add("TestExecution");
        logEntry.Tags.Add("TestEnd");
        logEntry.Tags.Add(status.ToString());

        if (!string.IsNullOrEmpty(errorMessage))
        {
            logEntry.Properties["ErrorMessage"] = errorMessage;
        }

        await AddLogEntryAsync(logEntry);
    }

    public async Task LogStepAsync(string stepName, string stepType, string action, object? data = null)
    {
        var logEntry = new StructuredLogEntry
        {
            Level = Domain.Models.Reporting.LogLevel.Debug,
            Template = "Step {StepName} of type {StepType} executing action {Action}",
            RenderedMessage = $"Step {stepName} of type {stepType} executing action {action}",
            Source = "StepExecution",
            Context = GetCurrentContext()
        };

        logEntry.Context.StepName = stepName;
        logEntry.Properties["StepName"] = stepName;
        logEntry.Properties["StepType"] = stepType;
        logEntry.Properties["Action"] = action;
        logEntry.Properties["EventType"] = "StepExecution";

        logEntry.Tags.Add("StepExecution");
        logEntry.Tags.Add(stepType);

        if (data != null)
        {
            logEntry.Properties["StepData"] = data;
        }

        await AddLogEntryAsync(logEntry);
    }

    public async Task LogPerformanceAsync(string operation, TimeSpan duration, Dictionary<string, object>? metrics = null)
    {
        var logEntry = new StructuredLogEntry
        {
            Level = Domain.Models.Reporting.LogLevel.Information,
            Template = "Performance: {Operation} completed in {Duration}",
            RenderedMessage = $"Performance: {operation} completed in {duration}",
            Source = "Performance",
            Context = GetCurrentContext()
        };

        logEntry.Properties["Operation"] = operation;
        logEntry.Properties["Duration"] = duration.ToString();
        logEntry.Properties["DurationMs"] = duration.TotalMilliseconds;
        logEntry.Properties["EventType"] = "Performance";

        logEntry.Tags.Add("Performance");

        if (metrics != null)
        {
            foreach (var metric in metrics)
            {
                logEntry.Properties[metric.Key] = metric.Value;
            }
        }

        await AddLogEntryAsync(logEntry);
    }

    public async Task LogExceptionAsync(Exception exception, string? context = null, Dictionary<string, object>? additionalData = null)
    {
        var logEntry = new StructuredLogEntry
        {
            Level = Domain.Models.Reporting.LogLevel.Error,
            Template = "Exception occurred: {ExceptionType} - {ExceptionMessage}",
            RenderedMessage = $"Exception occurred: {exception.GetType().Name} - {exception.Message}",
            Source = "ExceptionHandler",
            Exception = exception.ToString(),
            Context = GetCurrentContext()
        };

        logEntry.Properties["ExceptionType"] = exception.GetType().Name;
        logEntry.Properties["ExceptionMessage"] = exception.Message;
        logEntry.Properties["EventType"] = "Exception";

        if (!string.IsNullOrEmpty(context))
        {
            logEntry.Properties["ExceptionContext"] = context;
        }

        if (additionalData != null)
        {
            foreach (var data in additionalData)
            {
                logEntry.Properties[data.Key] = data.Value;
            }
        }

        logEntry.Tags.Add("Exception");
        logEntry.Tags.Add("Error");

        await AddLogEntryAsync(logEntry);
    }

    public async Task<IEnumerable<StructuredLogEntry>> GetLogsAsync(DateTime? startTime = null, DateTime? endTime = null, Domain.Models.Reporting.LogLevel? minLevel = null)
    {
        await Task.CompletedTask;
        
        var logs = _logBuffer.ToArray().AsEnumerable();

        if (startTime.HasValue)
        {
            logs = logs.Where(l => l.Timestamp >= startTime.Value);
        }

        if (endTime.HasValue)
        {
            logs = logs.Where(l => l.Timestamp <= endTime.Value);
        }

        if (minLevel.HasValue)
        {
            logs = logs.Where(l => l.Level >= minLevel.Value);
        }

        return logs.OrderByDescending(l => l.Timestamp);
    }

    public async Task<IEnumerable<StructuredLogEntry>> SearchLogsAsync(string searchTerm, int maxResults = 100)
    {
        await Task.CompletedTask;
        
        var logs = _logBuffer.ToArray()
            .Where(l => 
                l.RenderedMessage.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                l.Properties.Values.Any(v => v?.ToString()?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                l.Tags.Any(t => t.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(l => l.Timestamp)
            .Take(maxResults);

        return logs;
    }

    public async Task PurgeOldLogsAsync(TimeSpan maxAge)
    {
        await _writeSemaphore.WaitAsync();
        
        try
        {
            var cutoffTime = DateTime.UtcNow - maxAge;
            var logsToKeep = new ConcurrentQueue<StructuredLogEntry>();
            
            while (_logBuffer.TryDequeue(out var log))
            {
                if (log.Timestamp >= cutoffTime)
                {
                    logsToKeep.Enqueue(log);
                }
            }

            // Replace the old queue with the filtered one
            while (logsToKeep.TryDequeue(out var log))
            {
                _logBuffer.Enqueue(log);
            }

            _logger.LogInformation("Purged old logs older than {MaxAge}", maxAge);
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    public IDisposable BeginScope(string name, Dictionary<string, object>? state = null)
    {
        var context = new LogContext
        {
            SessionId = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid().ToString()
        };

        if (state != null)
        {
            foreach (var kvp in state)
            {
                context.AdditionalContext[kvp.Key] = kvp.Value;
            }
        }

        _contextStack.Push(context);
        
        return new LoggingScope(this, context);
    }

    private async Task AddLogEntryAsync(StructuredLogEntry logEntry)
    {
        _logBuffer.Enqueue(logEntry);

        // Ensure we don't exceed buffer size
        while (_logBuffer.Count > MAX_BUFFER_SIZE)
        {
            _logBuffer.TryDequeue(out _);
        }

        // If the buffer is getting full or we have critical errors, flush immediately
        if (_logBuffer.Count > MAX_BUFFER_SIZE * 0.8 || logEntry.Level >= Domain.Models.Reporting.LogLevel.Error)
        {
            await FlushLogsAsync();
        }
    }

    private LogContext GetCurrentContext()
    {
        if (_contextStack.TryPeek(out var context))
        {
            return context;
        }

        return new LogContext();
    }

    private string GetCallingMethod()
    {
        var stackTrace = new StackTrace(skipFrames: 2, fNeedFileInfo: false);
        var frame = stackTrace.GetFrame(0);
        var method = frame?.GetMethod();
        
        if (method != null)
        {
            return $"{method.DeclaringType?.Name}.{method.Name}";
        }

        return "Unknown";
    }

    private Dictionary<string, object> ConvertStateToProperties(object state)
    {
        var properties = new Dictionary<string, object>();

        if (state is Dictionary<string, object> dict)
        {
            return dict;
        }

        if (state is IEnumerable<KeyValuePair<string, object>> kvps)
        {
            foreach (var kvp in kvps)
            {
                properties[kvp.Key] = kvp.Value;
            }
            return properties;
        }

        // Try to serialize as JSON and parse back
        try
        {
            var json = JsonSerializer.Serialize(state);
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    properties[prop.Name] = prop.Value.ToString();
                }
            }
        }
        catch
        {
            properties["State"] = state.ToString() ?? "null";
        }

        return properties;
    }

    private void FlushLogs(object? state)
    {
        _ = Task.Run(async () => await FlushLogsAsync());
    }

    private async Task FlushLogsAsync()
    {
        if (_logBuffer.IsEmpty) return;

        await _writeSemaphore.WaitAsync();
        
        try
        {
            // In a real implementation, this would write to persistent storage
            // For now, we'll just ensure the underlying logger gets the messages
            
            var logsToFlush = new List<StructuredLogEntry>();
            while (_logBuffer.TryDequeue(out var log) && logsToFlush.Count < 100)
            {
                logsToFlush.Add(log);
            }

            foreach (var log in logsToFlush)
            {
                _logger.LogInformation(log.Exception != null ? new Exception(log.Exception) : null, 
                    log.RenderedMessage);
            }
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    private void EndScope(LogContext context)
    {
        _contextStack.TryPop(out _);
    }

    private class LoggingScope : IDisposable
    {
        private readonly AdvancedLoggingService _loggingService;
        private readonly LogContext _context;
        private bool _disposed = false;

        public LoggingScope(AdvancedLoggingService loggingService, LogContext context)
        {
            _loggingService = loggingService;
            _context = context;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _loggingService.EndScope(_context);
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Performance Monitoring Service for Test Automation
/// </summary>
public interface IPerformanceMonitoringService
{
    Task<IDisposable> StartOperationAsync(string operationName, Dictionary<string, object>? metadata = null);
    Task RecordMetricAsync(string metricName, double value, string? unit = null, Dictionary<string, object>? tags = null);
    Task RecordCounterAsync(string counterName, long value = 1, Dictionary<string, object>? tags = null);
    Task<PerformanceReport> GenerateReportAsync(TimeSpan period);
    Task<IEnumerable<PerformanceMetricSample>> GetMetricsAsync(string metricName, DateTime? startTime = null, DateTime? endTime = null);
}

/// <summary>
/// Implementation of Performance Monitoring Service
/// </summary>
public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly IAdvancedLoggingService _loggingService;
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly ConcurrentDictionary<string, List<PerformanceMetricSample>> _metrics = new();
    private readonly ConcurrentDictionary<string, long> _counters = new();

    public PerformanceMonitoringService(IAdvancedLoggingService loggingService, ILogger<PerformanceMonitoringService> logger)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IDisposable> StartOperationAsync(string operationName, Dictionary<string, object>? metadata = null)
    {
        await _loggingService.LogPerformanceAsync($"Operation {operationName} started", TimeSpan.Zero, metadata);
        return new PerformanceOperation(this, operationName, metadata ?? new Dictionary<string, object>());
    }

    public async Task RecordMetricAsync(string metricName, double value, string? unit = null, Dictionary<string, object>? tags = null)
    {
        var sample = new PerformanceMetricSample
        {
            MetricName = metricName,
            Value = value,
            Unit = unit ?? "count",
            Timestamp = DateTime.UtcNow,
            Tags = tags ?? new Dictionary<string, object>()
        };

        _metrics.AddOrUpdate(metricName, 
            new List<PerformanceMetricSample> { sample },
            (key, existing) =>
            {
                existing.Add(sample);
                // Keep only recent samples to prevent memory issues
                if (existing.Count > 10000)
                {
                    existing.RemoveRange(0, existing.Count - 10000);
                }
                return existing;
            });

        await _loggingService.LogPerformanceAsync($"Metric {metricName}", TimeSpan.Zero, new Dictionary<string, object>
        {
            ["MetricName"] = metricName,
            ["Value"] = value,
            ["Unit"] = unit ?? "count"
        });
    }

    public async Task RecordCounterAsync(string counterName, long value = 1, Dictionary<string, object>? tags = null)
    {
        _counters.AddOrUpdate(counterName, value, (key, existing) => existing + value);
        
        await _loggingService.LogPerformanceAsync($"Counter {counterName}", TimeSpan.Zero, new Dictionary<string, object>
        {
            ["CounterName"] = counterName,
            ["Value"] = value,
            ["TotalValue"] = _counters[counterName]
        });
    }

    public async Task<PerformanceReport> GenerateReportAsync(TimeSpan period)
    {
        await Task.CompletedTask;
        
        var endTime = DateTime.UtcNow;
        var startTime = endTime - period;
        
        var report = new PerformanceReport
        {
            ReportName = $"Performance Report - {period}",
            StartTime = startTime,
            EndTime = endTime,
            Period = period
        };

        // Aggregate metrics
        foreach (var metricGroup in _metrics)
        {
            var samples = metricGroup.Value
                .Where(s => s.Timestamp >= startTime && s.Timestamp <= endTime)
                .ToList();

            if (samples.Any())
            {
                var summary = new MetricSummary
                {
                    MetricName = metricGroup.Key,
                    SampleCount = samples.Count,
                    MinValue = samples.Min(s => s.Value),
                    MaxValue = samples.Max(s => s.Value),
                    AverageValue = samples.Average(s => s.Value),
                    TotalValue = samples.Sum(s => s.Value),
                    Unit = samples.First().Unit
                };

                report.MetricSummaries.Add(summary);
            }
        }

        // Add counters
        foreach (var counter in _counters)
        {
            report.Counters[counter.Key] = counter.Value;
        }

        return report;
    }

    public async Task<IEnumerable<PerformanceMetricSample>> GetMetricsAsync(string metricName, DateTime? startTime = null, DateTime? endTime = null)
    {
        await Task.CompletedTask;
        
        if (!_metrics.TryGetValue(metricName, out var samples))
        {
            return Enumerable.Empty<PerformanceMetricSample>();
        }

        var filteredSamples = samples.AsEnumerable();

        if (startTime.HasValue)
        {
            filteredSamples = filteredSamples.Where(s => s.Timestamp >= startTime.Value);
        }

        if (endTime.HasValue)
        {
            filteredSamples = filteredSamples.Where(s => s.Timestamp <= endTime.Value);
        }

        return filteredSamples.OrderBy(s => s.Timestamp);
    }

    private class PerformanceOperation : IDisposable
    {
        private readonly PerformanceMonitoringService _service;
        private readonly string _operationName;
        private readonly Dictionary<string, object> _metadata;
        private readonly Stopwatch _stopwatch;
        private bool _disposed = false;

        public PerformanceOperation(PerformanceMonitoringService service, string operationName, Dictionary<string, object> metadata)
        {
            _service = service;
            _operationName = operationName;
            _metadata = metadata;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                var duration = _stopwatch.Elapsed;
                
                _ = Task.Run(async () =>
                {
                    await _service._loggingService.LogPerformanceAsync(_operationName, duration, _metadata);
                    await _service.RecordMetricAsync($"{_operationName}.Duration", duration.TotalMilliseconds, "ms", _metadata);
                });

                _disposed = true;
            }
        }
    }
}

// Supporting classes for performance monitoring

public class PerformanceMetricSample
{
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Tags { get; set; } = new();
}

public class PerformanceReport
{
    public string ReportName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Period { get; set; }
    public List<MetricSummary> MetricSummaries { get; set; } = new();
    public Dictionary<string, long> Counters { get; set; } = new();
}

public class MetricSummary
{
    public string MetricName { get; set; } = string.Empty;
    public int SampleCount { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double AverageValue { get; set; }
    public double TotalValue { get; set; }
    public string Unit { get; set; } = string.Empty;
}