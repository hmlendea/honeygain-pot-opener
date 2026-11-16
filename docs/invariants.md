# Invariants

## System Invariants

These properties **always hold** for the current implementation. They are not configurable and not subject to change without a MAJOR version bump.

### Execution Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 1 | Process runs exactly once per invocation | `Program.Main` calls `Run()` once, no loops |
| 2 | Operations execute in fixed order: LogIn → ClaimReward → OpenPot | `BotService.Run()` sequence |
| 3 | Each operation logs exactly 2 events: Started, then Success or Failure | `BotService.ExecuteOperation` |
| 4 | ClaimReward failure logs at Warn level and is expected; LogIn/OpenPot at Error level and are recorded | `BotService` failure policy |
| 5 | WebDriver.Quit() called in finally block | `Program.Main` finally |
| 6 | No operation failure stops subsequent operations | Explicit `return` after failure handling |
| 7 | No retry logic for any operation | No loops, no Polly, no backoff |
| 8 | Exit code is 0 only when no unexpected operation failed | `Program.ResolveExitCode` |

### Configuration Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 9 | Configuration bound once at startup | `Program.Main` before DI build |
| 10 | No configuration reload during execution | No `IOptionsMonitor`, no change callbacks |
| 11 | Missing config section → properties remain default (`string.Empty`/`false`) | `ConfigurationBinder` behaviour and property initialisers |
| 12 | `DebugSettings.IsHeadless = !IsDebugMode` (computed, not stored) | `DebugSettings` class |
| 13 | Incomplete credentials abort before any browser is created | `BotSettings.HasValidCredentials` in `Program.Main` |

### Logging Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 14 | Every operation log includes `Username` LogInfo (email) | `BotService.ExecuteOperation` creates `logInfos` |
| 15 | Password never passed to logger | No code path includes password in `LogInfo` |
| 16 | StartUp logged before any operation | `Program.RunBot` → logger → `IBotService.Run()` |
| 17 | Fatal only logged in Program.Main guard | `BotService` never calls `logger.Fatal` |
| 18 | Log level minimum = Info (configurable but default) | `nuciLoggerSettings.minimumLevel` |
| 19 | Success is logged only for completed operations | `BotService.ExecuteOperation` returns after `LogFailure` |

### Security Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 20 | Credentials only in memory during execution | `BotSettings` singleton, process exit clears |
| 21 | No credential persistence | No file write, no database, no cache |
| 22 | Browser session ends on Quit() | `driver.Quit()` in finally |
| 23 | Single account per process | Single `BotSettings` instance |

### Integration Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 24 | All Honeygain URLs declared on HoneygainProcessor | Static URL properties |
| 25 | All selectors declared on HoneygainProcessor | Private static selector properties |
| 26 | IWebProcessor abstraction used (not Selenium directly) | `HoneygainProcessor` constructor |
| 27 | SeleniumWebProcessor created with WebDriverInitialiser | `Program.cs` registration |

### Testing Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 28 | Unit and integration tests mock IWebProcessor and ILogger | `HoneygainProcessorTests`, `BotServiceTests` |
| 29 | Tests verify call sequence and selector values | `Verify` with exact `By` selectors |
| 30 | Tests use Given/When/Then naming | All test methods |
| 31 | Production sources are embedded into the architecture tests | Architecture-test project configuration |

### Concurrency Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 32 | Strictly single-threaded execution | No async, no Task, no threads |
| 33 | Single WebDriver instance per process | Singleton `IWebDriver` registration |
| 34 | No shared mutable state | All settings immutable after init |

### Deployment Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 35 | Self-contained publish includes .NET runtime | `dotnet publish --self-contained` |
| 36 | Chrome/Chromium required on target machine | `WebDriverInitialiser` dependency |
| 37 | No CLI arguments accepted | `Main()` declares no parameters |

## Invariant Violations = Breaking Changes

Any change that violates an invariant requires:
- MAJOR version bump
- Entry in `change-guide.md`
- Migration documentation
- Updated tests

## Verification

### Automated (Tests)

```bash
# Run all tests - verifies behavioural invariants
dotnet test HoneygainPotOpener.slnx -c Release
```

### Manual (Code Review)

Check for:
- New loops/retries in `BotService`
- Async/await introduction
- Config reload mechanisms
- Password in logging calls
- Multiple `BotSettings` instances
- Direct Selenium usage in `HoneygainProcessor`
- Success logged for an operation that already logged `Failure`

## Related Documentation

- [Architecture](../architecture.md)
- [Error Handling](./error-handling.md)
- [Logging](./logging.md)
- [Security](./security.md)
- [Concurrency and Scheduling](./concurrency-and-scheduling.md)
- [Change Guide](./change-guide.md)