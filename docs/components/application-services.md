# Application Services

## Overview

The application services layer contains the orchestration logic (`BotService`) that defines the fixed operation sequence, emits structured log events, and applies best-effort continuation after failures.

## BotService

### Location
`HoneygainPotOpener/Service/BotService.cs`

### Interface
```csharp
public interface IBotService
{
    ExecutionReport Run();
}
```

### Implementation

```csharp
public sealed class BotService(
    IHoneygainProcessor processor,
    BotSettings settings,
    ILogger logger) : IBotService
{
    public ExecutionReport Run()
    {
        List<Operation> failedOperations = [];

        ExecuteOperation(MyOperation.LogIn, processor.LogIn, false, failedOperations);
        ExecuteOperation(MyOperation.ClaimReward, processor.ClaimReward, true, failedOperations);
        ExecuteOperation(MyOperation.OpenPot, processor.OpenPot, false, failedOperations);

        return new ExecutionReport { FailedOperations = failedOperations };
    }

    private void ExecuteOperation(
        Operation operation,
        Action action,
        bool isFailureExpected,
        List<Operation> failedOperations) { /* ... */ }
}
```

### Operation Sequence

`Run()` invokes three operations **in fixed order**:
1. `LogIn()` — Authenticate with Honeygain
2. `ClaimReward()` — Claim achievement reward if available
3. `OpenPot()` — Attempt to open Lucky Pot

Each operation is independent — failure in one does not stop subsequent operations.

### Operation Execution Pattern

All three operations share the single `ExecuteOperation` helper:

```csharp
private void ExecuteOperation(
    Operation operation,
    Action action,
    bool isFailureExpected,
    List<Operation> failedOperations)
{
    // 1. Create log context with email
    IEnumerable<LogInfo> logInfos = [
        new(MyLogInfoKey.Username, settings.EmailAddress)
    ];

    // 2. Log operation started
    logger.Info(operation, OperationStatus.Started, logInfos);

    try
    {
        // 3. Invoke processor
        action();
    }
    catch (Exception exception)
    {
        // 4. Log failure (severity depends on whether failure is expected)
        LogFailure(operation, exception, isFailureExpected, logInfos);

        if (!isFailureExpected)
        {
            failedOperations.Add(operation);
        }

        return;
    }

    // 5. Log success only when the operation completed
    logger.Info(operation, OperationStatus.Success, logInfos);
}
```

### Severity by Operation

| Operation | Failure Severity | Rationale |
|-----------|------------------|-----------|
| `LogIn` | `Error` | Authentication failure is critical |
| `ClaimReward` | `Warn` | Reward may not be available; not an error condition |
| `OpenPot` | `Error` | Pot opening is a primary objective |

### Log Events Emitted

Each operation emits a `Started` event and then exactly one terminal event: `Success` when the processor call returns normally, or `Failure` when it throws.

| Sequence | Level | Operation | Status | Context |
|----------|-------|-----------|--------|---------|
| 1 | Info | LogIn | Started | Username |
| 2 | Error/Info | LogIn | Failure/Success | Username + Exception (if failed) |
| 3 | Info | ClaimReward | Started | Username |
| 4 | Warn/Info | ClaimReward | Failure/Success | Username + Exception (if failed) |
| 5 | Info | OpenPot | Started | Username |
| 6 | Error/Info | OpenPot | Failure/Success | Username + Exception (if failed) |

**Total: 6 log events per successful run (2 per operation)**

### Success Evidence

The `logger.Info(..., OperationStatus.Success, ...)` call executes only after the processor call returns without an exception, so a success event is evidence that the operation completed. Failures are logged, recorded in `ExecutionReport.FailedOperations`, and not reported as success.

### Continuation Semantics

```
LogIn throws
    │
    ├─► logger.Error(LogIn, Failure, ex)
    ├─► failedOperations.Add(LogIn)
    ▼
ClaimReward executes
    │
    ├─► logger.Info(ClaimReward, Started)
    ├─► processor.ClaimReward()
    ├─► logger.Info(ClaimReward, Success)
    ▼
OpenPot executes
    │
    ├─► logger.Info(OpenPot, Started)
    ├─► processor.OpenPot()
    ├─► logger.Info(OpenPot, Success)
```

**No operation failure stops the sequence.**

### Dependencies

| Dependency | Type | Purpose |
|------------|------|---------|
| `IHoneygainProcessor` | Injected (singleton) | Executes Honeygain operations |
| `BotSettings` | Injected (singleton) | Provides `EmailAddress` for log context |
| `ILogger` | Injected (singleton) | Emits structured log events |

### Testing

**Test File:** `HoneygainPotOpener.UnitTests/Service/BotServiceTests.cs`

**Test Coverage:**
- `GivenRun_WhenCalled_ThenCallsLogInClaimRewardAndOpenPotInOrder` — verifies sequence
- `GivenRun_WhenAllSucceed_ThenLogsAllOperations` — verifies 6 log calls (3 Started + 3 Success)
- `GivenRun_WhenAllSucceed_ThenReportsSuccess` — verifies the report is successful and carries no failures
- `GivenRun_WhenLogInThrows_ThenLogsErrorAndContinues` — verifies error, no success, continuation, and failure recording
- `GivenRun_WhenClaimRewardThrows_ThenLogsWarningAndContinues` — verifies warn and continuation without failure recording
- `GivenRun_WhenOpenPotThrows_ThenLogsErrorAndContinues` — verifies error, no success, continuation, and failure recording
- `GivenRun_WhenLogInAndOpenPotThrow_ThenReportsEveryFailure` — verifies all unexpected failures are recorded in order

**Mock Verification:**
- Uses `Moq` to verify `processor` method calls
- Captures logger calls via `Callback` to inspect `Operation`, `OperationStatus`, `Exception`
- Filters by operation name to distinguish between operations

### Modification Guidance

| Change | Impact | Considerations |
|--------|--------|----------------|
| Add new operation | Add method to `IHoneygainProcessor`, `HoneygainProcessor`, `BotService.Run()` | Follow the same logging pattern; choose whether the failure is expected |
| Change operation order | Modify `Run()` method | May break assumptions (e.g., login must precede others) |
| Change log severity | Modify `LogFailure` | `ClaimReward` uses `Warn`; others use `Error` |
| Change failure policy | Modify the `isFailureExpected` argument | Expected failures are logged but excluded from `ExecutionReport` |
| Add retry logic | Wrap processor call in retry loop | Would require async or blocking wait; changes single-pass design |

### Invariants

1. **Fixed Order:** `LogIn` → `ClaimReward` → `OpenPot` always
2. **All Operations Attempted:** No early return on failure
3. **Two Log Events Per Operation:** Started, then Success or Failure
4. **Email in Every Log:** `MyLogInfoKey.Username` = `settings.EmailAddress`
5. **No State Mutation:** `BotService` has no mutable state; all state in dependencies
6. **Unexpected Failures Recorded:** non-expected operation failures appear in `ExecutionReport.FailedOperations`

### Related Documentation

- [Architecture (Detailed)](./architecture.md#botservice-orchestration)
- [Components: Integration Models](./components/integration-models.md)
- [Flows: Authentication](./flows/authentication.md)
- [Flows: Reward Claim](./flows/reward-claim.md)
- [Flows: Lucky Pot](./flows/lucky-pot.md)
- [Error Handling](./error-handling.md)
- [Logging](./logging.md)
- [Testing](./testing.md)