using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TestAutomation.TestFramework.Core.Parameters;

/// <summary>
/// Dynamic Parameter Generator - Generates test data and parameters
/// </summary>
public interface IParameterGenerator
{
    T Generate<T>(string parameterName, Dictionary<string, object>? constraints = null);
    Dictionary<string, object> GenerateParameters(Dictionary<string, Type> parameterTypes, Dictionary<string, object>? constraints = null);
    IEnumerable<Dictionary<string, object>> GenerateParameterSets(Dictionary<string, Type> parameterTypes, int count, Dictionary<string, object>? constraints = null);
}

/// <summary>
/// Implementation of Parameter Generator
/// </summary>
public class ParameterGenerator : IParameterGenerator
{
    private readonly Random _random = new();
    private readonly ILogger<ParameterGenerator>? _logger;

    public ParameterGenerator(ILogger<ParameterGenerator>? logger = null)
    {
        _logger = logger;
    }

    public T Generate<T>(string parameterName, Dictionary<string, object>? constraints = null)
    {
        constraints ??= new Dictionary<string, object>();
        
        try
        {
            var type = typeof(T);
            var value = GenerateValueForType(type, parameterName, constraints);
            
            if (value is T typedValue)
            {
                return typedValue;
            }
            
            // Try to convert
            return (T)Convert.ChangeType(value, type);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating parameter {ParameterName} of type {Type}", parameterName, typeof(T).Name);
            return default(T)!;
        }
    }

    public Dictionary<string, object> GenerateParameters(Dictionary<string, Type> parameterTypes, Dictionary<string, object>? constraints = null)
    {
        var parameters = new Dictionary<string, object>();
        constraints ??= new Dictionary<string, object>();

        foreach (var (paramName, paramType) in parameterTypes)
        {
            try
            {
                var paramConstraints = ExtractParameterConstraints(paramName, constraints);
                var value = GenerateValueForType(paramType, paramName, paramConstraints);
                parameters[paramName] = value;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating parameter {ParameterName}", paramName);
                parameters[paramName] = GetDefaultValue(paramType);
            }
        }

        return parameters;
    }

    public IEnumerable<Dictionary<string, object>> GenerateParameterSets(Dictionary<string, Type> parameterTypes, int count, Dictionary<string, object>? constraints = null)
    {
        var parameterSets = new List<Dictionary<string, object>>();

        for (int i = 0; i < count; i++)
        {
            var parameters = GenerateParameters(parameterTypes, constraints);
            parameterSets.Add(parameters);
        }

        return parameterSets;
    }

    private object GenerateValueForType(Type type, string parameterName, Dictionary<string, object> constraints)
    {
        // Handle nullable types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = Nullable.GetUnderlyingType(type)!;
        }

        return type.Name switch
        {
            nameof(String) => GenerateString(parameterName, constraints),
            nameof(Int32) => GenerateInt(constraints),
            nameof(Int64) => GenerateLong(constraints),
            nameof(Double) => GenerateDouble(constraints),
            nameof(Decimal) => GenerateDecimal(constraints),
            nameof(Boolean) => GenerateBoolean(constraints),
            nameof(DateTime) => GenerateDateTime(constraints),
            nameof(Guid) => Guid.NewGuid(),
            _ when type.IsEnum => GenerateEnum(type, constraints),
            _ when type == typeof(byte[]) => GenerateByteArray(constraints),
            _ => GenerateComplexObject(type, parameterName, constraints)
        };
    }

    private string GenerateString(string parameterName, Dictionary<string, object> constraints)
    {
        var length = GetConstraintValue<int>(constraints, "length", 10);
        var minLength = GetConstraintValue<int>(constraints, "minLength", 1);
        var maxLength = GetConstraintValue<int>(constraints, "maxLength", 50);
        var pattern = GetConstraintValue<string>(constraints, "pattern", "");

        // Handle specific parameter patterns
        if (parameterName.Contains("email", StringComparison.OrdinalIgnoreCase))
        {
            return GenerateEmail();
        }
        
        if (parameterName.Contains("phone", StringComparison.OrdinalIgnoreCase))
        {
            return GeneratePhoneNumber();
        }
        
        if (parameterName.Contains("url", StringComparison.OrdinalIgnoreCase))
        {
            return GenerateUrl();
        }

        if (parameterName.Contains("name", StringComparison.OrdinalIgnoreCase))
        {
            return GenerateName();
        }

        if (!string.IsNullOrEmpty(pattern))
        {
            return GenerateStringByPattern(pattern);
        }

        // Generate random string
        var actualLength = length > 0 ? length : _random.Next(minLength, maxLength + 1);
        return GenerateRandomString(actualLength);
    }

    private int GenerateInt(Dictionary<string, object> constraints)
    {
        var min = GetConstraintValue<int>(constraints, "min", 1);
        var max = GetConstraintValue<int>(constraints, "max", 1000);
        return _random.Next(min, max + 1);
    }

    private long GenerateLong(Dictionary<string, object> constraints)
    {
        var min = GetConstraintValue<long>(constraints, "min", 1L);
        var max = GetConstraintValue<long>(constraints, "max", 1000000L);
        return _random.NextInt64(min, max + 1);
    }

    private double GenerateDouble(Dictionary<string, object> constraints)
    {
        var min = GetConstraintValue<double>(constraints, "min", 0.0);
        var max = GetConstraintValue<double>(constraints, "max", 1000.0);
        return _random.NextDouble() * (max - min) + min;
    }

    private decimal GenerateDecimal(Dictionary<string, object> constraints)
    {
        var min = GetConstraintValue<decimal>(constraints, "min", 0m);
        var max = GetConstraintValue<decimal>(constraints, "max", 1000m);
        var range = max - min;
        return min + (decimal)_random.NextDouble() * range;
    }

    private bool GenerateBoolean(Dictionary<string, object> constraints)
    {
        var probability = GetConstraintValue<double>(constraints, "trueProbability", 0.5);
        return _random.NextDouble() < probability;
    }

    private DateTime GenerateDateTime(Dictionary<string, object> constraints)
    {
        var minDate = GetConstraintValue<DateTime>(constraints, "minDate", DateTime.Today.AddYears(-1));
        var maxDate = GetConstraintValue<DateTime>(constraints, "maxDate", DateTime.Today.AddYears(1));
        
        var range = maxDate - minDate;
        var randomDays = _random.Next(0, range.Days + 1);
        return minDate.AddDays(randomDays);
    }

    private object GenerateEnum(Type enumType, Dictionary<string, object> constraints)
    {
        var values = Enum.GetValues(enumType);
        var index = _random.Next(values.Length);
        return values.GetValue(index)!;
    }

    private byte[] GenerateByteArray(Dictionary<string, object> constraints)
    {
        var length = GetConstraintValue<int>(constraints, "length", 10);
        var bytes = new byte[length];
        _random.NextBytes(bytes);
        return bytes;
    }

    private object GenerateComplexObject(Type type, string parameterName, Dictionary<string, object> constraints)
    {
        // For complex objects, try to create a default instance
        try
        {
            var instance = Activator.CreateInstance(type);
            return instance ?? GetDefaultValue(type);
        }
        catch
        {
            return GetDefaultValue(type);
        }
    }

    private string GenerateEmail()
    {
        var names = new[] { "john", "jane", "bob", "alice", "charlie", "diana" };
        var domains = new[] { "example.com", "test.org", "sample.net", "demo.io" };
        
        var name = names[_random.Next(names.Length)];
        var domain = domains[_random.Next(domains.Length)];
        var number = _random.Next(1, 999);
        
        return $"{name}{number}@{domain}";
    }

    private string GeneratePhoneNumber()
    {
        return $"+1{_random.Next(200, 999)}{_random.Next(200, 999)}{_random.Next(1000, 9999)}";
    }

    private string GenerateUrl()
    {
        var protocols = new[] { "http", "https" };
        var domains = new[] { "example.com", "test.org", "sample.net" };
        var paths = new[] { "/", "/home", "/about", "/products", "/contact" };
        
        var protocol = protocols[_random.Next(protocols.Length)];
        var domain = domains[_random.Next(domains.Length)];
        var path = paths[_random.Next(paths.Length)];
        
        return $"{protocol}://{domain}{path}";
    }

    private string GenerateName()
    {
        var firstNames = new[] { "John", "Jane", "Bob", "Alice", "Charlie", "Diana", "Mike", "Sarah" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
        
        var firstName = firstNames[_random.Next(firstNames.Length)];
        var lastName = lastNames[_random.Next(lastNames.Length)];
        
        return $"{firstName} {lastName}";
    }

    private string GenerateStringByPattern(string pattern)
    {
        // Simple pattern implementation
        var result = new StringBuilder();
        
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            result.Append(c switch
            {
                '#' => _random.Next(0, 10).ToString()[0],
                '@' => (char)_random.Next('A', 'Z' + 1),
                '?' => (char)_random.Next('a', 'z' + 1),
                _ => c
            });
        }
        
        return result.ToString();
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var result = new StringBuilder(length);
        
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[_random.Next(chars.Length)]);
        }
        
        return result.ToString();
    }

    private Dictionary<string, object> ExtractParameterConstraints(string parameterName, Dictionary<string, object> allConstraints)
    {
        var paramConstraints = new Dictionary<string, object>();
        
        foreach (var (key, value) in allConstraints)
        {
            if (key.StartsWith($"{parameterName}.", StringComparison.OrdinalIgnoreCase))
            {
                var constraintName = key.Substring(parameterName.Length + 1);
                paramConstraints[constraintName] = value;
            }
        }
        
        return paramConstraints;
    }

    private T GetConstraintValue<T>(Dictionary<string, object> constraints, string key, T defaultValue)
    {
        if (constraints.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T typedValue)
                    return typedValue;
                
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        return defaultValue;
    }

    private object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type)!;
        }
        
        return null!;
    }
}

/// <summary>
/// Forex-specific parameter generator
/// </summary>
public class ForexParameterGenerator : ParameterGenerator
{
    private readonly string[] _majorCurrencies = { "USD", "EUR", "GBP", "JPY", "CHF", "CAD", "AUD", "NZD" };
    private readonly string[] _currencyPairs = { "EURUSD", "GBPUSD", "USDJPY", "USDCHF", "AUDUSD", "USDCAD", "NZDUSD" };
    private readonly Random _forexRandom = new();

    public ForexParameterGenerator(ILogger<ParameterGenerator>? logger = null) : base(logger)
    {
    }

    public string GenerateCurrencyPair()
    {
        return _currencyPairs[_forexRandom.Next(_currencyPairs.Length)];
    }

    public string GenerateCurrency()
    {
        return _majorCurrencies[_forexRandom.Next(_majorCurrencies.Length)];
    }

    public decimal GeneratePrice(string currencyPair, Dictionary<string, object>? constraints = null)
    {
        constraints ??= new Dictionary<string, object>();

        var basePrice = currencyPair.ToUpper() switch
        {
            "EURUSD" => 1.0500m,
            "GBPUSD" => 1.2500m,
            "USDJPY" => 150.00m,
            "USDCHF" => 0.9200m,
            "AUDUSD" => 0.6500m,
            "USDCAD" => 1.3500m,
            "NZDUSD" => 0.6000m,
            _ => 1.0000m
        };

        var volatility = GetForexConstraintValue<decimal>(constraints, "volatility", 0.01m);
        var variation = (decimal)_forexRandom.NextDouble() * volatility * 2 - volatility;
        
        return Math.Max(0, basePrice * (1 + variation));
    }

    public decimal GenerateLotSize(Dictionary<string, object>? constraints = null)
    {
        constraints ??= new Dictionary<string, object>();
        
        var min = GetForexConstraintValue<decimal>(constraints, "minLot", 0.01m);
        var max = GetForexConstraintValue<decimal>(constraints, "maxLot", 10.0m);
        
        var lots = new[] { 0.01m, 0.05m, 0.1m, 0.5m, 1.0m, 2.0m, 5.0m, 10.0m };
        var validLots = lots.Where(l => l >= min && l <= max).ToArray();
        
        return validLots.Length > 0 ? validLots[_forexRandom.Next(validLots.Length)] : min;
    }

    public int GenerateLeverage()
    {
        var leverages = new[] { 1, 10, 50, 100, 200, 400, 500 };
        return leverages[_forexRandom.Next(leverages.Length)];
    }

    public decimal GenerateBalance(Dictionary<string, object>? constraints = null)
    {
        constraints ??= new Dictionary<string, object>();
        
        var min = GetForexConstraintValue<decimal>(constraints, "minBalance", 1000m);
        var max = GetForexConstraintValue<decimal>(constraints, "maxBalance", 100000m);
        
        return min + (decimal)_forexRandom.NextDouble() * (max - min);
    }

    private T GetForexConstraintValue<T>(Dictionary<string, object> constraints, string key, T defaultValue)
    {
        if (constraints.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T typedValue)
                    return typedValue;
                
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        return defaultValue;
    }
}