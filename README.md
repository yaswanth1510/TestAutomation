# Advanced .NET C# Test Automation Suite for Forex Trading App Testing

A comprehensive, asynchronous test automation framework designed specifically for Forex trading applications, built with .NET 8 and modern architectural patterns.

## 🚀 Key Features

### 📚 Smart Test and Script Collector
- **Automatic Discovery**: Automatically discovers and organizes tests and scripts from assemblies
- **Intelligent Categorization**: Categorizes tests by type (Unit, Integration, E2E, Performance, Forex)
- **Tag-based Organization**: Support for tags and categories for flexible test organization
- **Metadata Extraction**: Extracts test metadata including priorities, descriptions, and parameters

### 🖥️ Web Interface (Planned)
- **Real-time Execution Tracking**: Monitor test execution in real-time
- **Interactive Configuration**: Configure test parameters and environments through UI
- **Detailed Reporting Dashboard**: Comprehensive test results with drill-down capabilities

### 📊 Detailed Reports
- **Step-by-step Logs**: Detailed execution logs with timestamps and durations
- **Screenshots & Assets**: Automatic capture and management of test artifacts
- **Environment Details**: Complete environment and configuration reporting
- **Multiple Formats**: Support for JSON, HTML, and custom report formats

### ⚡ Fully Asynchronous Execution
- **High Performance**: Built on async/await for maximum performance
- **Parallel Execution**: Run multiple tests simultaneously
- **Resource Optimization**: Efficient resource usage with async patterns

### 🔧 Flexible Environment Configuration
- **Dynamic Parameters**: Generate test data dynamically with constraints
- **Environment Management**: Manage multiple test environments
- **Configuration Injection**: Inject configuration at runtime

### 🛠️ Extensive Utility Toolkit
- **Step Manager**: Manage complex test step execution
- **Parameter Generators**: Generate realistic test data with constraints
- **Asset Management**: Handle screenshots, logs, files, and reports
- **Model Comparison**: Compare objects and validate results

### 🧩 Full .NET Freedom
- **No Restrictions**: Write tests using full .NET capabilities
- **Modern Architecture**: Clean architecture with separation of concerns
- **Extensible**: Plugin architecture for custom handlers and generators

## 🏗️ Architecture

The solution follows Clean Architecture principles with clear separation of concerns:

```
├── Backend (.NET 8)
│   ├── TestAutomation.Api          # ASP.NET Core Web API
│   ├── TestAutomation.Application  # Application layer
│   ├── TestAutomation.Domain       # Domain models and entities
│   └── TestAutomation.Infrastructure # Infrastructure services
│
├── Test Framework
│   ├── TestAutomation.TestFramework.Core  # Core framework components
│   └── TestAutomation.TestFramework.Forex # Forex-specific extensions
│
└── Tests
    ├── TestAutomation.Tests.Unit        # Unit tests
    └── TestAutomation.Tests.Integration # Integration tests
```

## 🏦 Forex Trading Specific Features

### Market Data Simulation
- **Realistic Price Movements**: Simulate real market conditions with volatility
- **Multiple Currency Pairs**: Support for major and minor currency pairs
- **Market Sessions**: Simulate different trading sessions (London, New York, Tokyo, Sydney)
- **News Events**: Simulate market-moving news events and their impact

### Trading API Testing
- **Order Management**: Test order placement, modification, and cancellation
- **Position Management**: Validate position opening, closing, and management
- **Account Management**: Test account balance, margin, and equity calculations
- **Risk Management**: Validate stop-loss, take-profit, and risk parameters

### Financial Calculations
- **P&L Calculations**: Accurate profit and loss calculations
- **Margin Requirements**: Test margin calculations for different instruments
- **Swap/Rollover**: Validate overnight fees and rollover charges
- **Leverage Testing**: Test different leverage scenarios

### Regulatory Compliance Testing
- **KYC/AML Workflows**: Test customer verification processes
- **Reporting Accuracy**: Validate regulatory reporting requirements
- **Audit Trails**: Test complete audit trail functionality

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- SQL Server (for data persistence)
- Redis (for caching)

### Installation

1. **Clone the repository**
```bash
git clone https://github.com/yaswanth1510/TestAutomation.git
cd TestAutomation
```

2. **Restore NuGet packages**
```bash
dotnet restore
```

3. **Build the solution**
```bash
dotnet build
```

4. **Run the tests**
```bash
dotnet test
```

### Quick Start Example

```csharp
using TestAutomation.TestFramework.Core.Discovery;
using TestAutomation.TestFramework.Core.Execution;
using TestAutomation.TestFramework.Core.Parameters;
using TestAutomation.TestFramework.Forex;

// Discover tests
var discoveryService = new TestDiscoveryService();
var testSuites = await discoveryService.DiscoverTestSuitesAsync("./TestAssemblies");

// Generate test parameters
var parameterGenerator = new ForexParameterGenerator();
var currencyPair = parameterGenerator.GenerateCurrencyPair(); // "EURUSD"
var price = parameterGenerator.GeneratePrice(currencyPair);   // 1.0523m
var lotSize = parameterGenerator.GenerateLotSize();           // 0.1m

// Execute test steps
var stepManager = new StepManager();
var context = new Dictionary<string, object>
{
    ["CurrencyPair"] = currencyPair,
    ["Price"] = price,
    ["LotSize"] = lotSize
};

var testSteps = testSuite.TestCases.First().Steps;
var results = await stepManager.ExecuteStepsAsync(testSteps, context);

// Manage assets
var assetManager = new AssetManager();
var screenshot = await assetManager.CaptureScreenshotAsync("test_execution");
var logAsset = await assetManager.SaveLogAsync("execution_log", "Test completed successfully");
```

## 🧪 Test Discovery

The framework automatically discovers tests from assemblies and organizes them:

```csharp
var discoveryService = new TestDiscoveryService();

// Discover all test suites in a directory
var testSuites = await discoveryService.DiscoverTestSuitesAsync("./Tests");

// Get available tags for filtering
var tags = await discoveryService.GetAvailableTagsAsync("./Tests");

// Discover specific test cases
var testCases = await discoveryService.DiscoverTestCasesAsync("MyTests.dll");
```

## 📊 Parameter Generation

Generate realistic test data with constraints:

```csharp
var parameterGenerator = new ParameterGenerator();

// Generate basic types
var email = parameterGenerator.Generate<string>("email");
var amount = parameterGenerator.Generate<decimal>("amount", new() { ["min"] = 100m, ["max"] = 1000m });

// Generate parameter sets
var parameterTypes = new Dictionary<string, Type>
{
    ["UserEmail"] = typeof(string),
    ["TradeAmount"] = typeof(decimal),
    ["IsDemo"] = typeof(bool)
};

var parameters = parameterGenerator.GenerateParameters(parameterTypes);
```

## 🏦 Forex-Specific Testing

```csharp
var forexGenerator = new ForexParameterGenerator();

// Generate forex-specific data
var currencyPair = forexGenerator.GenerateCurrencyPair();     // "GBPUSD"
var price = forexGenerator.GeneratePrice(currencyPair);       // 1.2534m
var lotSize = forexGenerator.GenerateLotSize();               // 0.5m
var leverage = forexGenerator.GenerateLeverage();             // 100
var balance = forexGenerator.GenerateBalance();               // 25000m

// Test trading scenarios
var constraints = new Dictionary<string, object>
{
    ["volatility"] = 0.02m,  // 2% volatility
    ["minLot"] = 0.01m,
    ["maxLot"] = 5.0m
};

var volatilePrice = forexGenerator.GeneratePrice("EURUSD", constraints);
```

## 🗂️ Asset Management

Automatically capture and manage test artifacts:

```csharp
var assetManager = new AssetManager();

// Capture screenshots
var screenshot = await assetManager.CaptureScreenshotAsync("login_page");

// Save logs
await assetManager.SaveLogAsync("execution_log", logContent);

// Save test reports
var reportData = new { TestName = "Login Test", Status = "Passed", Duration = "2.3s" };
await assetManager.CreateReportAsync("login_test_report", reportData);

// Cleanup old assets
await assetManager.CleanupOldAssetsAsync(TimeSpan.FromDays(30));
```

## 🎯 Step Management

Execute complex test workflows:

```csharp
var stepManager = new StepManager();

// Register custom step handlers
stepManager.RegisterStepHandler("DatabaseQuery", new SqlStepHandler());
stepManager.RegisterStepHandler("ApiCall", new RestApiStepHandler());

// Execute steps with context
var context = new Dictionary<string, object>();
var results = await stepManager.ExecuteStepsAsync(testSteps, context);

// Handle step results
foreach (var result in results)
{
    Console.WriteLine($"Step: {result.StepName}, Status: {result.Status}, Duration: {result.Duration}");
}
```

## 🧪 Sample Test Cases

The framework supports various testing approaches:

### NUnit Tests
```csharp
[TestFixture]
[Category("Forex")]
public class TradingTests
{
    [Test]
    [Priority(TestCasePriority.High)]
    [Tag("Order", "Validation")]
    public async Task Should_PlaceOrder_When_ValidParameters()
    {
        // Test implementation
        Assert.Pass("Order placed successfully");
    }
}
```

### Custom Test Framework
```csharp
public class ForexTradingTestSuite : TestSuite
{
    [ForexTest("Order Placement")]
    [Priority(Critical)]
    public async Task<TestResult> TestOrderPlacement()
    {
        var paramGen = new ForexParameterGenerator();
        var order = new Order
        {
            Symbol = paramGen.GenerateCurrencyPair(),
            Volume = paramGen.GenerateLotSize(),
            Type = OrderType.Market
        };

        // Execute test logic
        return TestResult.Pass();
    }
}
```

## 📈 Technology Stack

### Backend
- **.NET 8** - Latest LTS with high performance
- **ASP.NET Core** - Web API for RESTful endpoints  
- **Entity Framework Core** - Database ORM
- **SignalR** - Real-time notifications (planned)
- **Hangfire** - Background job processing (planned)
- **Serilog** - Structured logging (planned)

### Test Framework
- **NUnit** - Primary test framework
- **Microsoft.Extensions.Logging** - Logging abstractions
- **System.Text.Json** - JSON serialization
- **Reflection APIs** - Dynamic test discovery

### Planned Integrations
- **Selenium WebDriver** - UI testing
- **RestSharp** - API testing
- **NBomber** - Load testing
- **Redis** - Caching and distributed locking
- **Azure Blob Storage** - Asset storage

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/new-feature`
3. Make your changes and add tests
4. Ensure all tests pass: `dotnet test`
5. Commit your changes: `git commit -am 'Add new feature'`
6. Push to the branch: `git push origin feature/new-feature`
7. Create a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🚧 Roadmap

- [ ] Complete Web Interface with React
- [ ] SignalR integration for real-time updates
- [ ] Hangfire for background job processing
- [ ] Redis caching implementation
- [ ] Selenium WebDriver integration
- [ ] REST API testing framework
- [ ] Load testing with NBomber
- [ ] Docker containerization
- [ ] CI/CD pipeline setup
- [ ] Comprehensive documentation
- [ ] Video tutorials and examples

## 📞 Support

For questions, issues, or contributions, please:
- Open an issue on GitHub
- Contact the maintainers
- Check the documentation

---

**Built with ❤️ for the trading community by developers who understand the complexity of financial software testing.**