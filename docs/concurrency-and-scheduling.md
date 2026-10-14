# Concurrency and Scheduling

## Concurrency Model

### Single-Threaded Execution

The application is **strictly single-threaded**:

- `Program.Main` runs on the main thread
- `IBotService.Run()` executes synchronously
- All `IWebProcessor` calls are blocking
- No `async`/`await`, no `Task`, no `ThreadPool` usage

### No Parallelism

| Aspect | Status |
|--------|--------|
| Multiple accounts | Not supported (single `BotSettings`) |
| Parallel operations | Not supported (sequential: LogIn → ClaimReward → OpenPot) |
| Background tasks | None |
| Timer-based triggers | None (external scheduler only) |

### Thread Safety

| Component | Thread-Safe | Notes |
|-----------|-------------|-------|
| `BotSettings` | Yes (immutable after init) | Singleton, no mutation |
| `DebugSettings` | Yes (immutable after init) | Singleton, no mutation |
| `ILogger` (NuciLog) | Yes | NuciLog guarantees thread safety |
| `IWebProcessor` (Selenium) | **No** | WebDriver is not thread-safe; single instance |
| `HoneygainProcessor` | N/A | Transient, single-threaded use |
| `BotService` | N/A | Singleton, single-threaded use |

## Scheduling

### External Scheduler Only

The application has **no internal scheduling**. It runs once and exits.

**Invocation Models:**

| Scheduler | Example | Frequency |
|-----------|---------|-----------|
| cron | `0 6 * * *` | Daily at 6 AM |
| systemd timer | `OnCalendar=daily` | Daily |
| Windows Task Scheduler | Daily trigger | Daily |
| Kubernetes CronJob | `schedule: "0 6 * * *"` | Daily |
| Manual | `./HoneygainPotOpener` | Ad-hoc |

### No Internal Timing

| Feature | Implemented |
|---------|-------------|
| `Thread.Sleep` between operations | No |
| Configurable delays | No |
| Retry with backoff | No |
| Rate limiting | No |
| Randomised jitter | No |

## Execution Timeline

```
Process Start
    │
    ▼
Load Configuration (sync, blocking)
    │
    ▼
Initialise WebDriver (sync, blocking)  ← ~2-5 seconds
    │
    ▼
DI Container Build (sync, blocking)
    │
    ▼
BotService.Run()
    │
    ├─► LogIn()          ← ~10-30 seconds (network + page loads)
    │     ├─► GoToUrl
    │     ├─► Wait/Click/SetText (multiple)
    │     └─► WaitForDashboard
    │
    ├─► ClaimReward()    ← ~2-5 seconds
    │     ├─► IsElementVisible
    │     └─► Click (if visible)
    │
    └─► OpenPot()        ← ~5-15 seconds
          ├─► WaitForElementToBeVisible
          ├─► IsElementVisible (double-check)
          └─► DoubleClick
    │
    ▼
WebDriver.Quit() (sync, blocking)  ← ~1-2 seconds
    │
    ▼
Process Exit
```

**Total typical runtime:** 20-50 seconds

## Resource Contention

### WebDriver (Critical Section)

- Single `IWebDriver` instance wrapped by `IWebProcessor`
- Not shareable across processes
- **Only one instance per machine at a time** (unless separate browser profiles)

### Browser Profile

- WebDriverInitialiser may reuse or create profile
- Concurrent runs → profile corruption / session conflicts
- **Mitigation:** External scheduler ensures single invocation

### File Locks

| File | Lock Type | Contention |
|------|-----------|------------|
| `appsettings.json` | Read-only | None |
| `logfile.log` | Append (NuciLog) | Multiple processes → interleaved logs |
| Browser profile | Read/Write (Selenium) | **High** — single process only |

## Scaling Considerations

### Horizontal Scaling (Multiple Accounts)

**Not supported.** Would require:

1. Multiple `BotSettings` instances (config array)
2. Multiple `IWebProcessor` / WebDriver instances
3. Parallel `BotService` execution
4. Separate log correlation per account
5. Separate browser profiles

### Vertical Scaling

**Not applicable** — CPU/memory not bottlenecks. Network and browser are.

## Deadlocks / Race Conditions

### None Possible (Current Design)

- Single thread
- No locks (`lock`, `Monitor`, `SemaphoreSlim`)
- No async/await
- No shared mutable state

### If Async Introduced

| Risk | Mitigation |
|------|------------|
| WebDriver not thread-safe | Single WebDriver per account; no sharing |
| NuciLog file sink | Use async sink or single writer |
| Config reload | Immutable settings or `IOptionsMonitor` |

## Scheduler Integration

### Exit Codes for Scheduler

| Exit Code | Meaning | Scheduler Action |
|-----------|---------|------------------|
| 0 | Completed (any operation success/failure) | Success — schedule next run |
| Non-zero | Unguarded crash (config, WebDriver init) | Alert — investigate |

**Current:** Always returns 0 (void Main) unless unguarded exception.

### Recommended: Explicit Exit Codes

```csharp
// Program.cs
int exitCode = 0;
try
{
    serviceProvider.GetService<IBotService>().Run();
}
catch (AggregateException ex)
{
    // ... log ...
    exitCode = 2;  // Operation failures
}
catch (Exception ex)
{
    // ... log ...
    exitCode = 1;  // Unexpected error
}
finally
{
    webDriver.Quit();
}
Environment.Exit(exitCode);
```

### Scheduler Health Checks

| Check | Method |
|-------|--------|
| Process running | PID file / systemd / Task Scheduler |
| Log freshness | `logfile.log` modified within 24h |
| Success pattern | Grep for `Operation=OpenPot Status=Success` |
| Failure pattern | Grep for `Fatal` or repeated `Failure` |

## Future: Internal Scheduling (If Needed)

### Option 1: Hosted Service (Generic Host)

```csharp
// Requires: Microsoft.Extensions.Hosting
public class BotHostedService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Run once
            botService.Run();

            // Wait 24h (configurable)
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

### Option 2: Timer in Main

```csharp
// Simple but blocks thread
while (true)
{
    botService.Run();
    Thread.Sleep(TimeSpan.FromHours(24));
}
```

### Option 3: External Trigger (Recommended)

Keep current model — external scheduler is more reliable, observable, and standard.

## Related Documentation

- [Architecture](../architecture.md)
- [Components: Host and Composition](../components/host-and-composition.md)
- [Build and Deployment](../build-and-deployment.md)
- [Error Handling](../error-handling.md)