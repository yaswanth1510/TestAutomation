using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using TestAutomation.Domain.Models;

namespace TestAutomation.TestFramework.Core.Discovery;

/// <summary>
/// Smart Test and Script Collector - Automatically discovers and organizes tests and scripts
/// </summary>
public interface ITestDiscoveryService
{
    Task<IEnumerable<TestSuite>> DiscoverTestSuitesAsync(string testDirectory);
    Task<IEnumerable<TestCase>> DiscoverTestCasesAsync(string assemblyPath);
    Task<TestCase?> GetTestCaseDetailsAsync(string assemblyPath, string className, string methodName);
    Task<IEnumerable<string>> GetAvailableTagsAsync(string testDirectory);
}

/// <summary>
/// Implementation of the Test Discovery Service
/// </summary>
public class TestDiscoveryService : ITestDiscoveryService
{
    private readonly ILogger<TestDiscoveryService>? _logger;
    
    public TestDiscoveryService(ILogger<TestDiscoveryService>? logger = null)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<TestSuite>> DiscoverTestSuitesAsync(string testDirectory)
    {
        var testSuites = new List<TestSuite>();
        
        try
        {
            if (!Directory.Exists(testDirectory))
            {
                _logger?.LogWarning("Test directory does not exist: {TestDirectory}", testDirectory);
                return testSuites;
            }

            // Discover test assemblies
            var testAssemblies = Directory.GetFiles(testDirectory, "*.dll", SearchOption.AllDirectories)
                .Where(f => f.Contains("Test") && !f.Contains("TestFramework"))
                .ToList();

            foreach (var assemblyPath in testAssemblies)
            {
                var testSuite = await CreateTestSuiteFromAssemblyAsync(assemblyPath);
                if (testSuite != null)
                {
                    testSuites.Add(testSuite);
                }
            }

            _logger?.LogInformation("Discovered {Count} test suites from {Directory}", testSuites.Count, testDirectory);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error discovering test suites from {Directory}", testDirectory);
        }

        return testSuites;
    }

    public async Task<IEnumerable<TestCase>> DiscoverTestCasesAsync(string assemblyPath)
    {
        var testCases = new List<TestCase>();
        
        try
        {
            var assembly = await LoadAssemblyAsync(assemblyPath);
            if (assembly == null) return testCases;

            var testClasses = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && HasTestMethods(t))
                .ToList();

            foreach (var testClass in testClasses)
            {
                var classMethods = GetTestMethods(testClass);
                foreach (var method in classMethods)
                {
                    var testCase = CreateTestCaseFromMethod(testClass, method, assemblyPath);
                    testCases.Add(testCase);
                }
            }

            _logger?.LogInformation("Discovered {Count} test cases from {Assembly}", testCases.Count, assemblyPath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error discovering test cases from {Assembly}", assemblyPath);
        }

        return testCases;
    }

    public async Task<TestCase?> GetTestCaseDetailsAsync(string assemblyPath, string className, string methodName)
    {
        try
        {
            var assembly = await LoadAssemblyAsync(assemblyPath);
            if (assembly == null) return null;

            var testClass = assembly.GetType(className);
            if (testClass == null) return null;

            var method = testClass.GetMethod(methodName);
            if (method == null) return null;

            return CreateTestCaseFromMethod(testClass, method, assemblyPath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting test case details for {Class}.{Method}", className, methodName);
            return null;
        }
    }

    public async Task<IEnumerable<string>> GetAvailableTagsAsync(string testDirectory)
    {
        var tags = new HashSet<string>();
        
        try
        {
            var testSuites = await DiscoverTestSuitesAsync(testDirectory);
            foreach (var suite in testSuites)
            {
                foreach (var testCase in suite.TestCases)
                {
                    foreach (var tag in testCase.Tags)
                    {
                        tags.Add(tag);
                    }
                }
                foreach (var tag in suite.Tags)
                {
                    tags.Add(tag);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting available tags from {Directory}", testDirectory);
        }

        return tags.OrderBy(t => t);
    }

    private async Task<TestSuite?> CreateTestSuiteFromAssemblyAsync(string assemblyPath)
    {
        try
        {
            var testCases = await DiscoverTestCasesAsync(assemblyPath);
            if (!testCases.Any()) return null;

            var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            var testSuite = new TestSuite
            {
                Name = assemblyName,
                Description = $"Test suite for {assemblyName}",
                Category = DetermineCategory(assemblyName),
                Status = TestSuiteStatus.Active
            };

            testSuite.TestCases.AddRange(testCases);
            
            // Extract tags from test cases
            var allTags = testCases.SelectMany(tc => tc.Tags).Distinct().ToList();
            testSuite.Tags.AddRange(allTags);

            return testSuite;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating test suite from {Assembly}", assemblyPath);
            return null;
        }
    }

    private async Task<Assembly?> LoadAssemblyAsync(string assemblyPath)
    {
        try
        {
            return await Task.Run(() =>
            {
                var context = new AssemblyLoadContext(null, isCollectible: true);
                return context.LoadFromAssemblyPath(assemblyPath);
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading assembly {Assembly}", assemblyPath);
            return null;
        }
    }

    private bool HasTestMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(m => IsTestMethod(m));
    }

    private IEnumerable<MethodInfo> GetTestMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(IsTestMethod);
    }

    private bool IsTestMethod(MethodInfo method)
    {
        // Check for common test attributes
        var attributes = method.GetCustomAttributes(true);
        var attributeNames = attributes.Select(a => a.GetType().Name).ToList();

        return attributeNames.Any(name => 
            name.Contains("Test") || 
            name.Contains("Fact") || 
            name.Contains("Theory") ||
            name.Equals("TestMethod", StringComparison.OrdinalIgnoreCase));
    }

    private TestCase CreateTestCaseFromMethod(Type testClass, MethodInfo method, string assemblyPath)
    {
        var testCase = new TestCase
        {
            Name = method.Name,
            Description = ExtractDescription(method),
            FilePath = assemblyPath,
            ClassName = testClass.FullName ?? testClass.Name,
            MethodName = method.Name,
            Priority = ExtractPriority(method),
            EstimatedDuration = ExtractEstimatedDuration(method)
        };

        // Extract tags from attributes
        testCase.Tags.AddRange(ExtractTags(method));
        testCase.Tags.AddRange(ExtractTags(testClass));

        // Extract parameters
        var parameters = method.GetParameters();
        foreach (var param in parameters)
        {
            testCase.Parameters[param.Name ?? "unknown"] = param.DefaultValue ?? DBNull.Value;
        }

        return testCase;
    }

    private string ExtractDescription(MethodInfo method)
    {
        // Try to get description from attributes
        var attributes = method.GetCustomAttributes(true);
        
        // Look for Description, DisplayName, or similar attributes
        foreach (var attr in attributes)
        {
            var attrType = attr.GetType();
            var descriptionProp = attrType.GetProperty("Description") ?? attrType.GetProperty("DisplayName");
            if (descriptionProp != null)
            {
                var value = descriptionProp.GetValue(attr)?.ToString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }

        // Fallback to method name with formatting
        return method.Name.Replace("_", " ").Replace("Test", "").Trim();
    }

    private TestCasePriority ExtractPriority(MethodInfo method)
    {
        var attributes = method.GetCustomAttributes(true);
        
        foreach (var attr in attributes)
        {
            var attrType = attr.GetType();
            if (attrType.Name.Contains("Priority"))
            {
                var priorityProp = attrType.GetProperty("Priority") ?? attrType.GetProperty("Value");
                if (priorityProp != null)
                {
                    var value = priorityProp.GetValue(attr);
                    if (value is int intValue)
                    {
                        return intValue switch
                        {
                            0 => TestCasePriority.Critical,
                            1 => TestCasePriority.High,
                            2 => TestCasePriority.Medium,
                            _ => TestCasePriority.Low
                        };
                    }
                }
            }
        }

        return TestCasePriority.Medium;
    }

    private TimeSpan ExtractEstimatedDuration(MethodInfo method)
    {
        var attributes = method.GetCustomAttributes(true);
        
        foreach (var attr in attributes)
        {
            var attrType = attr.GetType();
            if (attrType.Name.Contains("Timeout"))
            {
                var timeoutProp = attrType.GetProperty("Timeout") ?? attrType.GetProperty("Milliseconds");
                if (timeoutProp != null)
                {
                    var value = timeoutProp.GetValue(attr);
                    if (value is int milliseconds)
                    {
                        return TimeSpan.FromMilliseconds(milliseconds);
                    }
                }
            }
        }

        return TimeSpan.FromMinutes(5); // Default estimation
    }

    private List<string> ExtractTags(MemberInfo member)
    {
        var tags = new List<string>();
        var attributes = member.GetCustomAttributes(true);

        foreach (var attr in attributes)
        {
            var attrType = attr.GetType();
            
            // Look for Category, Tag, or TestCategory attributes
            if (attrType.Name.Contains("Category") || attrType.Name.Contains("Tag"))
            {
                var valueProp = attrType.GetProperty("Category") ?? 
                               attrType.GetProperty("Tag") ?? 
                               attrType.GetProperty("Value") ??
                               attrType.GetProperty("Name");
                
                if (valueProp != null)
                {
                    var value = valueProp.GetValue(attr)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                        tags.Add(value);
                }
            }
        }

        return tags;
    }

    private string DetermineCategory(string assemblyName)
    {
        if (assemblyName.Contains("Unit", StringComparison.OrdinalIgnoreCase))
            return "Unit Tests";
        if (assemblyName.Contains("Integration", StringComparison.OrdinalIgnoreCase))
            return "Integration Tests";
        if (assemblyName.Contains("E2E", StringComparison.OrdinalIgnoreCase) || 
            assemblyName.Contains("EndToEnd", StringComparison.OrdinalIgnoreCase))
            return "End-to-End Tests";
        if (assemblyName.Contains("Performance", StringComparison.OrdinalIgnoreCase))
            return "Performance Tests";
        if (assemblyName.Contains("Forex", StringComparison.OrdinalIgnoreCase))
            return "Forex Tests";

        return "General Tests";
    }
}


