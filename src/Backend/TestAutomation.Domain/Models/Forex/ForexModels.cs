namespace TestAutomation.Domain.Models.Forex;

/// <summary>
/// Represents a Forex trading pair (e.g., EUR/USD, GBP/JPY)
/// </summary>
public class CurrencyPair : BaseEntity
{
    public string Symbol { get; set; } = string.Empty; // e.g., "EURUSD"
    public string BaseCurrency { get; set; } = string.Empty; // e.g., "EUR"
    public string QuoteCurrency { get; set; } = string.Empty; // e.g., "USD"
    public string DisplayName { get; set; } = string.Empty; // e.g., "EUR/USD"
    public int PipPosition { get; set; } = 4; // Decimal position for pip calculation
    public decimal MinLotSize { get; set; } = 0.01m;
    public decimal MaxLotSize { get; set; } = 100m;
    public decimal LotStep { get; set; } = 0.01m;
    public decimal MarginRate { get; set; } = 0.02m; // 2% margin requirement
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Represents market data for testing
/// </summary>
public class MarketData : BaseEntity
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public decimal Spread => Ask - Bid;
    public long Volume { get; set; }
    public MarketSessionType Session { get; set; } = MarketSessionType.Unknown;
    public Dictionary<string, decimal> AdditionalData { get; set; } = new(); // For custom indicators, etc.
}

/// <summary>
/// Represents a simulated trading order
/// </summary>
public class Order : BaseEntity
{
    public string OrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderType Type { get; set; } = OrderType.Market;
    public OrderSide Side { get; set; } = OrderSide.Buy;
    public decimal Volume { get; set; }
    public decimal? OpenPrice { get; set; }
    public decimal? ClosePrice { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime? OpenTime { get; set; }
    public DateTime? CloseTime { get; set; }
    public decimal Commission { get; set; }
    public decimal Swap { get; set; }
    public decimal ProfitLoss { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
}

/// <summary>
/// Represents a trading account for testing
/// </summary>
public class TradingAccount : BaseEntity
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal Balance { get; set; } = 10000m; // Default demo balance
    public decimal Equity { get; set; } = 10000m;
    public decimal Margin { get; set; } = 0m;
    public decimal FreeMargin { get; set; } = 10000m;
    public decimal MarginLevel { get; set; } = 0m;
    public AccountType Type { get; set; } = AccountType.Demo;
    public int Leverage { get; set; } = 100; // 1:100 leverage
    public List<Order> Orders { get; set; } = new();
    public List<Position> Positions { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Represents an open position
/// </summary>
public class Position : BaseEntity
{
    public string PositionId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public PositionType Type { get; set; } = PositionType.Long;
    public decimal Volume { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime OpenTime { get; set; }
    public decimal ProfitLoss => CalculatePnL();
    public decimal Commission { get; set; }
    public decimal Swap { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;

    private decimal CalculatePnL()
    {
        var priceDiff = Type == PositionType.Long ? CurrentPrice - OpenPrice : OpenPrice - CurrentPrice;
        return priceDiff * Volume * 100000; // Assuming standard lot calculation
    }
}

/// <summary>
/// Represents risk management parameters for testing
/// </summary>
public class RiskParameters : BaseEntity
{
    public string AccountId { get; set; } = string.Empty;
    public decimal MaxRiskPerTrade { get; set; } = 0.02m; // 2% max risk per trade
    public decimal MaxDailyLoss { get; set; } = 0.05m; // 5% max daily loss
    public decimal MaxDrawdown { get; set; } = 0.10m; // 10% max drawdown
    public int MaxOpenPositions { get; set; } = 10;
    public decimal MaxLotSize { get; set; } = 10m;
    public bool UseStopLoss { get; set; } = true;
    public bool UseTakeProfit { get; set; } = true;
    public TimeSpan MaxOrderLifetime { get; set; } = TimeSpan.FromHours(24);
}

/// <summary>
/// Represents market simulation scenarios for testing
/// </summary>
public class MarketScenario : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ScenarioType Type { get; set; } = ScenarioType.Normal;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<string> Symbols { get; set; } = new();
    public Dictionary<string, decimal> VolatilityMultipliers { get; set; } = new();
    public Dictionary<string, decimal> TrendDirections { get; set; } = new(); // -1 to 1, where -1 is strong bearish, 1 is strong bullish
    public List<NewsEvent> NewsEvents { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Represents a news event that affects market conditions
/// </summary>
public class NewsEvent : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public string Currency { get; set; } = string.Empty;
    public NewsImpact Impact { get; set; } = NewsImpact.Medium;
    public List<string> AffectedSymbols { get; set; } = new();
    public decimal VolatilityIncrease { get; set; } = 1.5m; // Multiplier for volatility
    public TimeSpan EffectDuration { get; set; } = TimeSpan.FromMinutes(30);
}

// Enums for Forex domain
public enum MarketSessionType
{
    Unknown,
    Sydney,
    Tokyo,
    London,
    NewYork,
    Overlap
}

public enum OrderType
{
    Market,
    Limit,
    Stop,
    StopLimit
}

public enum OrderSide
{
    Buy,
    Sell
}

public enum OrderStatus
{
    Pending,
    Filled,
    PartiallyFilled,
    Cancelled,
    Rejected,
    Expired
}

public enum AccountType
{
    Demo,
    Live,
    Test
}

public enum PositionType
{
    Long,
    Short
}

public enum ScenarioType
{
    Normal,
    HighVolatility,
    LowVolatility,
    TrendingUp,
    TrendingDown,
    Sideways,
    NewsEvent,
    MarketCrash,
    Custom
}

public enum NewsImpact
{
    Low,
    Medium,
    High
}