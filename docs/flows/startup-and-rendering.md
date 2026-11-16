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
Program.Main()
    │
    ▼
LoadConfiguration()
    │
    ├─► ConfigurationBuilder()
    ├─► AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    ├─► Build()
    │
    ▼
new BotSettings()
new DebugSettings()
config.Bind("BotSettings", botSettings)
config.Bind("DebugSettings", debugSettings)
    │
    ▼
botSettings.HasValidCredentials ?
    │
    ├─► false ──► Console.Error message → ExitCode 1 → return
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
serviceProvider.GetRequiredService<IBotService>().Run()
    │
    ├─► BotService.Run()
    │   ├─► LogIn()
    │   ├─► ClaimReward()
    │   └─► OpenPot()
    │   └─► ExecutionReport { IsSuccessful, FailedOperations }
    │
    ▼
Environment.ExitCode = ResolveExitCode(report)
    │
    ├─► 0 when the report is successful
    └─► 1 when an unexpected operation failed
    │
    ▼
catch (AggregateException ex)
    │
    ├─► foreach innerException
    │   └─► logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException)
    └─► Environment.ExitCode = 1
    │
    ▼
catch (Exception ex)
    │
    ├─► logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex)
    └─► Environment.ExitCode = 1
    │
    ▼
finally
    │
    └─► webDriver.Quit()
    │
    ▼
Process Exit (0 or 1)
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
BotSettings botSettings = new();      // { EmailAddress, Password }, both default to string.Empty
DebugSettings debugSettings = new();  // { IsDebugMode, IsHeadless }
config.Bind(nameof(BotSettings), botSettings);
config.Bind(nameof(DebugSettings), debugSettings);
```

**Startup Validation:** When `botSettings.HasValidCredentials` is `false`, the process writes a diagnostic message to standard error, sets exit code 1, and returns before any browser is created.

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
ILogger logger = serviceProvider.GetRequiredService<ILogger>();
logger.Info(Operation.StartUp, "Application started");
```

- **Operation:** `NuciLog.Core.Operation.StartUp` (built-in)
- **Status:** Implicit `OperationStatus.Success` (Info level)
- **Context:** No additional `LogInfo` — just the message

### 5. Orchestration Invocation

```csharp
try
{
    ExecutionReport report = serviceProvider.GetRequiredService<IBotService>().Run();
    Environment.ExitCode = ResolveExitCode(report);
}
catch (AggregateException exception)
{
    LogFatalExceptions(logger, exception);
    Environment.ExitCode = FailureExitCode;
}
catch (Exception exception)
{
    logger.Fatal(Operation.Unknown, OperationStatus.Failure, exception);
    Environment.ExitCode = FailureExitCode;
}
finally
{
    webDriver.Quit();
}
```

- **Guard Scope:** `IBotService.Run()`, exit-code assignment, and the `finally` cleanup
- **Excluded from Guard:** Configuration loading, credential validation, WebDriver initialisation, DI construction, logger resolution
- **Fatal Logging:** Uses `Operation.Unknown` (built-in) with `OperationStatus.Failure`
- **Driver Cleanup:** Always executed in `finally`
- **Exit Code:** Derived from `ExecutionReport.IsSuccessful`; `1` on any guarded exception

## Failure Scenarios

| Phase | Failure | Result |
|-------|---------|--------|
| Config load | Invalid JSON | Exception → process crash (no logging) |
| Config load | Missing file | Accepted (optional) → settings use defaults (empty strings/false) |
| Credential validation | Missing or blank email/password | Diagnostic message → exit code 1 → no browser created |
| WebDriver init | No browser/driver | Exception → process crash (no DI, no logging) |
| DI build | Missing registration | Exception → process crash |
| Logger resolve | NuciLog misconfig | Exception → process crash |
| Run() | Unexpected operation failure | Caught → recorded → driver quit → exit code 1 |
| Run() | Expected reward absence only | `ClaimReward` not recorded → driver quit → exit code 0 |
| Run() | Thrown exception | Caught → logged Fatal → driver quit → exit code 1 |
| Driver quit | Exception in finally | Propagates after cleanup attempts |

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

1. **Credentials validated before WebDriver** — no browser is created for incomplete credentials
2. **Settings bound once** — no runtime reload
3. **Single WebDriver instance** — created once, quit once
4. **Startup logged before Run()** — `Operation.StartUp` always first log entry
5. **Driver quit in finally** — guaranteed cleanup for the guarded path
6. **Exit code set** — `0` on success, `1` on unexpected operation failure, guarded exception, or invalid credentials

## Related Documentation

- [Components: Host and Composition](./components/host-and-composition.md)
- [Configuration](./configuration.md)
- [Logging](./logging.md)
- [Error Handling](./error-handling.md)
- [Architecture (Detailed)](./architecture.md#runtime-sequence)