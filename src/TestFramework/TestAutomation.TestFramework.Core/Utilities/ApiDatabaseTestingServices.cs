using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RestSharp;
using TestAutomation.Domain.Models.Configuration;

namespace TestAutomation.TestFramework.Core.Utilities;

/// <summary>
/// Advanced API Testing Utilities with comprehensive features
/// </summary>
public interface IApiTestingService
{
    Task<ApiResponse<T>> GetAsync<T>(string endpoint, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? body = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? body = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> DeleteAsync<T>(string endpoint, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(string endpoint, TimeSpan? timeout = null);
    Task<ApiPerformanceMetrics> MeasurePerformanceAsync<T>(Func<Task<ApiResponse<T>>> apiCall, int iterations = 1);
    void SetBaseUrl(string baseUrl);
    void SetAuthentication(AuthenticationConfiguration authConfig);
    void SetDefaultHeaders(Dictionary<string, string> headers);
}

/// <summary>
/// Implementation of API Testing Service
/// </summary>
public class ApiTestingService : IApiTestingService, IDisposable
{
    private readonly RestClient _client;
    private readonly ILogger<ApiTestingService> _logger;
    private readonly ApiConfiguration _configuration;
    private bool _disposed = false;

    public ApiTestingService(ApiConfiguration configuration, ILogger<ApiTestingService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var options = new RestClientOptions(configuration.BaseUrl)
        {
            Timeout = configuration.DefaultTimeout,
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => 
                !configuration.ValidateSslCertificate || sslPolicyErrors == System.Net.Security.SslPolicyErrors.None
        };

        if (!string.IsNullOrEmpty(configuration.ProxyUrl))
        {
            options.Proxy = new System.Net.WebProxy(configuration.ProxyUrl);
        }

        _client = new RestClient(options);

        // Set default headers
        foreach (var header in configuration.DefaultHeaders)
        {
            _client.AddDefaultHeader(header.Key, header.Value);
        }

        // Configure authentication
        if (configuration.Authentication != null)
        {
            SetAuthentication(configuration.Authentication);
        }
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest(endpoint, Method.Get);
        AddHeaders(request, headers);
        return await ExecuteRequestAsync<T>(request, cancellationToken);
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? body = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest(endpoint, Method.Post);
        AddHeaders(request, headers);
        
        if (body != null)
        {
            request.AddJsonBody(body);
        }
        
        return await ExecuteRequestAsync<T>(request, cancellationToken);
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? body = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest(endpoint, Method.Put);
        AddHeaders(request, headers);
        
        if (body != null)
        {
            request.AddJsonBody(body);
        }
        
        return await ExecuteRequestAsync<T>(request, cancellationToken);
    }

    public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest(endpoint, Method.Delete);
        AddHeaders(request, headers);
        return await ExecuteRequestAsync<T>(request, cancellationToken);
    }

    public async Task<bool> HealthCheckAsync(string endpoint, TimeSpan? timeout = null)
    {
        try
        {
            var request = new RestRequest(endpoint, Method.Get);
            if (timeout.HasValue)
            {
                request.Timeout = timeout.Value;
            }

            var response = await _client.ExecuteAsync(request);
            return response.IsSuccessful;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for endpoint: {Endpoint}", endpoint);
            return false;
        }
    }

    public async Task<ApiPerformanceMetrics> MeasurePerformanceAsync<T>(Func<Task<ApiResponse<T>>> apiCall, int iterations = 1)
    {
        var metrics = new ApiPerformanceMetrics
        {
            TotalIterations = iterations,
            StartTime = DateTime.UtcNow
        };

        var responseTimes = new List<TimeSpan>();
        var successCount = 0;

        for (int i = 0; i < iterations; i++)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                var response = await apiCall();
                var responseTime = DateTime.UtcNow - startTime;
                responseTimes.Add(responseTime);
                
                if (response.IsSuccessful)
                {
                    successCount++;
                }
                
                metrics.StatusCodes.TryGetValue(response.StatusCode, out var count);
                metrics.StatusCodes[response.StatusCode] = count + 1;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API call failed during performance measurement, iteration {Iteration}", i + 1);
                metrics.Errors.Add($"Iteration {i + 1}: {ex.Message}");
            }
        }

        metrics.EndTime = DateTime.UtcNow;
        metrics.TotalDuration = metrics.EndTime - metrics.StartTime;
        metrics.SuccessfulRequests = successCount;
        metrics.FailedRequests = iterations - successCount;
        metrics.SuccessRate = iterations > 0 ? (double)successCount / iterations * 100 : 0;

        if (responseTimes.Any())
        {
            metrics.AverageResponseTime = TimeSpan.FromTicks((long)responseTimes.Average(rt => rt.Ticks));
            metrics.MinResponseTime = responseTimes.Min();
            metrics.MaxResponseTime = responseTimes.Max();
            
            // Calculate percentiles
            var sortedTimes = responseTimes.OrderBy(rt => rt).ToList();
            metrics.P50ResponseTime = sortedTimes[(int)(sortedTimes.Count * 0.5)];
            metrics.P90ResponseTime = sortedTimes[(int)(sortedTimes.Count * 0.9)];
            metrics.P95ResponseTime = sortedTimes[(int)(sortedTimes.Count * 0.95)];
            metrics.P99ResponseTime = sortedTimes[(int)(sortedTimes.Count * 0.99)];
        }

        if (iterations > 0 && metrics.TotalDuration.TotalSeconds > 0)
        {
            metrics.RequestsPerSecond = iterations / metrics.TotalDuration.TotalSeconds;
        }

        return metrics;
    }

    public void SetBaseUrl(string baseUrl)
    {
        _client.Options.BaseUrl = new Uri(baseUrl);
    }

    public void SetAuthentication(AuthenticationConfiguration authConfig)
    {
        switch (authConfig.Type)
        {
            case AuthenticationType.Basic:
                if (!string.IsNullOrEmpty(authConfig.Username) && !string.IsNullOrEmpty(authConfig.Password))
                {
                    var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{authConfig.Username}:{authConfig.Password}"));
                    _client.AddDefaultHeader("Authorization", $"Basic {credentials}");
                }
                break;

            case AuthenticationType.Bearer:
                if (!string.IsNullOrEmpty(authConfig.Token))
                {
                    _client.AddDefaultHeader("Authorization", $"Bearer {authConfig.Token}");
                }
                break;

            case AuthenticationType.ApiKey:
                if (!string.IsNullOrEmpty(authConfig.ApiKey))
                {
                    _client.AddDefaultHeader("X-API-Key", authConfig.ApiKey);
                }
                break;

            case AuthenticationType.OAuth2:
                // OAuth2 would require a token refresh mechanism
                if (!string.IsNullOrEmpty(authConfig.Token))
                {
                    _client.AddDefaultHeader("Authorization", $"Bearer {authConfig.Token}");
                }
                break;
        }
    }

    public void SetDefaultHeaders(Dictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            _client.AddDefaultHeader(header.Key, header.Value);
        }
    }

    private async Task<ApiResponse<T>> ExecuteRequestAsync<T>(RestRequest request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            if (_configuration.EnableRequestLogging)
            {
                _logger.LogDebug("API Request: {Method} {Url}", request.Method, request.Resource);
            }

            var response = await _client.ExecuteAsync<T>(request, cancellationToken);
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            if (_configuration.EnableResponseLogging)
            {
                _logger.LogDebug("API Response: {StatusCode} in {Duration}ms", 
                    response.StatusCode, duration.TotalMilliseconds);
            }

            var apiResponse = new ApiResponse<T>
            {
                Data = response.Data,
                StatusCode = (int)response.StatusCode,
                IsSuccessful = response.IsSuccessful,
                Content = response.Content,
                Headers = response.Headers?.ToDictionary(h => h.Name, h => h.Value?.ToString() ?? "") ?? new Dictionary<string, string>(),
                ResponseTime = duration,
                ErrorMessage = response.ErrorMessage
            };

            if (response.ErrorException != null)
            {
                apiResponse.Exception = response.ErrorException;
            }

            return apiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API request failed: {Method} {Url}", request.Method, request.Resource);
            
            return new ApiResponse<T>
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Exception = ex,
                ResponseTime = DateTime.UtcNow - startTime
            };
        }
    }

    private static void AddHeaders(RestRequest request, Dictionary<string, string>? headers)
    {
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.AddHeader(header.Key, header.Value);
            }
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
            _client?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Database Testing Service with comprehensive SQL testing capabilities
/// </summary>
public interface IDatabaseTestingService
{
    Task<DbConnection> GetConnectionAsync();
    Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null);
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null) where T : class, new();
    Task<int> ExecuteAsync(string sql, object? parameters = null);
    Task<bool> TableExistsAsync(string tableName);
    Task<bool> ColumnExistsAsync(string tableName, string columnName);
    Task<long> GetRowCountAsync(string tableName, string? whereClause = null);
    Task<Dictionary<string, object?>> GetRowAsync(string tableName, string whereClause, object? parameters = null);
    Task<bool> ExecuteScriptAsync(string scriptPath);
    Task<DatabaseHealthCheck> PerformHealthCheckAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

/// <summary>
/// Implementation of Database Testing Service
/// </summary>
public class DatabaseTestingService : IDatabaseTestingService, IDisposable
{
    private readonly DatabaseConfiguration _configuration;
    private readonly ILogger<DatabaseTestingService> _logger;
    private DbConnection? _connection;
    private DbTransaction? _currentTransaction;
    private bool _disposed = false;

    public DatabaseTestingService(DatabaseConfiguration configuration, ILogger<DatabaseTestingService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DbConnection> GetConnectionAsync()
    {
        if (_connection == null || _connection.State != ConnectionState.Open)
        {
            _connection?.Dispose();
            _connection = CreateConnection();
            await _connection.OpenAsync();
        }
        return _connection;
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        try
        {
            var connection = await GetConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = (int)_configuration.CommandTimeout.TotalSeconds;
            command.Transaction = _currentTransaction;

            AddParameters(command, parameters);

            var result = await command.ExecuteScalarAsync();
            
            if (result == null || result == DBNull.Value)
                return default(T);

            return (T)Convert.ChangeType(result, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scalar query: {Sql}", sql);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null) where T : class, new()
    {
        try
        {
            var connection = await GetConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = (int)_configuration.CommandTimeout.TotalSeconds;
            command.Transaction = _currentTransaction;

            AddParameters(command, parameters);

            using var reader = await command.ExecuteReaderAsync();
            var results = new List<T>();
            var properties = typeof(T).GetProperties();

            while (await reader.ReadAsync())
            {
                var item = new T();
                
                foreach (var property in properties)
                {
                    try
                    {
                        var columnName = property.Name;
                        
                        // Check if column exists
                        var columnExists = false;
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                            {
                                columnExists = true;
                                break;
                            }
                        }

                        if (!columnExists) continue;

                        var value = reader[columnName];
                        if (value != null && value != DBNull.Value)
                        {
                            if (property.PropertyType.IsGenericType && 
                                property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
                                property.SetValue(item, Convert.ChangeType(value, underlyingType!));
                            }
                            else
                            {
                                property.SetValue(item, Convert.ChangeType(value, property.PropertyType));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error setting property {PropertyName} for type {TypeName}", 
                            property.Name, typeof(T).Name);
                    }
                }

                results.Add(item);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Sql}", sql);
            throw;
        }
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        try
        {
            var connection = await GetConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = (int)_configuration.CommandTimeout.TotalSeconds;
            command.Transaction = _currentTransaction;

            AddParameters(command, parameters);

            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command: {Sql}", sql);
            throw;
        }
    }

    public async Task<bool> TableExistsAsync(string tableName)
    {
        var sql = _configuration.Provider switch
        {
            DatabaseProvider.SqlServer => $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'",
            DatabaseProvider.MySQL => $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'",
            DatabaseProvider.PostgreSQL => $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}'",
            DatabaseProvider.SQLite => $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'",
            _ => throw new NotSupportedException($"Database provider {_configuration.Provider} is not supported")
        };

        var count = await ExecuteScalarAsync<int>(sql);
        return count > 0;
    }

    public async Task<bool> ColumnExistsAsync(string tableName, string columnName)
    {
        var sql = _configuration.Provider switch
        {
            DatabaseProvider.SqlServer => $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'",
            DatabaseProvider.MySQL => $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'",
            DatabaseProvider.PostgreSQL => $"SELECT COUNT(*) FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}'",
            DatabaseProvider.SQLite => $"PRAGMA table_info({tableName})",
            _ => throw new NotSupportedException($"Database provider {_configuration.Provider} is not supported")
        };

        if (_configuration.Provider == DatabaseProvider.SQLite)
        {
            var columns = await QueryAsync<SqliteColumnInfo>(sql);
            return columns.Any(c => c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            var count = await ExecuteScalarAsync<int>(sql);
            return count > 0;
        }
    }

    public async Task<long> GetRowCountAsync(string tableName, string? whereClause = null)
    {
        var sql = $"SELECT COUNT(*) FROM {tableName}";
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }

        return await ExecuteScalarAsync<long>(sql);
    }

    public async Task<Dictionary<string, object?>> GetRowAsync(string tableName, string whereClause, object? parameters = null)
    {
        var sql = $"SELECT * FROM {tableName} WHERE {whereClause}";
        
        var connection = await GetConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = (int)_configuration.CommandTimeout.TotalSeconds;
        command.Transaction = _currentTransaction;

        AddParameters(command, parameters);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader[i];
                row[columnName] = value == DBNull.Value ? null : value;
            }
            return row;
        }

        return new Dictionary<string, object?>();
    }

    public async Task<bool> ExecuteScriptAsync(string scriptPath)
    {
        try
        {
            if (!File.Exists(scriptPath))
            {
                _logger.LogError("Script file not found: {ScriptPath}", scriptPath);
                return false;
            }

            var script = await File.ReadAllTextAsync(scriptPath);
            var commands = script.Split(new[] { "GO", ";" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var commandText in commands)
            {
                if (string.IsNullOrWhiteSpace(commandText)) continue;

                await ExecuteAsync(commandText.Trim());
            }

            _logger.LogInformation("Successfully executed script: {ScriptPath}", scriptPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing script: {ScriptPath}", scriptPath);
            return false;
        }
    }

    public async Task<DatabaseHealthCheck> PerformHealthCheckAsync()
    {
        var healthCheck = new DatabaseHealthCheck
        {
            CheckTime = DateTime.UtcNow,
            Provider = _configuration.Provider.ToString()
        };

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Test connection
            var connection = await GetConnectionAsync();
            healthCheck.ConnectionSuccessful = true;
            healthCheck.ConnectionTime = DateTime.UtcNow - startTime;

            // Test simple query
            startTime = DateTime.UtcNow;
            await ExecuteScalarAsync<int>("SELECT 1");
            healthCheck.QuerySuccessful = true;
            healthCheck.QueryTime = DateTime.UtcNow - startTime;

            // Get database info
            healthCheck.DatabaseName = connection.Database;
            healthCheck.ServerVersion = connection.ServerVersion;

            healthCheck.IsHealthy = true;
        }
        catch (Exception ex)
        {
            healthCheck.IsHealthy = false;
            healthCheck.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Database health check failed");
        }

        return healthCheck;
    }

    public async Task BeginTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("Transaction is already active");
        }

        var connection = await GetConnectionAsync();
        _currentTransaction = await connection.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit");
        }

        await _currentTransaction.CommitAsync();
        _currentTransaction.Dispose();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to rollback");
        }

        await _currentTransaction.RollbackAsync();
        _currentTransaction.Dispose();
        _currentTransaction = null;
    }

    private DbConnection CreateConnection()
    {
        return _configuration.Provider switch
        {
            DatabaseProvider.SqlServer => new Microsoft.Data.SqlClient.SqlConnection(_configuration.ConnectionString),
            DatabaseProvider.SQLite => new Microsoft.Data.Sqlite.SqliteConnection(_configuration.ConnectionString),
            // Add other providers as needed
            _ => throw new NotSupportedException($"Database provider {_configuration.Provider} is not supported")
        };
    }

    private static void AddParameters(DbCommand command, object? parameters)
    {
        if (parameters == null) return;

        if (parameters is Dictionary<string, object?> dict)
        {
            foreach (var kvp in dict)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = kvp.Key.StartsWith('@') ? kvp.Key : $"@{kvp.Key}";
                parameter.Value = kvp.Value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }
        else
        {
            // Use reflection for anonymous objects
            var properties = parameters.GetType().GetProperties();
            foreach (var property in properties)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{property.Name}";
                parameter.Value = property.GetValue(parameters) ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
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
            _currentTransaction?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}

// Supporting classes

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public int StatusCode { get; set; }
    public bool IsSuccessful { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
}

public class ApiPerformanceMetrics
{
    public int TotalIterations { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public TimeSpan P50ResponseTime { get; set; }
    public TimeSpan P90ResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public TimeSpan P99ResponseTime { get; set; }
    public double RequestsPerSecond { get; set; }
    public Dictionary<int, int> StatusCodes { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class DatabaseHealthCheck
{
    public DateTime CheckTime { get; set; }
    public bool IsHealthy { get; set; }
    public string Provider { get; set; } = string.Empty;
    public bool ConnectionSuccessful { get; set; }
    public TimeSpan ConnectionTime { get; set; }
    public bool QuerySuccessful { get; set; }
    public TimeSpan QueryTime { get; set; }
    public string? DatabaseName { get; set; }
    public string? ServerVersion { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SqliteColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}