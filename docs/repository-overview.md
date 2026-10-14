# Repository Overview

## Purpose

Honeygain Pot Opener is a **single-pass console automation tool** for the Honeygain dashboard. It performs one complete automation cycle per process invocation:

1. Authenticates with Honeygain using configured credentials
2. Claims an available achievement reward (if any)
3. Attempts to open the daily Lucky Pot
4. Emits structured logs for each operation
5. Terminates the browser session

The application **does not** include:
- A scheduler or daemon mode
- A database or persistent storage (beyond config and logs)
- A network listener or API endpoint
- Retry logic for failed operations
- Multi-account support

## Scope

| In Scope | Out of Scope |
|----------|--------------|
| Single Honeygain account automation per run | Multi-account orchestration |
| Browser-based dashboard interaction via Selenium | API-based integration (Honeygain has no public API) |
| Structured operation logging via NuciLog | Metrics, tracing, health endpoints |
| Configuration via `appsettings.json` | Environment variables, secret vaults, command-line args |
| Linux, macOS, Windows execution | Mobile, browser extension, or cloud-hosted deployment |

## Entry Points

### Process Entry Point
- **File:** `HoneygainPotOpener/Program.cs`
- **Method:** `static void Main(string[] args)`
- **Arguments:** None defined (args ignored)
- **Exit Codes:** 0 (always, unless unhandled exception)

### Service Entry Point
- **Interface:** `HoneygainPotOpener.Service.IBotService`
- **Implementation:** `HoneygainPotOpener.Service.BotService`
- **Method:** `void Run()`
- **Invocation:** Called once from `Program.Main` after DI resolution

### Integration Entry Point
- **Interface:** `HoneygainPotOpener.Processors.IHoneygainProcessor`
- **Implementation:** `HoneygainPotOpener.Processors.HoneygainProcessor`
- **Methods:**
  - `void LogIn()`
  - `void ClaimReward()`
  - `void OpenPot()`
- **Invocation:** Called sequentially by `BotService.Run()`

## Principal Capabilities

| Capability | Implementation | Configuration |
|------------|----------------|---------------|
| Honeygain authentication | `HoneygainProcessor.LogIn()` | `botSettings.emailAddress`, `botSettings.password` |
| Cookie consent handling | `HoneygainProcessor.AcceptCookiesIfNeeded()` | Automatic (detects and clicks) |
| Achievement reward claim | `HoneygainProcessor.ClaimReward()` | Automatic (skips if not available) |
| Lucky Pot opening | `HoneygainProcessor.OpenPot()` | Automatic (fails if not available) |
| Operation logging | `BotService` + `NuciLogger` | `nuciLoggerSettings` in `appsettings.json` |
| Headless/visible browser | `WebDriverInitialiser` | `debugSettings.isDebugMode` |

## System Context

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Operator /     │────►│  Honeygain Pot   │────►│  Local Browser  │
│  Scheduler      │     │  Opener Process  │     │  + WebDriver    │
└─────────────────┘     └────────┬─────────┘     └────────┬────────┘
                                 │                        │
                                 ▼                        ▼
                        ┌──────────────────┐     ┌─────────────────┐
                        │  appsettings.json│     │  Honeygain      │
                        │  (credentials,   │     │  Dashboard      │
                        │   logging config)│     │  (HTTPS)        │
                        └──────────────────┘     └─────────────────┘
                                 │
                                 ▼
                        ┌──────────────────┐
                        │  Log Output      │
                        │  (./logfile.log) │
                        └──────────────────┘
```

## Version & Compatibility

- **Target Framework:** .NET 10.0 (`net10.0`)
- **Runtime:** .NET 10.0 required
- **Browser:** Any Selenium-compatible browser (Chrome, Firefox, Edge)
- **WebDriver:** Auto-discovered via `NuciWeb.Automation.Selenium.WebDriverInitialiser`
- **Release Archives:** Linux (arm, arm64, x64), macOS (arm64, x64), Windows (arm64, x64)

## Related Documentation

- [Architecture (Detailed)](./architecture.md)
- [Repository Structure](./repository-structure.md)
- [Quick Start](./quick-start.md)
- [Configuration](./configuration.md)
- [Flows: Startup and Rendering](./flows/startup-and-rendering.md)