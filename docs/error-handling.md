# Error Handling

## Error Taxonomy

### 1. Infrastructure Errors (Propagated from IWebProcessor)

| Exception | Source | Typical Cause |
|-----------|--------|---------------|
| `NoSuchElementException` | Selenium | Selector not found in DOM |
| `ElementNotInteractableException` | Selenium | Element hidden, disabled, or not ready |
| `ElementClickInterceptedException` | Selenium | Another element obscures target |
| `StaleElementReferenceException` | Selenium | DOM mutated after element located |
| `WebDriverTimeoutException` | Selenium | `WaitForElementToBeVisible` timeout |
| `WebDriverException` | Selenium | Base class for driver errors |
| `TimeoutException` | .NET | General operation timeout |

### 2. Business Logic Errors (Thrown by HoneygainProcessor)

| Exception | Method | Condition |
|-----------|--------|-----------|
| `InvalidOperationException` | `ClaimReward()` | "Claim reward" button not visible |
| `InvalidOperationException` | `OpenPot()` | "Open Lucky Pot" button not visible after wait |

### 3. Configuration Errors

| Error | Detection | Result |
|-------|-----------|--------|
| Missing `appsettings.json` | `AddJsonFile(optional: true)` | Accepted; settings remain default |
| Invalid JSON | `ConfigurationBuilder.Build()` | Exception at startup (unguarded) |
| Missing required settings | Point of use (e.g., `SetText(null)`) | `ArgumentNullException` or Selenium exception |
| Type mismatch in JSON | `config.Bind()` | Exception at bind time (unguarded) |

### 4. Unhandled Exceptions (Program.Main Guard)

| Exception | Context | Handling |
|-----------|---------|----------|
| `AggregateException` | `IBotService.Run()` | Log each inner as Fatal |
| `Exception` | `IBotService.Run()` | Log as Fatal |
| Any other | Config load, WebDriver init, DI build | **Unguarded** — process crashes |

## Handling Patterns

### Pattern 1: Processor Propagates, Orchestrator Catches

**HoneygainProcessor:** No try/catch — all exceptions propagate

**BotService:** Catches all exceptions per operation

```csharp
void LogIn()
{
    var logInfos = [new(MyLogInfoKey.Username, settings.EmailAddress)];
    logger.Info(MyOperation.LogIn, OperationStatus.Started, logInfos);
    try
    {
        processor.LogIn();
    }
    catch (Exception ex)
    {
        logger.Error(MyOperation.LogIn, OperationStatus.Failure, ex, logInfos);
    }
    logger.Info(MyOperation.LogIn, OperationStatus.Success, logInfos);
}
```

### Pattern 2: Severity by Operation

| Operation | Failure Severity | Rationale |
|-----------|------------------|-----------|
| `LogIn` | `Error` | Authentication failure is critical |
| `ClaimReward` | `Warn` | Reward may not be available (expected) |
| `OpenPot` | `Error` | Pot opening is primary objective |

### Pattern 3: Unconditional Success Logging

```csharp
try { processor.Method(); }
catch { logger.Error/Warn(Failure); }
logger.Info(Success);  // ALWAYS executed
```

**Consequence:** Success log ≠ operation succeeded

### Pattern 4: Best-Effort Continuation

```
LogIn fails → logged → ClaimReward runs → logged → OpenPot runs → logged
```

No operation failure stops subsequent operations.

### Pattern 5: Fatal Guard in Main

```csharp
try
{
    serviceProvider.GetService<IBotService>().Run();
}
catch (AggregateException ex)
{
    foreach (var inner in ex.InnerExceptions)
        logger.Fatal(Operation.Unknown, OperationStatus.Failure, inner);
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

## Recovery Strategies

### Current: No Recovery

| Failure Type | Recovery Action |
|--------------|-----------------|
| Infrastructure (Selenium) | None — logged, continue to next operation |
| Business logic (no reward/pot) | None — logged as Warn/Error, continue |
| Configuration | None — fails at point of use |
| Unhandled (Main guard) | Log Fatal, quit driver, exit |

### No Retry Logic

- No exponential backoff
- No circuit breaker
- No retry count configuration
- External scheduler responsible for re-invocation

### No Compensating Actions

- No logout on failure
- No session cleanup beyond `driver.Quit()`
- No state rollback (no persistent state)

## Error Flow Diagrams

### Login Failure

```
HoneygainProcessor.LogIn()
    │
    ├─► GoToUrl → timeout → WebDriverTimeoutException
    ├─► WaitForElement (cookie) → timeout → WebDriverTimeoutException
    ├─► SetText (email) → NoSuchElementException
    ├─► SetText (password) → NoSuchElementException
    ├─► Click (login) → ElementClickInterceptedException
    └─► WaitForElement (dashboard) → timeout → WebDriverTimeoutException
            │
            ▼
BotService.LogIn() catch (Exception ex)
    │
    ├─► logger.Error(LogIn, Failure, ex, Username)
    │
    ▼
logger.Info(LogIn, Success, Username)  ← LOGGED ANYWAY
    │
    ▼
BotService.Run() continues to ClaimReward()
```

### Reward Claim Failure (No Reward)

```
HoneygainProcessor.ClaimReward()
    │
    ├─► IsElementVisible → false
    │
    ▼
throw InvalidOperationException("There is no reward available...")
            │
            ▼
BotService.ClaimReward() catch (Exception ex)
    │
    ├─► logger.Warn(ClaimReward, Failure, ex, Username)
    │
    ▼
logger.Info(ClaimReward, Success, Username)  ← LOGGED ANYWAY
    │
    ▼
BotService.Run() continues to OpenPot()
```

### Pot Opening Failure (Not Available)

```
HoneygainProcessor.OpenPot()
    │
    ├─► WaitForElementToBeVisible → timeout → WebDriverTimeoutException
    │   OR
    ├─► IsElementVisible → false
    │
    ▼
throw InvalidOperationException("The Lucky Pot cannot be opened...")
            │
            ▼
BotService.OpenPot() catch (Exception ex)
    │
    ├─► logger.Error(OpenPot, Failure, ex, Username)
    │
    ▼
logger.Info(OpenPot, Success, Username)  ← LOGGED ANYWAY
    │
    ▼
BotService.Run() completes
```

### Unguarded Startup Failure

```
Program.Main()
    │
    ├─► LoadConfiguration() → invalid JSON → JsonException → CRASH (no logging)
    ├─► WebDriverInitialiser → no browser → Exception → CRASH (no logging, no driver)
    ├─► ServiceCollection.BuildServiceProvider() → missing registration → CRASH
    └─► logger = GetService<ILogger>() → NuciLog config error → CRASH
```

## Exit Codes

| Scenario | Exit Code | Notes |
|----------|-----------|-------|
| All operations attempted (any success/failure) | 0 | `Main` returns void |
| Unhandled exception in Main guard | 0 | Caught, logged Fatal, driver quit |
| Unhandled exception before Main guard | Non-zero | .NET runtime default (typically 1) |
| Unhandled exception in finally | Non-zero | If `driver.Quit()` throws |

## Testing Error Handling

### Unit Tests (BotServiceTests)

- `GivenRun_WhenLogInThrows_ThenLogsErrorAndContinues`
- `GivenRun_WhenClaimRewardThrows_ThenLogsWarningAndContinues`
- `GivenRun_WhenOpenPotThrows_ThenLogsErrorAndContinues`

**Verification:**
- Exception caught and logged at correct severity
- Success logged after failure
- Subsequent operations still invoked

### Unit Tests (HoneygainProcessorTests)

- `GivenClaimReward_WhenButtonNotVisible_ThenThrowsInvalidOperationException`
- `GivenOpenPot_WhenButtonNotVisible_ThenThrowsInvalidOperationException`

**Verification:**
- Business logic exceptions thrown at correct conditions

## Modification Guidance

| Change | Impact | Files |
|--------|--------|-------|
| Add retry logic | Changes single-pass design; requires async or blocking | `BotService`, `IHoneygainProcessor` |
| Change log severity | Affects log analysis/alerting | `BotService` catch blocks |
| Make success conditional | Success log = actual success | `BotService` (move Success log inside try) |
| Add custom exceptions | Better error discrimination | New exception classes, `HoneygainProcessor`, `BotService` |
| Add recovery actions | Compensating logic on failure | `BotService` catch blocks |
| Guard startup phases | Prevent unguarded crashes | `Program.Main` (expand try scope) |
| Set exit code on failure | External scheduler integration | `Program.Main` return int |

## Invariants

1. **All processor exceptions caught in BotService** — no unhandled exceptions from operations
2. **Three log events per operation** — Started, Failure/Success (in catch), Success (always)
3. **ClaimReward failure = Warn** — others = Error
4. **Continuation always** — Run() completes all three operations
5. **Driver quit in finally** — guaranteed for guarded path
6. **No retry** — single attempt per operation per process
7. **Unguarded startup** — config, WebDriver, DI, logger resolution can crash silently

## Related Documentation

- [Components: Application Services](./components/application-services.md)
- [Components: Integration Models](./components/integration-models.md)
- [Flows: Authentication](./flows/authentication.md)
- [Flows: Reward Claim](./flows/reward-claim.md)
- [Flows: Lucky Pot](./flows/lucky-pot.md)
- [Logging](./logging.md)
- [Testing](./testing.md)