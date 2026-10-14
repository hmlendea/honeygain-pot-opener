# Invariants

## System Invariants

These properties **always hold** for the current implementation. They are not configurable and not subject to change without a MAJOR version bump.

### Execution Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 1 | Process runs exactly once per invocation | `Program.Main` calls `Run()` once, no loops |
| 2 | Operations execute in fixed order: LogIn → ClaimReward → OpenPot | `BotService.Run()` sequence |
| 3 | Each operation logs exactly 3 events: Started, Failure/Success (catch), Success (always) | `BotService` methods |
| 4 | ClaimReward failure logs at Warn level; LogIn/OpenPot at Error level | `BotService` catch blocks |
| 5 | WebDriver.Quit() called in finally block | `Program.Main` finally |
| 6 | No operation failure stops subsequent operations | No re-throw in catch blocks |
| 7 | No retry logic for any operation | No loops, no Polly, no backoff |

### Configuration Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 8 | Configuration bound once at startup | `Program.Main` before DI build |
| 9 | No configuration reload during execution | No `IOptionsMonitor`, no change callbacks |
| 10 | Missing config section → properties remain default (null/false) | `ConfigurationBinder` behaviour |
| 11 | `DebugSettings.IsHeadless = !IsDebugMode` (computed, not stored) | `DebugSettings` class |

### Logging Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 12 | Every operation log includes `Username` LogInfo (email) | `BotService` methods create `logInfos` |
| 13 | Password never passed to logger | No code path includes password in `LogInfo` |
| 14 | StartUp logged before any operation | `Program.Main` → logger → `BotService.Run()` |
| 15 | Fatal only logged in Program.Main guard | `BotService` never calls `logger.Fatal` |
| 16 | Log level minimum = Info (configurable but default) | `nuciLoggerSettings.minimumLevel` |

### Security Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 17 | Credentials only in memory during execution | `BotSettings` singleton, process exit clears |
| 18 | No credential persistence | No file write, no database, no cache |
| 19 | Browser session ends on Quit() | `driver.Quit()` in finally |
| 20 | Single account per process | Single `BotSettings` instance |

### Integration Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 21 | All Honeygain URLs hardcoded in HoneygainProcessor | Constants in class |
| 22 | All selectors hardcoded in HoneygainProcessor | Constants in class |
| 23 | IWebProcessor abstraction used (not Selenium directly) | `HoneygainProcessor` constructor |
| 24 | SeleniumWebProcessor created with WebDriverInitialiser | `Program.cs` registration |

### Testing Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 25 | Unit tests mock IWebProcessor and ILogger | `HoneygainProcessorTests`, `BotServiceTests` |
| 26 | Tests verify call sequence and selector values | `Verify` with exact `By` selectors |
| 27 | Tests use Given/When/Then naming | All test methods |
| 28 | No integration/E2E tests in repository | Only `HoneygainPotOpener.UnitTests` project |

### Concurrency Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 29 | Strictly single-threaded execution | No async, no Task, no threads |
| 30 | Single WebDriver instance per process | Singleton `IWebProcessor` registration |
| 31 | No shared mutable state | All settings immutable after init |

### Deployment Invariants

| # | Invariant | Evidence |
|---|-----------|----------|
| 32 | Self-contained publish includes .NET runtime | `dotnet publish --self-contained` |
| 33 | Chrome/Chromium required on target machine | `WebDriverInitialiser` dependency |
| 34 | No CLI arguments accepted | `Main(string[] args)` unused |

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

## Related Documentation

- [Architecture](../architecture.md)
- [Error Handling](./error-handling.md)
- [Logging](./logging.md)
- [Security](./security.md)
- [Concurrency and Scheduling](./concurrency-and-scheduling.md)
- [Change Guide](./change-guide.md)