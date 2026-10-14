# State and Persistence

## Overview

This application has **no persistent state**. It is a stateless, single-run console application.

## In-Memory State (Transient)

### Configuration Objects

| Object | Lifetime | Mutability | Persistence |
|--------|----------|------------|-------------|
| `BotSettings` | Process | Immutable after bind | None |
| `DebugSettings` | Process | Immutable after bind | None |

### Service Instances

| Service | Lifetime | State |
|---------|----------|-------|
| `ILogger` | Singleton | Internal buffers (NuciLog) |
| `IWebProcessor` | Singleton | WebDriver session |
| `IHoneygainProcessor` | Transient | None (stateless) |
| `IBotService` | Singleton | None (stateless) |

### WebDriver Session

- Created at startup via `WebDriverInitialiser.Initialise()`
- Wrapped by `SeleniumWebProcessor` (singleton)
- **Ends at `driver.Quit()` in finally block**
- No session persistence across runs
- No cookie/file persistence (unless WebDriverInitialiser configures profile)

## No Persistent Storage

| Storage Type | Used | Details |
|--------------|------|---------|
| Database | No | — |
| File system (non-log) | No | — |
| Cache | No | — |
| Local storage (browser) | No | Cleared on Quit |
| Session storage (browser) | No | Cleared on Quit |
| IndexedDB | No | Cleared on Quit |
| Credential manager | No | — |
| Keyring / Secret service | No | — |

## Log File (Only Persistent Artifact)

| Aspect | Detail |
|--------|--------|
| **File** | `logfile.log` (configurable) |
| **Format** | NuciLog structured |
| **Retention** | None (indefinite) |
| **Rotation** | None |
| **Content** | Structured log events with Username |
| **Sensitivity** | Contains email (PII), no passwords |

## State Transitions

```
START
  │
  ▼
Load Config → BotSettings, DebugSettings (memory)
  │
  ▼
Init WebDriver → Browser session (memory + OS process)
  │
  ▼
Build DI Container → Service instances (memory)
  │
  ▼
BotService.Run()
  │
  ├─► LogIn() → Honeygain session (browser cookies, memory)
  ├─► ClaimReward() → Reads page state (DOM)
  └─► OpenPot() → Reads page state (DOM)
  │
  ▼
driver.Quit() → Browser session ENDS
  │
  ▼
Process EXIT → All memory RELEASED
  │
  ▼
END (only logfile.log remains)
```

## Honeygain Session State (External)

| State | Location | Lifetime |
|-------|----------|----------|
| Authentication cookies | Browser profile (memory) | Until `Quit()` |
| CSRF tokens | Page DOM / cookies | Until `Quit()` |
| Reward availability | Honeygain server | External |
| Lucky Pot availability | Honeygain server | External |

**No local caching** of reward/pot state — always reads live from UI.

## Configuration as "State"

The closest thing to persistent state is `appsettings.json`:

- **Read once** at startup
- **Never written** by application
- **Operator-managed** (manual edits)
- **No schema migration** — additive only

## Recovery

### No Recovery Needed

- No in-progress operations to recover
- No transactions to roll back
- No checkpoints
- External scheduler simply re-runs

### Idempotency

| Operation | Idempotent | Notes |
|-----------|------------|-------|
| LogIn | No | Creates new session |
| ClaimReward | Yes (if already claimed, button not visible) | Returns Warn |
| OpenPot | Yes (if already opened, button not visible) | Returns Error |

**Re-running is safe** — worst case: logs Warn/Error for already-completed actions.

## Related Documentation

- [Data Model](./data-model.md)
- [Concurrency and Scheduling](./concurrency-and-scheduling.md)
- [Error Handling](./error-handling.md)
- [Logging](./logging.md)
- [Security](./security.md)