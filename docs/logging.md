# Logging

## Logging Framework

**Library:** NuciLog (v1.2.1) + NuciLog.Core (v3.1.0)

**Configuration Source:** `appsettings.json` → `nuciLoggerSettings` section

**Registration:** `Program.cs` → `services.AddNuciLoggerSettings(config)` → `AddSingleton<ILogger, NuciLogger>()`

**Implementation:** `NuciLog.NuciLogger` implements `NuciLog.Core.ILogger`

## Log Levels

| Level | NuciLog Constant | Usage in Application |
|-------|------------------|---------------------|
| Debug | `LogLevel.Debug` | Not used (minimum level is Info) |
| Info | `LogLevel.Info` | `Operation.StartUp`, operation Started/Success |
| Warning | `LogLevel.Warning` | `ClaimReward` failure (expected condition) |
| Error | `LogLevel.Error` | `LogIn`/`OpenPot` failures, unhandled exceptions |
| Fatal | `LogLevel.Fatal` | Unhandled exceptions in `Program.Main` guard |

**Configured Minimum:** `Info` (from `nuciLoggerSettings.minimumLevel`)

## Structured Fields

Every log entry includes structured context via `LogInfo` records.

### Standard Fields (NuciLog)

| Field | Source | Description |
|-------|--------|-------------|
| Timestamp | NuciLog | ISO 8601 timestamp |
| Level | NuciLog | Debug/Info/Warning/Error/Fatal |
| Operation | Caller | `Operation` enum value (e.g., `LogIn`, `ClaimReward`) |
| Status | Caller | `OperationStatus` enum (Started, Success, Failure) |
| Message | Caller | Human-readable message |
| Exception | Caller | Exception object (when provided) |

### Application-Specific Fields

| Key | Type | Source | Present In |
|-----|------|--------|------------|
| `Username` | string | `MyLogInfoKey.Username` | All operation logs (LogIn, ClaimReward, OpenPot) |

**Username Value:** `BotSettings.EmailAddress` (configured Honeygain email)

### LogInfo Creation

```csharp
// In BotService.LogIn(), ClaimReward(), OpenPot()
IEnumerable<LogInfo> logInfos = [
    new(MyLogInfoKey.Username, settings.EmailAddress)
];
```

## Correlation

### Current Correlation Mechanism

**Single correlation key:** `Username` (Honeygain email address)

**Scope:** Per-process invocation — all operations in one run share the same Username

**Limitations:**
- No request ID / trace ID / span ID
- No correlation across process invocations
- No correlation with external systems (Honeygain session ID not captured)
- Single-account only — no multi-account correlation

### Correlation Flow

```
Process Start
    │
    ▼
Operation.StartUp (no Username)
    │
    ▼
LogIn Started (Username: user@example.com)
LogIn Failure/Success (Username: user@example.com)
    │
    ▼
ClaimReward Started (Username: user@example.com)
ClaimReward Failure/Success (Username: user@example.com)
    │
    ▼
OpenPot Started (Username: user@example.com)
OpenPot Failure/Success (Username: user@example.com)
```

## Sinks / Destinations

### Configured Sinks (via NuciLog)

**File Sink (Default):**
- **Path:** `./logfile.log` (from `nuciLoggerSettings.logFilePath`)
- **Enabled:** `nuciLoggerSettings.isFileOutputEnabled` (default `true`)
- **Format:** NuciLog structured format (JSON-like)
- **Rotation:** Not configured (no size/date rotation)
- **Retention:** Not configured (indefinite)

**Console Sink:**
- **Enabled:** Not explicitly configured (NuciLog default behaviour)
- **Format:** Human-readable

### Sink Configuration

```json
{
  "nuciLoggerSettings": {
    "logFilePath": "./logfile.log",
    "minimumLevel": "Info",
    "isFileOutputEnabled": true
  }
}
```

### No Other Sinks

- No database sink
- No external service (Seq, Elasticsearch, Datadog, etc.)
- No syslog / journald
- No custom sinks

## Redaction / Masking

### Current: No Automatic Redaction

**NuciLog does not automatically redact sensitive fields.**

### Manual Protection

| Data | Protection |
|------|------------|
| Password | **Never passed to logger** — only used in `SetText` call |
| Email | **Logged as Username** — intentional for correlation |
| Session cookies | Not logged |
| Honeygain responses | Not logged |

### Risk Areas

| Risk | Status |
|------|--------|
| Password in exception message | Not observed — exceptions from Selenium don't include password |
| Password in log context | Not passed — only `Username` LogInfo used |
| Email in logs | **Intentional** — correlation key |
| Credentials in log file | Only email; password absent |

## Retention

### Current: No Retention Policy

| Aspect | Configuration |
|--------|---------------|
| File rotation | None |
| Max file size | Unlimited |
| Max file age | Unlimited |
| Max file count | Unlimited (single file) |
| Archival | None |
| Cleanup | Manual only |

### Operator Responsibilities

- Monitor `logfile.log` size
- Implement external log rotation (logrotate, scheduled task)
- Archive/delete old logs per organisational policy

## Sampling

### Current: No Sampling

- All events at or above `minimumLevel` are logged
- No rate limiting
- No probabilistic sampling
- No burst protection

## Performance Considerations

### Synchronous Logging

**NuciLog default:** Synchronous file writes

**Impact:**
- Each `logger.Info/Error/Warn/Fatal` call blocks until file write completes
- File I/O on critical path during operations
- No async/await in logging calls

### Throughput Impact

| Operation | Log Calls | Blocking I/O |
|-----------|-----------|--------------|
| Startup | 1 | Yes |
| LogIn | 3 (Started, Failure/Success, Success) | Yes |
| ClaimReward | 3 | Yes |
| OpenPot | 3 | Yes |
| **Total per run** | **10** | **Yes** |

**Typical latency per log call:** 0.1-1ms (local SSD), higher on network storage

### Mitigation (Future)

- Configure NuciLog async sink if available
- Batch log writes
- Use in-memory buffer with background flush

## Error Logging

### Exception Logging Pattern

```csharp
// In BotService
catch (Exception ex)
{
    logger.Error(MyOperation.LogIn, OperationStatus.Failure, ex, logInfos);
}
```

**NuciLog.Exception Handling:**
- Exception type, message, and stack trace included
- Inner exceptions included (recursive)
- Structured as part of log entry

### Fatal Logging (Program.Main)

```csharp
catch (AggregateException ex)
{
    foreach (Exception innerException in ex.InnerExceptions)
    {
        logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
    }
}
catch (Exception ex)
{
    logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
}
```

- **Operation:** `Operation.Unknown` (NuciLog built-in)
- **Status:** `OperationStatus.Failure`
- **Context:** No `LogInfo` (no Username)

## Audit Logging

### Current: No Separate Audit Log

All events go to same log sink with same structure.

### Security-Relevant Events Logged

| Event | Operation | Level | Context |
|-------|-----------|-------|---------|
| Process start | `StartUp` | Info | None |
| Login attempt | `LogIn` | Info/Error | Username |
| Login failure | `LogIn` | Error | Username + Exception |
| Reward claim attempt | `ClaimReward` | Info/Warn | Username |
| Reward claim failure | `ClaimReward` | Warn | Username + Exception |
| Pot open attempt | `OpenPot` | Info/Error | Username |
| Pot open failure | `OpenPot` | Error | Username + Exception |
| Unhandled exception | `Unknown` | Fatal | Exception only |

### Not Logged (Audit Gaps)

- Configuration values (except email in Username)
- WebDriver initialisation details
- Honeygain HTTP requests/responses
- Selector resolution
- Browser console logs

## Configuration

### appsettings.json

```json
{
  "nuciLoggerSettings": {
    "logFilePath": "./logfile.log",
    "minimumLevel": "Info",
    "isFileOutputEnabled": true
  }
}
```

### Program.cs Registration

```csharp
services.AddNuciLoggerSettings(config)
    .AddSingleton<ILogger, NuciLogger>();
```

### Runtime Level Change

**Not supported** — `minimumLevel` read once at startup. No `IOptionsMonitor` for NuciLog settings.

### Disabling File Output

```json
{
  "nuciLoggerSettings": {
    "isFileOutputEnabled": false
  }
}
```

## Log Output Format (Example)

```
[2026-10-14T10:30:00.123Z] INFO  Operation=StartUp Status=Success Message="Application started"
[2026-10-14T10:30:05.456Z] INFO  Operation=LogIn Status=Started Username="user@example.com"
[2026-10-14T10:30:15.789Z] INFO  Operation=LogIn Status=Success Username="user@example.com"
[2026-10-14T10:30:16.012Z] INFO  Operation=ClaimReward Status=Started Username="user@example.com"
[2026-10-14T10:30:16.234Z] WARN  Operation=ClaimReward Status=Failure Username="user@example.com" Exception="InvalidOperationException: There is no reward available..."
[2026-10-14T10:30:16.235Z] INFO  Operation=ClaimReward Status=Success Username="user@example.com"
[2026-10-14T10:30:16.456Z] INFO  Operation=OpenPot Status=Started Username="user@example.com"
[2026-10-14T10:30:16.678Z] INFO  Operation=OpenPot Status=Success Username="user@example.com"
```

## Modification Guidance

| Change | Files | Considerations |
|--------|-------|----------------|
| Add log field | `BotService` methods → new `LogInfo` | Update `MyLogInfoKey` if new key type |
| Change log level | `BotService` catch blocks | `ClaimReward` uses Warn; others Error |
| Add correlation ID | Generate in `Program`, pass via settings | Requires new `LogInfoKey` and plumbing |
| Enable async logging | NuciLog configuration | Check NuciLog docs for async sink |
| Add log rotation | External (logrotate) or custom sink | Not in NuciLog config |
| Add structured output (JSON) | NuciLog formatter config | Check NuciLog extensibility |
| Add external sink | Custom `ILogger` implementation | Replace `NuciLogger` registration |

## Invariants

1. **Email in every operation log** — `Username` LogInfo always present
2. **Password never logged** — no code path passes password to logger
3. **Three events per operation** — Started, Failure/Success (in catch), Success (always)
4. **Startup logged first** — `Operation.StartUp` before any operation
5. **Fatal only in Program.Main guard** — not in `BotService` or `HoneygainProcessor`
6. **No correlation ID** — only Username for correlation

## Related Documentation

- [Components: Application Services](./components/application-services.md)
- [Flows: Authentication](./flows/authentication.md)
- [Flows: Reward Claim](./flows/reward-claim.md)
- [Flows: Lucky Pot](./flows/lucky-pot.md)
- [Error Handling](./error-handling.md)
- [Security](./security.md)
- [Privacy](../PRIVACY.md)