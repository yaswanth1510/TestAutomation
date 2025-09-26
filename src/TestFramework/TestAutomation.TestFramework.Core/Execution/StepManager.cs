using Microsoft.Extensions.Logging;
using TestAutomation.Domain.Models;

namespace TestAutomation.TestFramework.Core.Execution;

/// <summary>
/// Step Manager - Manages test step execution and tracking
/// </summary>
public interface IStepManager
{
    Task<TestStepResult> ExecuteStepAsync(TestStep step, Dictionary<string, object> context);
    Task<IEnumerable<TestStepResult>> ExecuteStepsAsync(IEnumerable<TestStep> steps, Dictionary<string, object> context);
    void RegisterStepHandler(string stepType, IStepHandler handler);
    Task<bool> ValidateStepAsync(TestStep step);
}

/// <summary>
/// Interface for step handlers
/// </summary>
public interface IStepHandler
{
    string StepType { get; }
    Task<TestStepResult> ExecuteAsync(TestStep step, Dictionary<string, object> context);
    Task<bool> ValidateAsync(TestStep step);
}

/// <summary>
/// Implementation of Step Manager
/// </summary>
public class StepManager : IStepManager
{
    private readonly Dictionary<string, IStepHandler> _stepHandlers = new();
    private readonly ILogger<StepManager>? _logger;

    public StepManager(ILogger<StepManager>? logger = null)
    {
        _logger = logger;
        RegisterDefaultHandlers();
    }

    public async Task<TestStepResult> ExecuteStepAsync(TestStep step, Dictionary<string, object> context)
    {
        var stepResult = new TestStepResult
        {
            StepName = step.Name,
            StepOrder = step.Order,
            StartedAt = DateTime.UtcNow,
            ExpectedResult = step.ExpectedResult,
            Status = TestStepResultStatus.NotRun
        };

        try
        {
            _logger?.LogInformation("Executing step: {StepName} ({StepType})", step.Name, step.Type);

            var handler = GetStepHandler(step.Type.ToString());
            if (handler == null)
            {
                stepResult.Status = TestStepResultStatus.Failed;
                stepResult.ErrorMessage = $"No handler found for step type: {step.Type}";
                return stepResult;
            }

            stepResult = await handler.ExecuteAsync(step, context);
            stepResult.CompletedAt = DateTime.UtcNow;
            stepResult.Duration = stepResult.CompletedAt - stepResult.StartedAt;

            _logger?.LogInformation("Step completed: {StepName} - Status: {Status}", step.Name, stepResult.Status);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing step: {StepName}", step.Name);
            stepResult.Status = TestStepResultStatus.Failed;
            stepResult.ErrorMessage = ex.Message;
            stepResult.CompletedAt = DateTime.UtcNow;
            stepResult.Duration = stepResult.CompletedAt - stepResult.StartedAt;
        }

        return stepResult;
    }

    public async Task<IEnumerable<TestStepResult>> ExecuteStepsAsync(IEnumerable<TestStep> steps, Dictionary<string, object> context)
    {
        var results = new List<TestStepResult>();
        var sortedSteps = steps.OrderBy(s => s.Order).ToList();

        _logger?.LogInformation("Executing {Count} test steps", sortedSteps.Count);

        foreach (var step in sortedSteps)
        {
            var result = await ExecuteStepAsync(step, context);
            results.Add(result);

            // Stop execution on failure unless it's a cleanup step
            if (result.Status == TestStepResultStatus.Failed && step.Type != TestStepType.Cleanup)
            {
                _logger?.LogWarning("Step execution failed, stopping execution: {StepName}", step.Name);
                break;
            }
        }

        return results;
    }

    public void RegisterStepHandler(string stepType, IStepHandler handler)
    {
        _stepHandlers[stepType.ToLower()] = handler;
        _logger?.LogInformation("Registered step handler for type: {StepType}", stepType);
    }

    public async Task<bool> ValidateStepAsync(TestStep step)
    {
        try
        {
            var handler = GetStepHandler(step.Type.ToString());
            if (handler == null)
            {
                _logger?.LogWarning("No handler found for step type: {StepType}", step.Type);
                return false;
            }

            return await handler.ValidateAsync(step);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error validating step: {StepName}", step.Name);
            return false;
        }
    }

    private IStepHandler? GetStepHandler(string stepType)
    {
        _stepHandlers.TryGetValue(stepType.ToLower(), out var handler);
        return handler;
    }

    private void RegisterDefaultHandlers()
    {
        // Register built-in step handlers
        RegisterStepHandler("Action", new ActionStepHandler(_logger));
        RegisterStepHandler("Verification", new VerificationStepHandler(_logger));
        RegisterStepHandler("Setup", new SetupStepHandler(_logger));
        RegisterStepHandler("Cleanup", new CleanupStepHandler(_logger));
        RegisterStepHandler("Navigation", new NavigationStepHandler(_logger));
    }
}

/// <summary>
/// Base class for step handlers
/// </summary>
public abstract class BaseStepHandler : IStepHandler
{
    protected readonly ILogger? _logger;

    protected BaseStepHandler(ILogger? logger)
    {
        _logger = logger;
    }

    public abstract string StepType { get; }

    public virtual async Task<TestStepResult> ExecuteAsync(TestStep step, Dictionary<string, object> context)
    {
        var result = new TestStepResult
        {
            StepName = step.Name,
            StepOrder = step.Order,
            StartedAt = DateTime.UtcNow,
            ExpectedResult = step.ExpectedResult,
            Status = TestStepResultStatus.NotRun
        };

        try
        {
            result = await ExecuteStepLogicAsync(step, context, result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in step handler {StepType} for step {StepName}", StepType, step.Name);
            result.Status = TestStepResultStatus.Failed;
            result.ErrorMessage = ex.Message;
        }

        result.CompletedAt = DateTime.UtcNow;
        result.Duration = result.CompletedAt - result.StartedAt;
        return result;
    }

    public virtual async Task<bool> ValidateAsync(TestStep step)
    {
        try
        {
            return await ValidateStepLogicAsync(step);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error validating step in {StepType} handler", StepType);
            return false;
        }
    }

    protected abstract Task<TestStepResult> ExecuteStepLogicAsync(TestStep step, Dictionary<string, object> context, TestStepResult result);
    protected abstract Task<bool> ValidateStepLogicAsync(TestStep step);
}

/// <summary>
/// Handler for Action steps
/// </summary>
public class ActionStepHandler : BaseStepHandler
{
    public ActionStepHandler(ILogger? logger) : base(logger) { }

    public override string StepType => "Action";

    protected override async Task<TestStepResult> ExecuteStepLogicAsync(TestStep step, Dictionary<string, object> context, TestStepResult result)
    {
        // Simulate action execution
        await Task.Delay(100); // Simulate some work

        result.Status = TestStepResultStatus.Passed;
        result.ActualResult = $"Action '{step.Action}' executed successfully";
        
        _logger?.LogInformation("Executed action step: {Action}", step.Action);
        return result;
    }

    protected override async Task<bool> ValidateStepLogicAsync(TestStep step)
    {
        await Task.CompletedTask;
        return !string.IsNullOrEmpty(step.Action);
    }
}

/// <summary>
/// Handler for Verification steps
/// </summary>
public class VerificationStepHandler : BaseStepHandler
{
    public VerificationStepHandler(ILogger? logger) : base(logger) { }

    public override string StepType => "Verification";

    protected override async Task<TestStepResult> ExecuteStepLogicAsync(TestStep step, Dictionary<string, object> context, TestStepResult result)
    {
        await Task.Delay(50); // Simulate verification

        // Simple verification logic
        var expectedResult = step.ExpectedResult;
        var actualResult = context.ContainsKey("lastResult") ? context["lastResult"]?.ToString() : "No result";

        result.ActualResult = actualResult;
        result.Status = string.Equals(expectedResult, actualResult, StringComparison.OrdinalIgnoreCase) 
            ? TestStepResultStatus.Passed 
            : TestStepResultStatus.Failed;

        _logger?.LogInformation("Verified step: Expected='{Expected}', Actual='{Actual}', Status={Status}", 
            expectedResult, actualResult, result.Status);

        return result;
    }

    protected override async Task<bool> ValidateStepLogicAsync(TestStep step)
    {
        await Task.CompletedTask;
        return !string.IsNullOrEmpty(step.ExpectedResult);
    }
}

/// <summary>
/// Handler for Setup steps
/// </summary>
public class SetupStepHandler : BaseStepHandler
{
    public SetupStepHandler(ILogger? logger) : base(logger) { }

    public override string StepType => "Setup";

    protected override async Task<TestStepResult> ExecuteStepLogicAsync(TestStep step, Dictionary<string, object> context, TestStepResult result)
    {
        await Task.Delay(200); // Simulate setup work

        // Add setup data to context
        foreach (var param in step.Parameters)
        {
            context[param.Key] = param.Value;
        }

        result.Status = TestStepResultStatus.Passed;
        result.ActualResult = "Setup completed successfully";
        
        _logger?.LogInformation("Setup step completed: {StepName}", step.Name);
        return result;
    }

    protected override async Task<bool> ValidateStepLogicAsync(TestStep step)
    {
        await Task.CompletedTask;
        return true; // Setup steps are generally always valid
    }
}

/// <summary>
/// Handler for Cleanup steps
/// </summary>
public class CleanupStepHandler : BaseStepHandler
{
    public CleanupStepHandler(ILogger? logger) : base(logger) { }

    public override string StepType => "Cleanup";

    protected override async Task<TestStepResult> ExecuteStepLogicAsync(TestStep step, Dictionary<string, object> context, TestStepResult result)
    {
        await Task.Delay(100); // Simulate cleanup

        result.Status = TestStepResultStatus.Passed;
        result.ActualResult = "Cleanup completed successfully";
        
        _logger?.LogInformation("Cleanup step completed: {StepName}", step.Name);
        return result;
    }

    protected override async Task<bool> ValidateStepLogicAsync(TestStep step)
    {
        await Task.CompletedTask;
        return true; // Cleanup steps should always try to execute
    }
}

/// <summary>
/// Handler for Navigation steps
/// </summary>
public class NavigationStepHandler : BaseStepHandler
{
    public NavigationStepHandler(ILogger? logger) : base(logger) { }

    public override string StepType => "Navigation";

    protected override async Task<TestStepResult> ExecuteStepLogicAsync(TestStep step, Dictionary<string, object> context, TestStepResult result)
    {
        await Task.Delay(150); // Simulate navigation

        var url = step.Parameters.ContainsKey("url") ? step.Parameters["url"]?.ToString() : "";
        
        result.Status = TestStepResultStatus.Passed;
        result.ActualResult = $"Navigated to: {url}";
        
        _logger?.LogInformation("Navigation step completed: {Url}", url);
        return result;
    }

    protected override async Task<bool> ValidateStepLogicAsync(TestStep step)
    {
        await Task.CompletedTask;
        return step.Parameters.ContainsKey("url") && !string.IsNullOrEmpty(step.Parameters["url"]?.ToString());
    }
}