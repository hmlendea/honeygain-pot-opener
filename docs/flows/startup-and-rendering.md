# Startup and Rendering Flow

## Overview

This flow covers the process from OS process start through configuration loading, WebDriver initialisation, DI container construction, startup logging, and orchestration invocation.

## Sequence Diagram

```
Operator/Scheduler
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ Process Start: dotnet run --project HoneygainPotOpener.csproj │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
Program.Main(string[] args)
    │
    ├─► new BotSettings()
    ├─► new DebugSettings()
    │
    ▼
LoadConfiguration()
    │
    ├─► ConfigurationBuilder()
    ├─► AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    ├─► Build()
    │
    ▼
config.Bind("BotSettings", botSettings)
config.Bind("DebugSettings", debugSettings)
    │
    ▼
WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode)
    │
    ├─► Discovers available browser (Chrome/Firefox/Edge)
    ├─► Locates matching WebDriver
    ├─► Configures options (headless if !IsDebugMode)
    ├─► Creates IWebDriver instance
    │
    ▼
new ServiceCollection()
    │
    ├─► AddNuciLoggerSettings(config)
    ├─► AddSingleton(botSettings)
    ├─► AddSingleton(debugSettings)
    ├─► AddSingleton<ILogger, NuciLogger>()
    ├─► AddSingleton(webDriver)
    ├─► AddTransient<IWebProcessor, SeleniumWebProcessor>()
    ├─► AddSingleton<IHoneygainProcessor, HoneygainProcessor>()
    ├─► AddSingleton<IBotService, BotService>()
    │
    ▼
BuildServiceProvider() → IServiceProvider
    │
    ▼
serviceProvider.GetService<ILogger>() → logger
    │
    ▼
logger.Info(Operation.StartUp, "Application started")
    │
    ▼
try
    │
    ▼
serviceProvider.GetService<IBotService>().Run()
    │
    ├─► BotService.Run()
    │   ├─► LogIn()
    │   ├─► ClaimReward()
    │   └─► OpenPot()
    │
    ▼
catch (AggregateException ex)
    │
    ├─► foreach innerException
    │   └─► logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException)
    │
    ▼
catch (Exception ex)
    │
    └─► logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex)
    │
    ▼
finally
    │
    └─► webDriver.Quit()
    │
    ▼
Process Exit (code 0 unless unhandled exception)
```

## Detailed Steps

### 1. Configuration Loading

**File:** `Program.LoadConfiguration()`

```csharp
static IConfiguration LoadConfiguration() => new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", true, true)  // optional, reloadOnChange
    .Build();
```

- **File:** `appsettings.json` in working directory
- **Optional:** Missing file does not throw
- **Reload on change:** Enabled but **not effective** for already-bound objects
- **No other providers:** No environment variables, command-line args, or user secrets

**Bound Objects:**
```csharp
BotSettings botSettings = new();      // { EmailAddress, Password }
DebugSettings debugSettings = new();  // { IsDebugMode, IsHeadless }
config.Bind(nameof(BotSettings), botSettings);
config.Bind(nameof(DebugSettings), debugSettings);
```

### 2. WebDriver Initialisation

**File:** `Program.Main` → `WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode)`

**External:** `NuciWeb.Automation.Selenium.WebDriverInitialiser`

**Behaviour:**
- Auto-discovers installed browsers
- Matches with available WebDriver
- Configures browser options:
  - `IsDebugMode = true` → visible window, no headless
  - `IsDebugMode = false` → headless mode
- Returns `OpenQA.Selenium.IWebDriver`

**Failure Modes:**
- No compatible browser found → exception (process exits, no DI container built)
- WebDriver version mismatch → exception
- Permission denied → exception

**Timing:** Occurs **before** DI container construction — fails fast.

### 3. DI Container Construction

**Registrations (in order):**

| Registration | Lifetime | Notes |
|--------------|----------|-------|
| `AddNuciLoggerSettings(config)` | — | Configures NuciLog from `nuciLoggerSettings` section |
| `AddSingleton(botSettings)` | Singleton | Already-bound instance |
| `AddSingleton(debugSettings)` | Singleton | Already-bound instance |
| `AddSingleton<ILogger, NuciLogger>()` | Singleton | NuciLog implementation |
| `AddSingleton(webDriver)` | Singleton | Pre-created driver instance |
| `AddTransient<IWebProcessor, SeleniumWebProcessor>()` | Transient | Resolved once, held by singleton processor |
| `AddSingleton<IHoneygainProcessor, HoneygainProcessor>()` | Singleton | Receives `IWebProcessor` and `BotSettings` |
| `AddSingleton<IBotService, BotService>()` | Singleton | Receives `IHoneygainProcessor`, `BotSettings`, `ILogger` |

**Build:** `BuildServiceProvider()` creates the container.

### 4. Startup Logging

```csharp
logger = serviceProvider.GetService<ILogger>();
logger.Info(Operation.StartUp, $"Application started");
```

- **Operation:** `NuciLog.Core.Operation.StartUp` (built-in)
- **Status:** Implicit `OperationStatus.Success` (Info level)
- **Context:** No additional `LogInfo` — just the message

### 5. Orchestration Invocation

```csharp
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
    webDriver.Quit();
}
```

- **Guard Scope:** Only `IBotService.Run()` and service resolution
- **Excluded from Guard:** Configuration loading, WebDriver initialisation, DI construction, logger resolution
- **Fatal Logging:** Uses `Operation.Unknown` (built-in) with `OperationStatus.Failure`
- **Driver Cleanup:** Always executed in `finally`

## Failure Scenarios

| Phase | Failure | Result |
|-------|---------|--------|
| Config load | Invalid JSON | Exception → process crash (no logging) |
| Config load | Missing file | Accepted (optional) → settings use defaults (null/false) |
| Settings bind | Missing required values | Null/empty strings → failure at point of use |
| WebDriver init | No browser/driver | Exception → process crash (no DI, no logging) |
| DI build | Missing registration | Exception → process crash |
| Logger resolve | NuciLog misconfig | Exception → process crash |
| Run() | Any exception | Caught → logged Fatal → driver quit → exit 0 |
| Driver quit | Exception in finally | Suppressed (finally completes) |

## Configuration Requirements

**Minimum `appsettings.json`:**
```json
{
  "botSettings": {
    "emailAddress": "user@example.com",
    "password": "actual-password"
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

## Timing Characteristics

| Phase | Typical Duration | Variability |
|-------|------------------|-------------|
| Config load | < 10ms | Low |
| Settings bind | < 5ms | Low |
| WebDriver init | 1-5s | High (browser launch) |
| DI build | < 50ms | Low |
| Startup log | < 10ms | Low |
| Run() | 10-60s | High (network, page loads) |

## Invariants

1. **WebDriver created before DI** — fails fast on browser issues
2. **Settings bound once** — no runtime reload
3. **Single WebDriver instance** — created once, quit once
4. **Startup logged before Run()** — `Operation.StartUp` always first log entry
5. **Driver quit in finally** — guaranteed cleanup for guarded path
6. **No exit code set** — process exits 0 on caught exceptions

## Related Documentation

- [Components: Host and Composition](./components/host-and-composition.md)
- [Configuration](./configuration.md)
- [Logging](./logging.md)
- [Error Handling](./error-handling.md)
- [Architecture (Detailed)](./architecture.md#runtime-sequence)