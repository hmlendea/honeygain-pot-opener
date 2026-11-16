# Ambiguities and Open Questions

## Known Ambiguities

### 1. Success Logging Semantics

**Resolved:** Each operation now logs exactly one terminal event: `Success` when the processor call returns, or `Failure` when it throws. `BotService.ExecuteOperation` returns immediately after recording a failure.

```csharp
try { action(); }
catch { LogFailure(...); return; }
logger.Info(operation, OperationStatus.Success, logInfos);
```

**Current Behaviour:** Success is recorded only for completed operations.

### 2. ClaimReward Warning vs Error

**Resolved:** `ClaimReward` failure remains a `Warn`, and the operation is marked as having an expected failure. `LogIn` and `OpenPot` failures log `Error` and are recorded in `ExecutionReport.FailedOperations`, which drives the exit code.

**Current Behaviour:** `Warn` and expected for ClaimReward; `Error` and recorded for the others.

### 3. Single Account Only

**Ambiguity:** Architecture supports single `BotSettings` instance.

**Questions:**
- Is multi-account support planned?
- Should config support array of accounts?
- How would correlation work (single log file)?

**Current Behaviour:** One account per process; run multiple processes for multiple accounts

### 4. No Retry Logic

**Ambiguity:** Transient failures (network, timing) cause immediate operation failure.

**Questions:**
- Should there be configurable retry with backoff?
- Which operations are retryable? (LogIn yes, ClaimReward/OpenPot maybe)
- How to avoid duplicate reward claims on retry?

**Current Behaviour:** No retry; external scheduler re-invokes

### 5. Selector Fragility

**Ambiguity:** XPath selectors use text content (`contains(text(), 'Claim reward')`) which is locale-sensitive and UI-fragile.

**Questions:**
- Are there stable data attributes (data-testid, etc.)?
- Should selectors be configurable?
- Is there a Honeygain API to replace UI automation?

**Current Behaviour:** Hardcoded XPath/ID constants in `HoneygainProcessor`

### 6. Password in Memory

**Ambiguity:** Password stored as plain `string` in `BotSettings` singleton.

**Questions:**
- Should `SecureString` be used?
- Should password be zeroed after use?
- Is process memory scraping a realistic threat?

**Current Behaviour:** Plain string, no zeroing

### 7. Email in Logs (PII)

**Ambiguity:** Email logged as `Username` for correlation.

**Questions:**
- Is email considered PII requiring protection?
- Should it be hashed (SHA-256 with salt) in logs?
- Is correlation ID better than email?

**Current Behaviour:** Plain email in every operation log

### 8. Configuration Validation

**Resolved for required fields:** `Program.Main` evaluates `BotSettings.HasValidCredentials` after binding and aborts with a diagnostic message and exit code `1` before creating any browser.

**Remaining:** Value-specific validation, such as e-mail format or Honeygain account validity, is still detected at the point of use.

**Current Behaviour:** Presence validation at startup; format and account validation at use

### 9. Log Retention / Rotation

**Ambiguity:** Log file grows indefinitely with no rotation.

**Questions:**
- Should NuciLog configure rotation?
- External logrotate expected?
- What retention period?

**Current Behaviour:** No rotation, no retention policy

### 10. Exit Codes

**Resolved:** `Program.Main` sets `Environment.ExitCode` from `ExecutionReport.IsSuccessful` and from the guarded exception handlers.

- `0` when no unexpected operation failed, including when the reward was simply unavailable.
- `1` on an unexpected `LogIn` or `OpenPot` failure, a guarded exception, or incomplete credentials.

**Current Behaviour:** Exit code reflects whether the pass completed.

## Open Questions

### 1. Honeygain API Availability

**Question:** Does Honeygain provide a public/private API for these operations?

**Impact:** If yes, replace Selenium automation with API calls — more reliable, faster, no browser dependency.

**Status:** Unknown — no API documentation found

### 2. Multi-Account Support

**Question:** Should the application support multiple Honeygain accounts in one run?

**Options:**
- Config array: `botSettings: [{email, password}, ...]`
- Separate config files per account
- Separate process invocations (current)

**Decision Factors:** Operator complexity, log correlation, resource usage

### 3. Headless Mode Detection

**Question:** Does `WebDriverInitialiser.InitialiseAvailableWebDriver(isDebugMode)` properly configure headless Chrome when debug mode is disabled?

**Concerns:** Some sites detect headless and block. May need `--disable-blink-features=AutomationControlled`.

**Status:** Unverified — depends on NuciWeb implementation

### 4. Cookie Persistence

**Question:** Does `WebDriverInitialiser` reuse browser profile between runs?

**Impact:** If yes, login session may persist (faster). If no, full login every run.

**Status:** Unknown — depends on NuciWeb implementation

### 5. Concurrent Runs Safety

**Question:** What happens if two instances run simultaneously?

**Risks:** WebDriver conflict, profile corruption, Honeygain rate limiting.

**Mitigation:** External scheduler must prevent overlap (systemd `Persistent=true`, cron no overlap).

### 6. .NET 10 Target Framework

**Question:** Is `net10.0` intentional (preview/LTS) or should it be `net8.0`/`net9.0` for stability?

**Impact:** .NET 10 may not be GA. Self-contained publish includes runtime anyway.

**Status:** Current csproj targets `net10.0`

### 7. NuciLog/NuciWeb Version Alignment

**Question:** Are `NuciLog 1.2.1` + `NuciLog.Core 3.1.0` and `NuciWeb 4.0.0` + `Automation 1.0.0` + `Selenium 1.0.2` compatible?

**Observation:** Different major versions for core vs facade. May indicate breaking changes.

**Status:** Builds and tests pass — appears compatible

### 8. Test Coverage Gaps

**Question:** Are there untested code paths?

**Gaps:**
- `Program.cs` (composition root) — no tests
- `WebDriverInitialiser` — external dependency
- `NuciLog` integration — no tests
- Error paths in `HoneygainProcessor` (only business logic exceptions tested)

### 9. Dependency Update Policy

**Question:** How often should dependencies be updated?

**Current:** Exact versions pinned. No automated Dependabot/Renovate.

**Options:** Monthly, per .NET release, security-only, never.

### 10. Observability Beyond Logs

**Question:** Should the application expose metrics/health endpoints?

**Options:**
- Prometheus metrics endpoint
- Health check endpoint (for Kubernetes)
- Structured JSON logs for log aggregation
- OpenTelemetry tracing

**Status:** None implemented

## Decisions Needed

| # | Question | Priority | Owner |
|---|----------|----------|-------|
| 1 | Add retry logic for transient failures? | Medium | Maintainer |
| 2 | Hash email in logs? | Medium | Maintainer |
| 3 | Investigate Honeygain API? | Medium | Maintainer |
| 4 | Add log rotation? | Low | Maintainer |
| 5 | Multi-account support? | Low | Maintainer |
| 6 | SecureString for password? | Low | Maintainer |
| 7 | Metrics/health endpoints? | Low | Maintainer |

## Related Documentation

- [Error Handling](./error-handling.md)
- [Logging](./logging.md)
- [Security](./security.md)
- [Configuration](./configuration.md)
- [Invariants](./invariants.md)
- [Change Guide](./change-guide.md)