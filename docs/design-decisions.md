# Design Decisions

## 1. Single-Pass Console Application

**Decision:** The application performs one automation cycle per process invocation and exits.

**Rationale:**
- Simplifies error recovery — a crash or hang in one cycle does not affect subsequent cycles
- Enables external scheduling (cron, systemd, task scheduler) with full control over frequency and retry
- Avoids long-running state management complexity (session expiry, token refresh, memory leaks)
- Matches the Honeygain Lucky Pot's daily cadence — one run per day is sufficient

**Alternatives Considered:**
- Daemon mode with internal scheduler — rejected due to complexity and resource management concerns
- Background service with periodic execution — rejected as it would require persistent state and scheduling logic

**Evidence:** `Program.Main` calls `botService.Run()` once and exits; `README.md` states "The application exits after the reward and Lucky Pot operations have been attempted."

## 2. Browser Automation via Selenium (Not API)

**Decision:** Use Selenium WebDriver to automate the Honeygain web dashboard rather than calling a REST API.

**Rationale:**
- Honeygain does not expose a public REST API for Lucky Pot or achievement rewards
- Browser automation is the only available integration path
- Selenium provides cross-browser compatibility and WebDriver standard compliance

**Consequences:**
- Selectors are fragile — Honeygain UI changes can break automation without compile-time signals
- Requires a local browser and WebDriver installation
- Slower than API calls due to page load and rendering overhead

**Evidence:** `HoneygainProcessor` uses XPath and ID selectors; `IWebProcessor` wraps Selenium operations.

## 3. Dependency Injection with Explicit Interface Contracts

**Decision:** Use `Microsoft.Extensions.DependencyInjection` with explicit interfaces (`IBotService`, `IHoneygainProcessor`, `IWebProcessor`) separating orchestration from integration.

**Rationale:**
- Enables unit testing with mocks (Moq) — `BotServiceTests` and `HoneygainProcessorTests` mock dependencies
- Isolates Honeygain-specific logic from orchestration logic
- Allows future substitution of browser automation implementation (e.g., Playwright) without changing orchestration

**Evidence:**
- `BotService` depends on `IHoneygainProcessor` (not `HoneygainProcessor`)
- `HoneygainProcessor` depends on `IWebProcessor` (not `SeleniumWebProcessor`)
- Tests mock `IHoneygainProcessor` and `IWebProcessor`

## 4. Best-Effort Continuation with Unconditional Success Logging

**Decision:** Each operation (`LogIn`, `ClaimReward`, `OpenPot`) is attempted regardless of prior failures. Success is logged unconditionally after the `catch` block.

**Rationale:**
- Maximises the chance of completing all operations in a single run
- A failed login should not prevent reward claim or pot opening attempts (though they will likely fail too)
- Simple, predictable control flow

**Consequences:**
- **Log reliability issue:** A "Success" log entry does not guarantee the operation completed successfully — it is logged even after an exception was caught and logged
- Tests explicitly verify this behaviour: `GivenRun_WhenLogInThrows_ThenLogsErrorAndContinues` confirms success is logged after error

**Evidence:** `BotService.LogIn()`, `BotService.ClaimReward()`, `BotService.OpenPot()` all follow the pattern:
```
logger.Info(Started)
try { processor.Method() }
catch { logger.Error/Warn(Failure, ex) }
logger.Info(Success)  // unconditional
```

## 5. Singleton Lifetime for Processor and Service

**Decision:** `HoneygainProcessor`, `BotService`, `IWebDriver`, `ILogger`, and settings are registered as singletons. `IWebProcessor` is transient.

**Rationale:**
- Single WebDriver instance per process — avoids multiple browser windows
- Single processor instance — maintains consistent state across operations
- Transient `IWebProcessor` — allows future per-operation isolation, though currently retained by singleton processor

**Consequences:**
- The transient `IWebProcessor` is resolved once and held by the singleton `HoneygainProcessor` — effectively singleton lifetime in practice
- No disposal of `IServiceProvider` — relies on process exit for cleanup

**Evidence:** `Program.cs` service registration:
```csharp
.AddSingleton<IHoneygainProcessor, HoneygainProcessor>()
.AddTransient<IWebProcessor, SeleniumWebProcessor>()
.AddSingleton<IBotService, BotService>()
```

## 6. Configuration Binding Once at Startup

**Decision:** `BotSettings` and `DebugSettings` are bound once from `appsettings.json` at startup and registered as singletons.

**Rationale:**
- Simplifies the application — no runtime configuration reload logic needed
- Credentials and settings are stable for a single automation pass
- Avoids complexity of `IOptionsMonitor<T>` and reload callbacks

**Consequences:**
- Changes to `appsettings.json` during execution are not picked up
- The JSON provider has `reloadOnChange: true` but the bound objects are not refreshed
- `DebugSettings.IsHeadless` is computed from `IsDebugMode` at bind time

**Evidence:** `Program.cs`:
```csharp
config.Bind(nameof(BotSettings), botSettings);
config.Bind(nameof(DebugSettings), debugSettings);
```

## 7. No Exit Code on Failure

**Decision:** `Program.Main` does not set an exit code. The process exits 0 even when operations fail (as long as exceptions are caught).

**Rationale:**
- Failures are logged and the process continues — exit code 0 indicates "process completed" not "all operations succeeded"
- External schedulers can inspect logs for actual operation outcomes

**Consequences:**
- External schedulers cannot use exit code alone to detect failures
- Must parse logs or check Honeygain dashboard for actual results

**Evidence:** `Program.Main` has no `Environment.Exit` or return value; `Main` returns `void`.

## 8. WebDriver Initialisation Before DI Container

**Decision:** `WebDriverInitialiser.InitialiseAvailableWebDriver()` is called before `ServiceCollection` construction.

**Rationale:**
- WebDriver creation may fail (no browser/driver found) — failing early before DI setup
- The driver instance is registered as a singleton in the container

**Consequences:**
- If WebDriver initialisation fails, the DI container is never built and no cleanup occurs
- The driver is created outside the DI lifecycle — manual `Quit()` in `finally` is required

**Evidence:** `Program.cs`:
```csharp
IWebDriver webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode);
// ... then ServiceCollection
```

## 9. No Retry Logic

**Decision:** No retry mechanism exists for failed operations or WebDriver interactions.

**Rationale:**
- Single-pass design — failures are logged and the process moves on
- Retry logic would add complexity and potential for extended hangs
- External schedulers can re-invoke the process

**Consequences:**
- Transient failures (network blips, page load delays) cause operation failure
- No exponential backoff or circuit breaker patterns

**Evidence:** `BotService` catches exceptions and continues to next operation; no retry loops in `HoneygainProcessor`.

## 10. Email Address in Logs, Password Never Logged

**Decision:** The Honeygain e-mail address is included as `Username` context in all operation logs. The password is never logged.

**Rationale:**
- E-mail address provides correlation for multi-account deployments
- Password is sensitive — never appears in logs, exceptions, or telemetry

**Evidence:**
- `BotService` creates `LogInfo(MyLogInfoKey.Username, settings.EmailAddress)` for every operation
- No logging call references `settings.Password`
- `PRIVACY.md` confirms: "No secrets, access tokens, or passwords are logged."

## Related Documentation

- [Architecture (Detailed)](./architecture.md)
- [Error Handling](./error-handling.md)
- [Logging](./logging.md)
- [Security](./security.md)
- [Root ARCHITECTURE.md](../ARCHITECTURE.md)