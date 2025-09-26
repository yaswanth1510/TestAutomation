using TestAutomation.Domain.Models;
using TestAutomation.TestFramework.Core.Parameters;
using TestAutomation.TestFramework.Core.Execution;
using TestAutomation.TestFramework.Core.Assets;

namespace TestAutomation.Demo;

/// <summary>
/// Demo application showcasing the Test Automation Framework capabilities
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Advanced .NET C# Test Automation Suite for Forex Trading");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine();

        await DemoParameterGeneration();
        Console.WriteLine();
        
        await DemoStepExecution();
        Console.WriteLine();
        
        await DemoAssetManagement();
        Console.WriteLine();
        
        await DemoForexSpecificFeatures();
        
        Console.WriteLine();
        Console.WriteLine("✅ Demo completed successfully!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static async Task DemoParameterGeneration()
    {
        Console.WriteLine("📊 Parameter Generation Demo");
        Console.WriteLine(new string('-', 30));

        var parameterGenerator = new ParameterGenerator();

        // Generate basic parameters
        var email = parameterGenerator.Generate<string>("email");
        var amount = parameterGenerator.Generate<decimal>("amount", new() { ["min"] = 100m, ["max"] = 1000m });
        var isDemo = parameterGenerator.Generate<bool>("isDemo");
        var testDate = parameterGenerator.Generate<DateTime>("testDate");

        Console.WriteLine($"Generated Email: {email}");
        Console.WriteLine($"Generated Amount: ${amount:F2}");
        Console.WriteLine($"Generated IsDemo: {isDemo}");
        Console.WriteLine($"Generated Date: {testDate:yyyy-MM-dd}");

        // Generate parameter sets
        var parameterTypes = new Dictionary<string, Type>
        {
            ["UserEmail"] = typeof(string),
            ["TradeAmount"] = typeof(decimal),
            ["IsDemo"] = typeof(bool),
            ["TestDate"] = typeof(DateTime)
        };

        Console.WriteLine("\n📋 Generated Parameter Set:");
        var parameters = parameterGenerator.GenerateParameters(parameterTypes);
        foreach (var param in parameters)
        {
            Console.WriteLine($"  {param.Key}: {param.Value}");
        }

        await Task.CompletedTask;
    }

    static async Task DemoStepExecution()
    {
        Console.WriteLine("⚡ Step Execution Demo");
        Console.WriteLine(new string('-', 25));

        var stepManager = new StepManager();
        
        // Create sample test steps
        var testSteps = new List<TestStep>
        {
            new TestStep
            {
                Name = "Setup Test Environment",
                Order = 1,
                Type = TestStepType.Setup,
                Action = "Initialize",
                Parameters = new Dictionary<string, object> { ["Environment"] = "Demo" }
            },
            new TestStep
            {
                Name = "Navigate to Login Page",
                Order = 2,
                Type = TestStepType.Navigation,
                Action = "Navigate",
                Parameters = new Dictionary<string, object> { ["url"] = "https://demo.forex.com/login" }
            },
            new TestStep
            {
                Name = "Enter Credentials",
                Order = 3,
                Type = TestStepType.Action,
                Action = "EnterCredentials",
                Parameters = new Dictionary<string, object> { ["username"] = "demo", ["password"] = "test123" }
            },
            new TestStep
            {
                Name = "Verify Login Success",
                Order = 4,
                Type = TestStepType.Verification,
                Action = "Verify",
                ExpectedResult = "Login successful"
            },
            new TestStep
            {
                Name = "Cleanup",
                Order = 5,
                Type = TestStepType.Cleanup,
                Action = "Cleanup"
            }
        };

        var context = new Dictionary<string, object>
        {
            ["TestEnvironment"] = "Demo",
            ["lastResult"] = "Login successful"
        };

        Console.WriteLine("Executing test steps...\n");
        var results = await stepManager.ExecuteStepsAsync(testSteps, context);

        foreach (var result in results)
        {
            var statusIcon = result.Status switch
            {
                TestStepResultStatus.Passed => "✅",
                TestStepResultStatus.Failed => "❌",
                TestStepResultStatus.Warning => "⚠️",
                _ => "⏳"
            };

            Console.WriteLine($"{statusIcon} Step {result.StepOrder}: {result.StepName}");
            Console.WriteLine($"   Status: {result.Status}");
            Console.WriteLine($"   Duration: {result.Duration?.TotalMilliseconds:F0}ms");
            if (!string.IsNullOrEmpty(result.ActualResult))
            {
                Console.WriteLine($"   Result: {result.ActualResult}");
            }
            Console.WriteLine();
        }
    }

    static async Task DemoAssetManagement()
    {
        Console.WriteLine("🗂️ Asset Management Demo");
        Console.WriteLine(new string('-', 28));

        var assetManager = new AssetManager();

        // Capture screenshot
        Console.WriteLine("📸 Capturing screenshot...");
        var screenshot = await assetManager.CaptureScreenshotAsync("demo_screenshot", "Demo application screenshot");
        Console.WriteLine($"   Screenshot saved: {screenshot.Name} ({screenshot.FileSize} bytes)");

        // Save log
        Console.WriteLine("\n📝 Saving execution log...");
        var logContent = $"Demo execution started at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n" +
                        "Step 1: Initialize demo environment - SUCCESS\n" +
                        "Step 2: Generate test parameters - SUCCESS\n" +
                        "Step 3: Execute test steps - SUCCESS\n" +
                        "Demo execution completed successfully.";
        
        var logAsset = await assetManager.SaveLogAsync("demo_execution_log", logContent, "Demo execution log");
        Console.WriteLine($"   Log saved: {logAsset.Name} ({logAsset.FileSize} bytes)");

        // Create test report
        Console.WriteLine("\n📊 Creating test report...");
        var reportData = new
        {
            TestName = "Demo Test Suite",
            ExecutionTime = DateTime.UtcNow,
            Status = "Passed",
            Duration = "2.5 seconds",
            StepsExecuted = 5,
            StepsPassed = 5,
            StepsFailed = 0,
            Environment = "Demo",
            Assets = new[] { screenshot.Name, logAsset.Name }
        };

        var report = await assetManager.CreateReportAsync("demo_test_report", reportData, "Demo test execution report");
        Console.WriteLine($"   Report created: {report.Name} ({report.FileSize} bytes)");

        Console.WriteLine($"\n📁 All assets saved to: TestAssets/");
    }

    static async Task DemoForexSpecificFeatures()
    {
        Console.WriteLine("🏦 Forex-Specific Features Demo");
        Console.WriteLine(new string('-', 35));

        var forexGenerator = new ForexParameterGenerator();

        // Generate forex data
        Console.WriteLine("💱 Generating Forex Trading Data:");
        
        for (int i = 0; i < 3; i++)
        {
            var currencyPair = forexGenerator.GenerateCurrencyPair();
            var price = forexGenerator.GeneratePrice(currencyPair);
            var lotSize = forexGenerator.GenerateLotSize();
            var leverage = forexGenerator.GenerateLeverage();
            var balance = forexGenerator.GenerateBalance();

            Console.WriteLine($"\n  Trade Scenario {i + 1}:");
            Console.WriteLine($"    Currency Pair: {currencyPair}");
            Console.WriteLine($"    Current Price: {price:F5}");
            Console.WriteLine($"    Lot Size: {lotSize}");
            Console.WriteLine($"    Leverage: 1:{leverage}");
            Console.WriteLine($"    Account Balance: ${balance:F2}");
        }

        // Demonstrate market simulation
        Console.WriteLine("\n📈 Market Simulation Scenarios:");
        
        var scenarios = new[]
        {
            new { Name = "High Volatility", Volatility = 0.05m },
            new { Name = "Normal Market", Volatility = 0.01m },
            new { Name = "Low Volatility", Volatility = 0.005m }
        };

        foreach (var scenario in scenarios)
        {
            var constraints = new Dictionary<string, object> { ["volatility"] = scenario.Volatility };
            var prices = new List<decimal>();
            
            for (int i = 0; i < 5; i++)
            {
                prices.Add(forexGenerator.GeneratePrice("EURUSD", constraints));
            }

            Console.WriteLine($"\n  {scenario.Name} (Volatility: {scenario.Volatility:P1}):");
            Console.WriteLine($"    EURUSD Prices: {string.Join(", ", prices.Select(p => p.ToString("F5")))}");
            Console.WriteLine($"    Price Range: {prices.Min():F5} - {prices.Max():F5}");
        }

        // Create forex domain models
        Console.WriteLine("\n🏪 Creating Forex Domain Models:");
        
        var currencyPairModel = new TestAutomation.Domain.Models.Forex.CurrencyPair
        {
            Symbol = "EURUSD",
            BaseCurrency = "EUR",
            QuoteCurrency = "USD",
            DisplayName = "EUR/USD",
            PipPosition = 4,
            MinLotSize = 0.01m,
            MaxLotSize = 100m,
            MarginRate = 0.02m,
            IsActive = true
        };

        var tradingAccount = new TestAutomation.Domain.Models.Forex.TradingAccount
        {
            AccountNumber = "DEMO123456",
            AccountName = "Demo Trading Account",
            Currency = "USD",
            Balance = 10000m,
            Equity = 10000m,
            Type = TestAutomation.Domain.Models.Forex.AccountType.Demo,
            Leverage = 100,
            IsActive = true
        };

        var order = new TestAutomation.Domain.Models.Forex.Order
        {
            OrderId = Guid.NewGuid().ToString(),
            Symbol = currencyPairModel.Symbol,
            Type = TestAutomation.Domain.Models.Forex.OrderType.Market,
            Side = TestAutomation.Domain.Models.Forex.OrderSide.Buy,
            Volume = 0.1m,
            OpenPrice = forexGenerator.GeneratePrice("EURUSD"),
            Status = TestAutomation.Domain.Models.Forex.OrderStatus.Filled,
            AccountId = tradingAccount.AccountNumber
        };

        Console.WriteLine($"  Currency Pair: {currencyPairModel.DisplayName}");
        Console.WriteLine($"  Trading Account: {tradingAccount.AccountName} (Balance: ${tradingAccount.Balance:F2})");
        Console.WriteLine($"  Sample Order: {order.Side} {order.Volume} {order.Symbol} @ {order.OpenPrice:F5}");

        await Task.CompletedTask;
    }
}
