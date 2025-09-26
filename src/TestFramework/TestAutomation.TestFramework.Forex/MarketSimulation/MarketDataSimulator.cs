using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Logging;
using TestAutomation.Domain.Models.Configuration;
using TestAutomation.Domain.Models.Forex;

namespace TestAutomation.TestFramework.Forex.MarketSimulation;

/// <summary>
/// Advanced Market Data Simulator for Forex Trading Testing
/// </summary>
public interface IMarketDataSimulator
{
    Task<MarketData> GenerateMarketDataAsync(string symbol, MarketCondition condition = MarketCondition.Normal, Dictionary<string, object>? parameters = null);
    Task<IEnumerable<MarketData>> GenerateHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate, TimeSpan interval, MarketCondition condition = MarketCondition.Normal);
    Task<MarketData> SimulateNewsEventAsync(string symbol, NewsEvent newsEvent);
    Task<IEnumerable<MarketData>> SimulateMarketSessionAsync(string symbol, TradingSession session, TimeSpan duration);
    Task<MarketData> GetCurrentPriceAsync(string symbol);
    Task StartRealTimeSimulationAsync(string symbol, Action<MarketData> onPriceUpdate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of Market Data Simulator
/// </summary>
public class MarketDataSimulator : IMarketDataSimulator
{
    private readonly ForexConfiguration _config;
    private readonly ILogger<MarketDataSimulator> _logger;
    private readonly Dictionary<string, MarketData> _currentPrices = new();
    private readonly Random _random = new();

    // Base prices for major currency pairs
    private readonly Dictionary<string, decimal> _basePrices = new()
    {
        { "EURUSD", 1.0500m },
        { "GBPUSD", 1.2500m },
        { "USDJPY", 150.00m },
        { "USDCHF", 0.9200m },
        { "AUDUSD", 0.6500m },
        { "USDCAD", 1.3500m },
        { "NZDUSD", 0.6000m },
        { "EURGBP", 0.8400m },
        { "EURJPY", 157.50m },
        { "GBPJPY", 187.50m }
    };

    public MarketDataSimulator(ForexConfiguration config, ILogger<MarketDataSimulator> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize current prices
        foreach (var kvp in _basePrices)
        {
            _currentPrices[kvp.Key] = new MarketData
            {
                Symbol = kvp.Key,
                Timestamp = DateTime.UtcNow,
                Bid = kvp.Value,
                Ask = kvp.Value + GetSpread(kvp.Key),
                Volume = _random.Next(100, 10000),
                Session = GetCurrentSession()
            };
        }
    }

    public async Task<MarketData> GenerateMarketDataAsync(string symbol, MarketCondition condition = MarketCondition.Normal, Dictionary<string, object>? parameters = null)
    {
        await Task.CompletedTask;
        
        var currentPrice = GetOrCreateCurrentPrice(symbol);
        var volatility = GetVolatilityForCondition(condition);
        
        if (parameters != null)
        {
            volatility *= GetParameterValue<decimal>(parameters, "volatility", 1.0m);
        }

        var priceChange = GeneratePriceChange(currentPrice.Bid, volatility, condition);
        var newBid = Math.Max(0.0001m, currentPrice.Bid + priceChange);
        var spread = GetSpread(symbol);
        
        // Apply spread multiplier based on market condition
        if (condition == MarketCondition.Volatile || condition == MarketCondition.NewsEvent)
        {
            spread *= 2.0m;
        }

        var newMarketData = new MarketData
        {
            Symbol = symbol,
            Timestamp = DateTime.UtcNow,
            Bid = newBid,
            Ask = newBid + spread,
            Volume = GenerateVolume(condition),
            Session = GetCurrentSession()
        };

        // Add market condition specific data
        newMarketData.AdditionalData["volatility"] = volatility;
        newMarketData.AdditionalData["condition"] = condition.ToString();
        newMarketData.AdditionalData["priceChange"] = priceChange;

        _currentPrices[symbol] = newMarketData;
        
        _logger.LogTrace("Generated market data for {Symbol}: {Bid}/{Ask}", symbol, newBid, newMarketData.Ask);
        
        return newMarketData;
    }

    public async Task<IEnumerable<MarketData>> GenerateHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate, TimeSpan interval, MarketCondition condition = MarketCondition.Normal)
    {
        var historicalData = new List<MarketData>();
        var currentDate = startDate;
        var basePrice = GetBasePrice(symbol);
        var currentPrice = basePrice;

        _logger.LogInformation("Generating historical data for {Symbol} from {StartDate} to {EndDate} with {Interval} intervals", 
            symbol, startDate, endDate, interval);

        while (currentDate <= endDate)
        {
            var volatility = GetVolatilityForCondition(condition);
            var priceChange = GeneratePriceChange(currentPrice, volatility, condition);
            currentPrice = Math.Max(0.0001m, currentPrice + priceChange);
            
            var spread = GetSpread(symbol);
            var marketData = new MarketData
            {
                Symbol = symbol,
                Timestamp = currentDate,
                Bid = currentPrice,
                Ask = currentPrice + spread,
                Volume = GenerateVolume(condition),
                Session = GetSessionForTime(currentDate)
            };

            historicalData.Add(marketData);
            currentDate = currentDate.Add(interval);
            
            await Task.Delay(1); // Yield control to prevent blocking
        }

        _logger.LogInformation("Generated {Count} historical data points for {Symbol}", historicalData.Count, symbol);
        
        return historicalData;
    }

    public async Task<MarketData> SimulateNewsEventAsync(string symbol, NewsEvent newsEvent)
    {
        await Task.CompletedTask;
        
        var currentPrice = GetOrCreateCurrentPrice(symbol);
        var impactMultiplier = newsEvent.Impact switch
        {
            NewsImpact.Low => 0.5m,
            NewsImpact.Medium => 1.0m,
            NewsImpact.High => 2.0m,
            _ => 1.0m
        };

        var volatilityIncrease = newsEvent.VolatilityIncrease * impactMultiplier;
        var priceImpact = GeneratePriceChange(currentPrice.Bid, 0.01m * volatilityIncrease, MarketCondition.NewsEvent);
        
        var newBid = Math.Max(0.0001m, currentPrice.Bid + priceImpact);
        var spread = GetSpread(symbol) * volatilityIncrease; // Spreads widen during news

        var newsMarketData = new MarketData
        {
            Symbol = symbol,
            Timestamp = newsEvent.EventTime,
            Bid = newBid,
            Ask = newBid + spread,
            Volume = GenerateVolume(MarketCondition.NewsEvent) * (long)volatilityIncrease,
            Session = GetSessionForTime(newsEvent.EventTime)
        };

        newsMarketData.AdditionalData["newsEvent"] = newsEvent.Title;
        newsMarketData.AdditionalData["impact"] = newsEvent.Impact.ToString();
        newsMarketData.AdditionalData["volatilityIncrease"] = volatilityIncrease;
        newsMarketData.AdditionalData["priceImpact"] = priceImpact;

        _currentPrices[symbol] = newsMarketData;
        
        _logger.LogInformation("Simulated news event impact for {Symbol}: {EventTitle} - Price changed by {PriceImpact}", 
            symbol, newsEvent.Title, priceImpact);

        return newsMarketData;
    }

    public async Task<IEnumerable<MarketData>> SimulateMarketSessionAsync(string symbol, TradingSession session, TimeSpan duration)
    {
        var sessionData = new List<MarketData>();
        var sessionVolatility = GetSessionVolatility(session);
        var sessionSpreadMultiplier = GetSessionSpreadMultiplier(session);
        
        var startTime = DateTime.UtcNow;
        var endTime = startTime.Add(duration);
        var interval = TimeSpan.FromMinutes(1); // 1-minute data
        
        var currentTime = startTime;
        var currentPrice = GetOrCreateCurrentPrice(symbol);

        _logger.LogInformation("Simulating {Session} session for {Symbol} over {Duration}", 
            session, symbol, duration);

        while (currentTime <= endTime)
        {
            var priceChange = GeneratePriceChange(currentPrice.Bid, sessionVolatility, MarketCondition.Normal);
            var newBid = Math.Max(0.0001m, currentPrice.Bid + priceChange);
            var spread = GetSpread(symbol) * sessionSpreadMultiplier;

            var marketData = new MarketData
            {
                Symbol = symbol,
                Timestamp = currentTime,
                Bid = newBid,
                Ask = newBid + spread,
                Volume = GenerateSessionVolume(session),
                Session = GetMarketSessionType(session)
            };

            marketData.AdditionalData["sessionVolatility"] = sessionVolatility;
            marketData.AdditionalData["spreadMultiplier"] = sessionSpreadMultiplier;

            sessionData.Add(marketData);
            currentPrice = marketData;
            currentTime = currentTime.Add(interval);
            
            await Task.Delay(10); // Small delay to simulate real-time
        }

        _currentPrices[symbol] = currentPrice;
        
        _logger.LogInformation("Generated {Count} data points for {Session} session", sessionData.Count, session);
        
        return sessionData;
    }

    public async Task<MarketData> GetCurrentPriceAsync(string symbol)
    {
        await Task.CompletedTask;
        return GetOrCreateCurrentPrice(symbol);
    }

    public async Task StartRealTimeSimulationAsync(string symbol, Action<MarketData> onPriceUpdate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting real-time simulation for {Symbol}", symbol);
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var marketData = await GenerateMarketDataAsync(symbol);
                onPriceUpdate(marketData);
                
                // Wait for next update (simulate real-time feed frequency)
                await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(100, 1000)), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Real-time simulation cancelled for {Symbol}", symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in real-time simulation for {Symbol}", symbol);
            throw;
        }
    }

    private MarketData GetOrCreateCurrentPrice(string symbol)
    {
        if (_currentPrices.TryGetValue(symbol, out var current))
        {
            return current;
        }

        var basePrice = GetBasePrice(symbol);
        var spread = GetSpread(symbol);
        
        var newMarketData = new MarketData
        {
            Symbol = symbol,
            Timestamp = DateTime.UtcNow,
            Bid = basePrice,
            Ask = basePrice + spread,
            Volume = _random.Next(100, 5000),
            Session = GetCurrentSession()
        };

        _currentPrices[symbol] = newMarketData;
        return newMarketData;
    }

    private decimal GetBasePrice(string symbol)
    {
        return _basePrices.GetValueOrDefault(symbol, 1.0000m);
    }

    private decimal GetSpread(string symbol)
    {
        // Default spreads in pips
        var spreadPips = symbol switch
        {
            "EURUSD" => 1.5m,
            "GBPUSD" => 2.0m,
            "USDJPY" => 1.5m,
            "USDCHF" => 2.5m,
            "AUDUSD" => 2.0m,
            "USDCAD" => 2.5m,
            "NZDUSD" => 3.0m,
            _ => 2.0m
        };

        // Convert pips to actual spread
        var pipValue = symbol.Contains("JPY") ? 0.01m : 0.0001m;
        return spreadPips * pipValue;
    }

    private decimal GetVolatilityForCondition(MarketCondition condition)
    {
        return condition switch
        {
            MarketCondition.Normal => 0.0005m,
            MarketCondition.Volatile => 0.002m,
            MarketCondition.Trending => 0.001m,
            MarketCondition.Ranging => 0.0003m,
            MarketCondition.Breakout => 0.003m,
            MarketCondition.Reversal => 0.0015m,
            MarketCondition.NewsEvent => 0.005m,
            MarketCondition.LowLiquidity => 0.0008m,
            MarketCondition.HighLiquidity => 0.0003m,
            _ => 0.0005m
        };
    }

    private decimal GeneratePriceChange(decimal currentPrice, decimal volatility, MarketCondition condition)
    {
        // Generate normally distributed random price change
        var random1 = _random.NextDouble();
        var random2 = _random.NextDouble();
        var normalRandom = Math.Sqrt(-2.0 * Math.Log(random1)) * Math.Cos(2.0 * Math.PI * random2);
        
        var change = (decimal)normalRandom * volatility * currentPrice;
        
        // Apply trend bias for certain conditions
        if (condition == MarketCondition.Trending)
        {
            var trendBias = _random.NextDouble() > 0.5 ? 0.3m : -0.3m;
            change += change * trendBias;
        }
        
        return change;
    }

    private long GenerateVolume(MarketCondition condition)
    {
        var baseVolume = condition switch
        {
            MarketCondition.Normal => _random.Next(500, 2000),
            MarketCondition.Volatile => _random.Next(2000, 10000),
            MarketCondition.NewsEvent => _random.Next(5000, 50000),
            MarketCondition.LowLiquidity => _random.Next(100, 500),
            MarketCondition.HighLiquidity => _random.Next(3000, 15000),
            _ => _random.Next(500, 2000)
        };

        return baseVolume;
    }

    private long GenerateSessionVolume(TradingSession session)
    {
        return session switch
        {
            TradingSession.Sydney => _random.Next(200, 1000),
            TradingSession.Tokyo => _random.Next(800, 3000),
            TradingSession.London => _random.Next(2000, 8000),
            TradingSession.NewYork => _random.Next(1500, 6000),
            _ => _random.Next(500, 2000)
        };
    }

    private decimal GetSessionVolatility(TradingSession session)
    {
        return session switch
        {
            TradingSession.Sydney => 0.0003m,
            TradingSession.Tokyo => 0.0005m,
            TradingSession.London => 0.0008m,
            TradingSession.NewYork => 0.0007m,
            _ => 0.0005m
        };
    }

    private decimal GetSessionSpreadMultiplier(TradingSession session)
    {
        return session switch
        {
            TradingSession.Sydney => 1.5m, // Wider spreads during low liquidity
            TradingSession.Tokyo => 1.2m,
            TradingSession.London => 1.0m, // Best spreads during peak hours
            TradingSession.NewYork => 1.0m,
            _ => 1.3m
        };
    }

    private MarketSessionType GetCurrentSession()
    {
        var utcNow = DateTime.UtcNow;
        var hour = utcNow.Hour;

        // Approximate session times (UTC)
        return hour switch
        {
            >= 22 or < 7 => MarketSessionType.Sydney,
            >= 0 and < 9 => MarketSessionType.Tokyo,
            >= 7 and < 16 => MarketSessionType.London,
            >= 13 and < 22 => MarketSessionType.NewYork,
            _ => MarketSessionType.Unknown
        };
    }

    private MarketSessionType GetSessionForTime(DateTime time)
    {
        var hour = time.Hour;
        return hour switch
        {
            >= 22 or < 7 => MarketSessionType.Sydney,
            >= 0 and < 9 => MarketSessionType.Tokyo,
            >= 7 and < 16 => MarketSessionType.London,
            >= 13 and < 22 => MarketSessionType.NewYork,
            _ => MarketSessionType.Unknown
        };
    }

    private MarketSessionType GetMarketSessionType(TradingSession session)
    {
        return session switch
        {
            TradingSession.Sydney => MarketSessionType.Sydney,
            TradingSession.Tokyo => MarketSessionType.Tokyo,
            TradingSession.London => MarketSessionType.London,
            TradingSession.NewYork => MarketSessionType.NewYork,
            _ => MarketSessionType.Unknown
        };
    }

    private T GetParameterValue<T>(Dictionary<string, object> parameters, string key, T defaultValue)
    {
        if (parameters.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
}