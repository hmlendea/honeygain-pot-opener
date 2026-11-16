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
| Missing `appsettings.json` | `AddJsonFile(optional: true)` | Accepted; settings remain default (empty strings) |
| Invalid JSON | `ConfigurationBuilder.Build()` | Exception at startup (unguarded) |
| Missing required settings | `BotSettings.HasValidCredentials` at startup | Diagnostic message on standard error and exit code 1; no driver is created |
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

**BotService:** Executes each operation through `ExecuteOperation`, which catches all exceptions

```csharp
private void ExecuteOperation(
    Operation operation,
    Action action,
    bool isFailureExpected,
    List<Operation> failedOperations)
{
    var logInfos = [new(MyLogInfoKey.Username, settings.EmailAddress)];
    logger.Info(operation, OperationStatus.Started, logInfos);
    try
    {
        action();
    }
    catch (Exception exception)
    {
        LogFailure(operation, exception, isFailureExpected, logInfos);
        if (!isFailureExpected)
        {
            failedOperations.Add(operation);
        }
        return;
    }
    logger.Info(operation, OperationStatus.Success, logInfos);
}
```

### Pattern 2: Severity and Expectation by Operation

| Operation | Failure Expected | Failure Severity | Rationale |
|-----------|------------------|------------------|-----------|
| `LogIn` | No | `Error` | Authentication failure is critical |
| `ClaimReward` | Yes | `Warn` | Reward may not be available (expected) |
| `OpenPot` | No | `Error` | Pot opening is primary objective |

### Pattern 3: Conditional Success Logging

```csharp
try { action(); }
catch { LogFailure(...); return; }
logger.Info(operation, OperationStatus.Success, logInfos);
```

**Consequence:** Success is logged only when the operation completed normally; a failure logs `Failure` and records the operation in `ExecutionReport.FailedOperations`.

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
| Configuration | None — missing credentials abort before the browser starts |
| Unhandled (Main guard) | Log Fatal, quit driver, exit code 1 |

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
BotService.ExecuteOperation(LogIn) catch (Exception ex)
    │
    ├─► logger.Error(LogIn, Failure, ex, Username)
    ├─► failedOperations.Add(LogIn)
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
BotService.ExecuteOperation(ClaimReward) catch (Exception ex)
    │
    ├─► logger.Warn(ClaimReward, Failure, ex, Username)
    ├─► failure is expected → not recorded in ExecutionReport
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
BotService.ExecuteOperation(OpenPot) catch (Exception ex)
    │
    ├─► logger.Error(OpenPot, Failure, ex, Username)
    ├─► failedOperations.Add(OpenPot)
    │
    ▼
BotService.Run() completes and returns a failed ExecutionReport
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
| No unexpected operation failures | 0 | `ExecutionReport.IsSuccessful` is `true` |
| Unexpected operation failure (`LogIn` or `OpenPot`) | 1 | Recorded in `ExecutionReport.FailedOperations` |
| Reward unavailable (expected) | 0 | `ClaimReward` failure is not treated as a run failure |
| Missing or incomplete credentials | 1 | Diagnostic message written before the browser starts |
| Unhandled exception in Main guard | 1 | Caught, logged Fatal, driver quit |
| Unhandled exception before Main guard | Non-zero | .NET runtime default (typically 1) |
| Unhandled exception in finally | Non-zero | If `driver.Quit()` throws |

## Testing Error Handling

### Unit Tests (BotServiceTests)

- `GivenRun_WhenLogInThrows_ThenLogsErrorAndContinues`
- `GivenRun_WhenClaimRewardThrows_ThenLogsWarningAndContinues`
- `GivenRun_WhenOpenPotThrows_ThenLogsErrorAndContinues`
- `GivenRun_WhenLogInAndOpenPotThrow_ThenReportsEveryFailure`

**Verification:**
- Exception caught and logged at correct severity
- Success is not logged for a failed operation
- Unexpected failures are recorded in `ExecutionReport.FailedOperations`
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
| Change log severity | Affects log analysis/alerting | `BotService.LogFailure` |
| Change failure expectation | Affects exit code and failure reporting | `BotService.Run` argument |
| Add custom exceptions | Better error discrimination | New exception classes, `HoneygainProcessor`, `BotService` |
| Add recovery actions | Compensating logic on failure | `BotService` catch block |
| Guard startup phases | Prevent unguarded crashes | `Program.Main` (expand try scope) |
| Change exit code policy | External scheduler integration | `Program.ResolveExitCode` |

## Invariants

1. **All processor exceptions caught in BotService** — no unhandled exceptions from operations
2. **Two log events per operation** — Started, then Success or Failure
3. **ClaimReward failure = Warn and expected** — others = Error and recorded
4. **Continuation always** — Run() completes all three operations
5. **Driver quit in finally** — guaranteed for the guarded path
6. **No retry** — single attempt per operation per process
7. **Partially guarded startup** — config, WebDriver, DI, and logger resolution remain outside the orchestration guard; missing credentials are checked explicitly

## Related Documentation

- [Components: Application Services](./components/application-services.md)
- [Components: Integration Models](./components/integration-models.md)
- [Flows: Authentication](./flows/authentication.md)
- [Flows: Reward Claim](./flows/reward-claim.md)
- [Flows: Lucky Pot](./flows/lucky-pot.md)
- [Logging](./logging.md)
- [Testing](./testing.md)