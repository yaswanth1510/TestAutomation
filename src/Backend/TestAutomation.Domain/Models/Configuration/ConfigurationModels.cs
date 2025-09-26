namespace TestAutomation.Domain.Models.Configuration;

/// <summary>
/// Represents comprehensive test execution configuration
/// </summary>
public class TestExecutionConfiguration : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public ExecutionMode Mode { get; set; } = ExecutionMode.Sequential;
    public int MaxParallelTests { get; set; } = 4;
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public bool ContinueOnFailure { get; set; } = true;
    public bool CaptureScreenshots { get; set; } = true;
    public bool RecordVideos { get; set; } = false;
    public bool EnableDetailedLogging { get; set; } = true;
    
    // Browser Configuration
    public BrowserConfiguration BrowserConfig { get; set; } = new();
    
    // Database Configuration
    public DatabaseConfiguration DatabaseConfig { get; set; } = new();
    
    // API Configuration
    public ApiConfiguration ApiConfig { get; set; } = new();
    
    // Forex-specific Configuration
    public ForexConfiguration ForexConfig { get; set; } = new();
    
    // Notification Configuration
    public NotificationConfiguration NotificationConfig { get; set; } = new();
    
    // Reporting Configuration
    public ReportingConfiguration ReportingConfig { get; set; } = new();
    
    // Custom Parameters
    public Dictionary<string, object> CustomParameters { get; set; } = new();
    
    // Environment Variables
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}

/// <summary>
/// Represents browser testing configuration
/// </summary>
public class BrowserConfiguration : BaseEntity
{
    public BrowserType DefaultBrowser { get; set; } = BrowserType.Chrome;
    public List<BrowserType> SupportedBrowsers { get; set; } = new() { BrowserType.Chrome, BrowserType.Firefox, BrowserType.Edge };
    public bool Headless { get; set; } = false;
    public WindowSize WindowSize { get; set; } = WindowSize.Desktop;
    public string? CustomWindowSize { get; set; } // "1920x1080"
    public TimeSpan ImplicitWait { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan PageLoadTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableLogging { get; set; } = true;
    public bool EnablePerformanceLogs { get; set; } = false;
    public bool EnableNetworkLogs { get; set; } = false;
    public string? DownloadDirectory { get; set; }
    public Dictionary<string, object> BrowserOptions { get; set; } = new();
    public List<BrowserExtension> Extensions { get; set; } = new();
}

/// <summary>
/// Represents browser extension configuration
/// </summary>
public class BrowserExtension : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Represents database testing configuration
/// </summary>
public class DatabaseConfiguration : BaseEntity
{
    public string ConnectionString { get; set; } = string.Empty;
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableConnectionPooling { get; set; } = true;
    public int MaxPoolSize { get; set; } = 100;
    public bool EnableQueryLogging { get; set; } = false;
    public bool UseTransactions { get; set; } = true;
    public string? TestDataDirectory { get; set; }
    public List<DatabaseScript> SetupScripts { get; set; } = new();
    public List<DatabaseScript> TeardownScripts { get; set; } = new();
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// Represents database scripts for setup and teardown
/// </summary>
public class DatabaseScript : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? SqlContent { get; set; }
    public int ExecutionOrder { get; set; } = 0;
    public bool IsRequired { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents API testing configuration
/// </summary>
public class ApiConfiguration : BaseEntity
{
    public string BaseUrl { get; set; } = string.Empty;
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public AuthenticationConfiguration? Authentication { get; set; }
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public bool EnableRetry { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool EnableRequestLogging { get; set; } = true;
    public bool EnableResponseLogging { get; set; } = true;
    public bool ValidateSslCertificate { get; set; } = true;
    public string? ProxyUrl { get; set; }
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Represents authentication configuration for API testing
/// </summary>
public class AuthenticationConfiguration : BaseEntity
{
    public AuthenticationType Type { get; set; } = AuthenticationType.None;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
    public string? ApiKey { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TokenEndpoint { get; set; }
    public List<string> Scopes { get; set; } = new();
    public Dictionary<string, string> AdditionalParameters { get; set; } = new();
}

/// <summary>
/// Represents Forex-specific testing configuration
/// </summary>
public class ForexConfiguration : BaseEntity
{
    public List<string> DefaultCurrencyPairs { get; set; } = new() { "EURUSD", "GBPUSD", "USDJPY" };
    public TradingSession DefaultTradingSession { get; set; } = TradingSession.London;
    public decimal DefaultLotSize { get; set; } = 0.1m;
    public int DefaultLeverage { get; set; } = 100;
    public decimal DefaultBalance { get; set; } = 10000m;
    public string BaseCurrency { get; set; } = "USD";
    public bool SimulateMarketConditions { get; set; } = true;
    public bool EnableSlippage { get; set; } = true;
    public decimal MaxSlippagePips { get; set; } = 2m;
    public bool EnableSwap { get; set; } = true;
    public bool EnableCommission { get; set; } = true;
    public decimal DefaultCommission { get; set; } = 0.0001m; // 0.01%
    public MarketDataConfiguration MarketData { get; set; } = new();
    public RiskManagementConfiguration RiskManagement { get; set; } = new();
}

/// <summary>
/// Represents market data configuration for Forex testing
/// </summary>
public class MarketDataConfiguration : BaseEntity
{
    public MarketDataProvider Provider { get; set; } = MarketDataProvider.Simulation;
    public string? DataFeedUrl { get; set; }
    public string? ApiKey { get; set; }
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(1);
    public int HistoryDepth { get; set; } = 1000; // Number of historical bars
    public bool EnableRealTimeData { get; set; } = false;
    public bool CacheHistoricalData { get; set; } = true;
    public string? CacheDirectory { get; set; }
    public Dictionary<string, decimal> DefaultSpreads { get; set; } = new();
    public Dictionary<string, object> ProviderSettings { get; set; } = new();
}

/// <summary>
/// Represents risk management configuration
/// </summary>
public class RiskManagementConfiguration : BaseEntity
{
    public decimal MaxRiskPerTrade { get; set; } = 0.02m; // 2%
    public decimal MaxDailyLoss { get; set; } = 0.05m; // 5%
    public decimal MaxDrawdown { get; set; } = 0.10m; // 10%
    public int MaxOpenPositions { get; set; } = 5;
    public decimal MaxLotSize { get; set; } = 5.0m;
    public bool RequireStopLoss { get; set; } = true;
    public bool RequireTakeProfit { get; set; } = false;
    public TimeSpan MaxPositionDuration { get; set; } = TimeSpan.FromHours(24);
    public List<TradingRestriction> Restrictions { get; set; } = new();
}

/// <summary>
/// Represents trading restrictions
/// </summary>
public class TradingRestriction : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public RestrictionType Type { get; set; } = RestrictionType.TimeWindow;
    public string Condition { get; set; } = string.Empty; // JSON condition
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents notification configuration
/// </summary>
public class NotificationConfiguration : BaseEntity
{
    public bool EnableNotifications { get; set; } = true;
    public List<NotificationChannel> Channels { get; set; } = new();
    public NotificationTriggers Triggers { get; set; } = new();
    public Dictionary<string, string> Templates { get; set; } = new();
}

/// <summary>
/// Represents notification channels
/// </summary>
public class NotificationChannel : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public NotificationChannelType Type { get; set; } = NotificationChannelType.Email;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, string> Settings { get; set; } = new();
    public List<string> Recipients { get; set; } = new();
    public NotificationLevel MinLevel { get; set; } = NotificationLevel.Warning;
}

/// <summary>
/// Represents notification triggers
/// </summary>
public class NotificationTriggers : BaseEntity
{
    public bool OnTestStart { get; set; } = false;
    public bool OnTestComplete { get; set; } = true;
    public bool OnTestFailure { get; set; } = true;
    public bool OnCriticalError { get; set; } = true;
    public bool OnPerformanceIssue { get; set; } = false;
    public decimal FailureRateThreshold { get; set; } = 0.1m; // 10%
    public TimeSpan PerformanceThreshold { get; set; } = TimeSpan.FromMinutes(30);
    public Dictionary<string, object> CustomTriggers { get; set; } = new();
}

/// <summary>
/// Represents reporting configuration
/// </summary>
public class ReportingConfiguration : BaseEntity
{
    public bool EnableReporting { get; set; } = true;
    public List<ReportFormat> OutputFormats { get; set; } = new() { ReportFormat.HTML, ReportFormat.JSON };
    public string OutputDirectory { get; set; } = "./TestReports";
    public bool IncludeScreenshots { get; set; } = true;
    public bool IncludeLogs { get; set; } = true;
    public bool IncludeStackTraces { get; set; } = true;
    public bool IncludeEnvironmentDetails { get; set; } = true;
    public bool GenerateTrendReports { get; set; } = true;
    public int HistoryRetentionDays { get; set; } = 30;
    public List<CustomReport> CustomReports { get; set; } = new();
    public Dictionary<string, object> ReportSettings { get; set; } = new();
}

/// <summary>
/// Represents custom report configuration
/// </summary>
public class CustomReport : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TemplatePath { get; set; } = string.Empty;
    public ReportFormat Format { get; set; } = ReportFormat.HTML;
    public bool IsEnabled { get; set; } = true;
    public List<string> IncludeFields { get; set; } = new();
    public List<string> ExcludeFields { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents environment-specific settings
/// </summary>
public class EnvironmentSettings : BaseEntity
{
    public string EnvironmentName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EnvironmentType Type { get; set; } = EnvironmentType.Development;
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> ConnectionStrings { get; set; } = new();
    public Dictionary<string, string> ApiEndpoints { get; set; } = new();
    public Dictionary<string, string> Credentials { get; set; } = new();
    public Dictionary<string, object> ApplicationSettings { get; set; } = new();
    public List<EnvironmentVariable> Variables { get; set; } = new();
    public List<EnvironmentDependency> Dependencies { get; set; } = new();
}

/// <summary>
/// Represents environment variables
/// </summary>
public class EnvironmentVariable : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsSecret { get; set; } = false;
    public string? Description { get; set; }
}

/// <summary>
/// Represents environment dependencies
/// </summary>
public class EnvironmentDependency : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Database, API, Service, etc.
    public string Endpoint { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public Dictionary<string, object> Configuration { get; set; } = new();
}

// Enums for Configuration Domain

public enum ExecutionMode
{
    Sequential,
    Parallel,
    Distributed
}

public enum BrowserType
{
    Chrome,
    Firefox,
    Edge,
    Safari,
    Opera,
    InternetExplorer
}

public enum WindowSize
{
    Mobile,
    Tablet,
    Desktop,
    FullHD,
    UltraHD,
    Custom
}

public enum DatabaseProvider
{
    SqlServer,
    MySQL,
    PostgreSQL,
    Oracle,
    SQLite,
    MongoDB,
    Redis
}

public enum AuthenticationType
{
    None,
    Basic,
    Bearer,
    OAuth2,
    ApiKey,
    Custom
}

public enum TradingSession
{
    Sydney,
    Tokyo,
    London,
    NewYork,
    All
}

public enum MarketDataProvider
{
    Simulation,
    Historical,
    Live,
    Custom
}

public enum RestrictionType
{
    TimeWindow,
    MarketCondition,
    AccountBalance,
    RiskLevel,
    Custom
}

public enum NotificationChannelType
{
    Email,
    Slack,
    Teams,
    Discord,
    Webhook,
    SMS,
    Push
}

public enum NotificationLevel
{
    Info,
    Warning,
    Error,
    Critical
}

public enum EnvironmentType
{
    Development,
    Testing,
    Staging,
    Production,
    Load,
    Demo
}