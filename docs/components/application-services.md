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
    void Run();
}
```

### Implementation

```csharp
public class BotService(
    IHoneygainProcessor processor,
    BotSettings settings,
    ILogger logger) : IBotService
{
    public void Run()
    {
        LogIn();
        ClaimReward();
        OpenPot();
    }

    void LogIn() { /* ... */ }
    void ClaimReward() { /* ... */ }
    void OpenPot() { /* ... */ }
}
```

### Operation Sequence

`Run()` invokes three operations **in fixed order**:
1. `LogIn()` — Authenticate with Honeygain
2. `ClaimReward()` — Claim achievement reward if available
3. `OpenPot()` — Attempt to open Lucky Pot

Each operation is independent — failure in one does not stop subsequent operations.

### Operation Method Pattern

All three methods follow the identical pattern:

```csharp
void OperationName()
{
    // 1. Create log context with email
    IEnumerable<LogInfo> logInfos = [
        new(MyLogInfoKey.Username, settings.EmailAddress)
    ];

    // 2. Log operation started
    logger.Info(MyOperation.OperationName, OperationStatus.Started, logInfos);

    try
    {
        // 3. Invoke processor
        processor.OperationName();
    }
    catch (Exception ex)
    {
        // 4. Log failure (severity varies)
        logger.Error/Warn(MyOperation.OperationName, OperationStatus.Failure, ex, logInfos);
    }

    // 5. Log success UNCONDITIONALLY
    logger.Info(MyOperation.OperationName, OperationStatus.Success, logInfos);
}
```

### Severity by Operation

| Operation | Failure Severity | Rationale |
|-----------|------------------|-----------|
| `LogIn` | `Error` | Authentication failure is critical |
| `ClaimReward` | `Warn` | Reward may not be available; not an error condition |
| `OpenPot` | `Error` | Pot opening is a primary objective |

### Log Events Emitted

For each operation, **three log events** are emitted (assuming no unhandled exception):

| Sequence | Level | Operation | Status | Context |
|----------|-------|-----------|--------|---------|
| 1 | Info | LogIn | Started | Username |
| 2 | Error/Info | LogIn | Failure/Success | Username + Exception (if failed) |
| 3 | Info | LogIn | Success | Username |
| 4 | Info | ClaimReward | Started | Username |
| 5 | Warn/Info | ClaimReward | Failure/Success | Username + Exception (if failed) |
| 6 | Info | ClaimReward | Success | Username |
| 7 | Info | OpenPot | Started | Username |
| 8 | Error/Info | OpenPot | Failure/Success | Username + Exception (if failed) |
| 9 | Info | OpenPot | Success | Username |

**Total: 9 log events per successful run (3 per operation)**

### Critical Behaviour: Unconditional Success Logging

The `logger.Info(..., OperationStatus.Success, ...)` call is **outside the `try` block**, after the `catch`. This means:

- **Success is logged even when the operation threw an exception**
- Log-derived success is **not evidence** that the browser operation completed
- Tests verify this: `GivenRun_WhenLogInThrows_ThenLogsErrorAndContinues` expects both `Failure` and `Success` for the same operation

### Continuation Semantics

```
LogIn throws
    │
    ├─► logger.Error(LogIn, Failure, ex)
    ├─► logger.Info(LogIn, Success)  ← logged anyway
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
- `GivenRun_WhenLogInThrows_ThenLogsErrorAndContinues` — verifies error + success + continuation
- `GivenRun_WhenClaimRewardThrows_ThenLogsWarningAndContinues` — verifies warn + success + continuation
- `GivenRun_WhenOpenPotThrows_ThenLogsErrorAndContinues` — verifies error + success + continuation

**Mock Verification:**
- Uses `Moq` to verify `processor` method calls
- Captures logger calls via `Callback` to inspect `Operation`, `OperationStatus`, `Exception`
- Filters by operation name to distinguish between operations

### Modification Guidance

| Change | Impact | Considerations |
|--------|--------|----------------|
| Add new operation | Add method to `IHoneygainProcessor`, `HoneygainProcessor`, `BotService.Run()` | Follow same logging pattern; choose severity |
| Change operation order | Modify `Run()` method | May break assumptions (e.g., login must precede others) |
| Change log severity | Modify `catch` block in specific method | `ClaimReward` uses `Warn`; others use `Error` |
| Make success conditional | Move `logger.Info(Success)` inside `try` after processor call | Would change log semantics; update tests |
| Add retry logic | Wrap processor call in retry loop | Would require async or blocking wait; changes single-pass design |
| Add operation result tracking | Return `bool` or result object from methods | Would require interface changes; affects tests |

### Invariants

1. **Fixed Order:** `LogIn` → `ClaimReward` → `OpenPot` always
2. **All Operations Attempted:** No early return on failure
3. **Three Log Events Per Operation:** Started, Failure/Success (in catch), Success (always)
4. **Email in Every Log:** `MyLogInfoKey.Username` = `settings.EmailAddress`
5. **No State Mutation:** `BotService` has no mutable state; all state in dependencies

### Related Documentation

- [Architecture (Detailed)](./architecture.md#botservice-orchestration)
- [Components: Integration Models](./components/integration-models.md)
- [Flows: Authentication](./flows/authentication.md)
- [Flows: Reward Claim](./flows/reward-claim.md)
- [Flows: Lucky Pot](./flows/lucky-pot.md)
- [Error Handling](./error-handling.md)
- [Logging](./logging.md)
- [Testing](./testing.md)