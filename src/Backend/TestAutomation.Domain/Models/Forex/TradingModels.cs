namespace TestAutomation.Domain.Models.Forex;

/// <summary>
/// Represents a comprehensive trading strategy for testing
/// </summary>
public class TradingStrategy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public StrategyType Type { get; set; } = StrategyType.Manual;
    public List<string> SupportedSymbols { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public RiskParameters RiskSettings { get; set; } = new();
    public List<TradingRule> Rules { get; set; } = new();
    public decimal MinimumBalance { get; set; } = 1000m;
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(30);
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Represents individual trading rules within a strategy
/// </summary>
public class TradingRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RuleType Type { get; set; } = RuleType.Entry;
    public string Condition { get; set; } = string.Empty; // JSON condition
    public string Action { get; set; } = string.Empty; // JSON action
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents market simulation configuration for testing
/// </summary>
public class MarketSimulation : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> Symbols { get; set; } = new();
    public MarketCondition Condition { get; set; } = MarketCondition.Normal;
    public decimal VolatilityMultiplier { get; set; } = 1.0m;
    public List<EconomicEvent> EconomicEvents { get; set; } = new();
    public Dictionary<string, decimal> SpreadMultipliers { get; set; } = new();
    public bool SimulateSlippage { get; set; } = true;
    public decimal MaxSlippagePips { get; set; } = 3m;
}

/// <summary>
/// Represents economic events that affect market conditions
/// </summary>
public class EconomicEvent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDateTime { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public EconomicImpact Impact { get; set; } = EconomicImpact.Medium;
    public decimal ExpectedValue { get; set; }
    public decimal ActualValue { get; set; }
    public decimal PreviousValue { get; set; }
    public List<string> AffectedSymbols { get; set; } = new();
    public TimeSpan ImpactDuration { get; set; } = TimeSpan.FromMinutes(30);
}

/// <summary>
/// Represents a trading session for backtesting
/// </summary>
public class BacktestingSession : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid StrategyId { get; set; }
    public TradingStrategy? Strategy { get; set; }
    public Guid SimulationId { get; set; }
    public MarketSimulation? Simulation { get; set; }
    public decimal InitialBalance { get; set; } = 10000m;
    public string Currency { get; set; } = "USD";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public BacktestStatus Status { get; set; } = BacktestStatus.NotStarted;
    public List<BacktestResult> Results { get; set; } = new();
    public BacktestMetrics? Metrics { get; set; }
}

/// <summary>
/// Represents backtesting results and performance metrics
/// </summary>
public class BacktestResult : BaseEntity
{
    public Guid SessionId { get; set; }
    public BacktestingSession? Session { get; set; }
    public DateTime TestDate { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal OpenPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Volume { get; set; }
    public OrderSide Side { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal Commission { get; set; }
    public decimal Swap { get; set; }
    public TimeSpan Duration { get; set; }
    public string Signal { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents comprehensive backtesting performance metrics
/// </summary>
public class BacktestMetrics : BaseEntity
{
    public Guid SessionId { get; set; }
    public BacktestingSession? Session { get; set; }
    
    // Performance Metrics
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercentage { get; set; }
    public decimal AnnualizedReturn { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownPercentage { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal SortinoRatio { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal RecoveryFactor { get; set; }
    
    // Trading Statistics
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal LargestWin { get; set; }
    public decimal LargestLoss { get; set; }
    public decimal AverageTradeReturn { get; set; }
    
    // Risk Metrics
    public decimal ValueAtRisk { get; set; } // 95% VaR
    public decimal ConditionalValueAtRisk { get; set; } // CVaR
    public decimal BetaCoefficient { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal DownsideDeviation { get; set; }
    
    // Time-based Metrics
    public TimeSpan TotalTradingTime { get; set; }
    public TimeSpan AverageTradeDuration { get; set; }
    public decimal TradesPerDay { get; set; }
    
    // Additional Metrics
    public Dictionary<string, decimal> SymbolPerformance { get; set; } = new();
    public Dictionary<string, int> SymbolTradeCount { get; set; } = new();
    public List<DrawdownPeriod> DrawdownPeriods { get; set; } = new();
}

/// <summary>
/// Represents a drawdown period during backtesting
/// </summary>
public class DrawdownPeriod : BaseEntity
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownPercentage { get; set; }
    public TimeSpan Duration { get; set; }
    public TimeSpan RecoveryTime { get; set; }
    public bool IsRecovered { get; set; }
}

/// <summary>
/// Represents compliance and regulatory testing parameters
/// </summary>
public class ComplianceTest : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceType Type { get; set; } = ComplianceType.KYC;
    public string Regulation { get; set; } = string.Empty; // e.g., "MiFID II", "ESMA", "CFTC"
    public List<ComplianceRule> Rules { get; set; } = new();
    public ComplianceStatus Status { get; set; } = ComplianceStatus.Pending;
    public DateTime LastTestedDate { get; set; }
    public List<ComplianceViolation> Violations { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Represents individual compliance rules
/// </summary>
public class ComplianceRule : BaseEntity
{
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceRuleType Type { get; set; } = ComplianceRuleType.Mandatory;
    public string ValidationExpression { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public ComplianceRuleStatus Status { get; set; } = ComplianceRuleStatus.Active;
}

/// <summary>
/// Represents compliance violations found during testing
/// </summary>
public class ComplianceViolation : BaseEntity
{
    public Guid ComplianceTestId { get; set; }
    public ComplianceTest? ComplianceTest { get; set; }
    public Guid RuleId { get; set; }
    public ComplianceRule? Rule { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ViolationSeverity Severity { get; set; } = ViolationSeverity.Medium;
    public DateTime DetectedAt { get; set; }
    public string Context { get; set; } = string.Empty; // JSON context data
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
}

// Additional Enums for Forex Trading Domain

public enum StrategyType
{
    Manual,
    Algorithmic,
    Scalping,
    DayTrading,
    SwingTrading,
    PositionTrading,
    Arbitrage,
    CarryTrade,
    GridTrading,
    Martingale
}

public enum RuleType
{
    Entry,
    Exit,
    RiskManagement,
    MoneyManagement,
    TimeFilter,
    MarketCondition
}

public enum MarketCondition
{
    Normal,
    Volatile,
    Trending,
    Ranging,
    Breakout,
    Reversal,
    NewsEvent,
    LowLiquidity,
    HighLiquidity
}

public enum EconomicImpact
{
    Low,
    Medium,
    High,
    Extreme
}

public enum BacktestStatus
{
    NotStarted,
    Initializing,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}

public enum ComplianceType
{
    KYC,
    AML,
    RiskManagement,
    Reporting,
    RecordKeeping,
    ClientProtection,
    MarketIntegrity,
    SystemicRisk
}

public enum ComplianceStatus
{
    Pending,
    InProgress,
    Passed,
    Failed,
    PartiallyCompliant,
    RequiresReview
}

public enum ComplianceRuleType
{
    Mandatory,
    Recommended,
    Optional,
    Informational
}

public enum ComplianceRuleStatus
{
    Active,
    Inactive,
    Deprecated,
    UnderReview
}

public enum ViolationSeverity
{
    Low,
    Medium,
    High,
    Critical
}