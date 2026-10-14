# Host and Composition

## Overview

The composition root (`Program.cs`) is the single location where concrete implementations are selected, service lifetimes are defined, and the process lifecycle is managed. It is the only component that references `SeleniumWebProcessor`, `NuciLogger`, and `WebDriverInitialiser` directly.

## Program.cs Deep Dive

### Responsibilities

1. **Configuration Loading** — Loads `appsettings.json` with optional file and change monitoring
2. **Settings Binding** — Binds `BotSettings` and `DebugSettings` once at startup
3. **WebDriver Initialisation** — Creates `IWebDriver` before DI container construction
4. **Service Registration** — Registers all services with explicit lifetimes
5. **Startup Logging** — Records `Operation.StartUp` via `NuciLogger`
6. **Orchestration Invocation** — Resolves and calls `IBotService.Run()`
7. **Error Handling** — Catches exceptions, logs fatal events
8. **Resource Cleanup** — Calls `webDriver.Quit()` in `finally`

### Code Walkthrough

```csharp
static void Main(string[] args)
{
    // 1. Create settings objects (will be bound from config)
    BotSettings botSettings = new();
    DebugSettings debugSettings = new();

    // 2. Load configuration
    IConfiguration config = LoadConfiguration();
    config.Bind(nameof(BotSettings), botSettings);
    config.Bind(nameof(DebugSettings), debugSettings);

    // 3. Initialise WebDriver BEFORE DI container
    IWebDriver webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode);

    // 4. Build DI container
    IServiceProvider serviceProvider = new ServiceCollection()
        .AddNuciLoggerSettings(config)           // NuciLog configuration
        .AddSingleton(botSettings)               // Bound settings
        .AddSingleton(debugSettings)
        .AddSingleton<ILogger, NuciLogger>()     // Logging
        .AddSingleton(webDriver)                 // Browser driver
        .AddTransient<IWebProcessor, SeleniumWebProcessor>()  // Browser ops
        .AddSingleton<IHoneygainProcessor, HoneygainProcessor>() // Honeygain adapter
        .AddSingleton<IBotService, BotService>() // Orchestration
        .BuildServiceProvider();

    // 5. Resolve logger and record startup
    logger = serviceProvider.GetService<ILogger>();
    logger.Info(Operation.StartUp, $"Application started");

    // 6. Run orchestration with error handling
    try
    {
        serviceProvider.GetService<IBotService>().Run();
    }
    catch (AggregateException ex)
    {
        foreach (Exception innerException in ex.InnerExceptions)
        {
            logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
        }
    }
    catch (Exception ex)
    {
        logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
    }
    finally
    {
        // 7. Always quit driver
        webDriver.Quit();
    }
}

static IConfiguration LoadConfiguration() => new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", true, true)  // optional, reloadOnChange
    .Build();
```

### Service Lifetime Decisions

| Service | Lifetime | Rationale |
|---------|----------|-----------|
| `BotSettings` | Singleton | Bound once, credentials stable for process |
| `DebugSettings` | Singleton | Bound once, headless mode fixed for process |
| `ILogger` → `NuciLogger` | Singleton | Single logger instance, shared context |
| `IWebDriver` | Singleton | One browser session per process |
| `IWebProcessor` → `SeleniumWebProcessor` | Transient | Allows future per-operation isolation; currently retained by singleton processor |
| `IHoneygainProcessor` → `HoneygainProcessor` | Singleton | Maintains consistent state across operations |
| `IBotService` → `BotService` | Singleton | Single orchestration instance |

### Critical Implementation Details

#### WebDriver Initialisation Before DI
```csharp
IWebDriver webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode);
// ... then ServiceCollection
```
- Fails fast if no browser/driver available
- Driver instance registered as singleton in container
- Manual `Quit()` required in `finally` (not managed by DI)

#### Configuration Binding Once
```csharp
config.Bind(nameof(BotSettings), botSettings);
config.Bind(nameof(DebugSettings), debugSettings);
```
- `reloadOnChange: true` in `AddJsonFile` has no effect on already-bound objects
- Settings are immutable for the process lifetime

#### No ServiceProvider Disposal
```csharp
// No: serviceProvider.Dispose();
// Only:
webDriver.Quit();
```
- `IServiceProvider` not disposed — relies on process exit
- Only `IWebDriver` explicitly cleaned up

#### No Exit Code
```csharp
static void Main(string[] args)  // void return
```
- Process exits 0 unless unhandled exception
- Failures logged but not reflected in exit code

### Configuration Structure

```json
{
  "botSettings": {
    "emailAddress": "[[HONEYGAIN_EMAIL_ADDRESS]]",
    "password": "[[HONEYGAIN_PASSWORD]]"
  },
  "debugSettings": {
    "isDebugMode": false
  },
  "nuciLoggerSettings": {
    "logFilePath": "./logfile.log",
    "minimumLevel": "Info",
    "isFileOutputEnabled": true
  }
}
```

### Dependencies

**Direct References:**
- `Microsoft.Extensions.Configuration` / `Configuration.Json` / `Configuration.Binder` / `Configuration.FileExtensions`
- `Microsoft.Extensions.DependencyInjection`
- `NuciLog`, `NuciLog.Core`
- `NuciWeb.Automation`, `NuciWeb.Automation.Selenium`
- `OpenQA.Selenium` (for `IWebDriver`)
- Internal: `Configuration`, `Service`, `Processors`, `Logging`

**No References To:**
- `HoneygainProcessor` concrete type (uses `IHoneygainProcessor`)
- `SeleniumWebProcessor` concrete type (uses `IWebProcessor`)
- `NuciLogger` concrete type (uses `ILogger`)

### Testing

No unit tests for `Program.cs` (static entry point). Integration testing would require:
- Real browser/WebDriver
- Real Honeygain credentials
- Full end-to-end execution

### Modification Guidance

| Change | Location | Considerations |
|--------|----------|----------------|
| Add new service | `Program.cs` registration | Choose correct lifetime; add to constructor of consumers |
| Change WebDriver initialisation | `Program.cs` before DI | Must happen before container; handle failures |
| Add configuration section | `appsettings.json` + new settings class | Bind in `Program.cs`; register as singleton |
| Change logger implementation | `Program.cs` `.AddSingleton<ILogger, NewLogger>()` | Must implement `NuciLog.Core.ILogger` |
| Add command-line args | `Main(string[] args)` | Parse before config; override config values |
| Set exit code on failure | `Main` return `int` | Requires tracking operation success/failure |

### Related Documentation

- [Architecture (Detailed)](./architecture.md#program-composition-root)
- [Repository Structure](./repository-structure.md#honeygainpotopener-main-application)
- [Configuration](./configuration.md)
- [Flows: Startup and Rendering](./flows/startup-and-rendering.md)