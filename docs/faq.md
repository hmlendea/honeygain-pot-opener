# FAQ

## General

### What does this application do?

Automates three actions on the Honeygain dashboard:
1. **Log in** with configured credentials
2. **Claim daily reward** (if available)
3. **Open Lucky Pot** (if available)

Runs once and exits. Designed for daily scheduled execution.

### What are the requirements?

- .NET 10 Runtime (or self-contained publish)
- Chrome/Chromium browser installed
- Honeygain account with credentials
- Network access to `dashboard.honeygain.com`

### How do I run it?

```bash
# Build
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish

# Configure
cp appsettings.json.template appsettings.json
# Edit appsettings.json with real credentials
chmod 600 appsettings.json

# Run
./publish/HoneygainPotOpener
```

## Configuration

### Where do I put my credentials?

In `appsettings.json`:
```json
{
  "botSettings": {
    "emailAddress": "your@email.com",
    "password": "your-password"
  }
}
```

**Never commit real credentials.** Repository contains placeholders.

### How do I run in debug mode (visible browser)?

```json
{
  "debugSettings": {
    "isDebugMode": true
  }
}
```

### How do I change log level?

```json
{
  "nuciLoggerSettings": {
    "minimumLevel": "Debug",
    "logFilePath": "./logfile.log",
    "isFileOutputEnabled": true
  }
}
```

Levels: `Debug`, `Info`, `Warning`, `Error`, `Fatal`

### Can I use environment variables instead of config file?

**Not currently supported.** Only `appsettings.json` is read.

## Operations

### Why does it claim reward even when there's none?

The application **always attempts** all three operations. If no reward is available:
- `ClaimReward` logs `Warning` (expected condition)
- Continues to `OpenPot`
- This is by design — single pass, best effort

### Why does "Success" log appear after a failure?

**By design.** Each operation logs:
1. `Started`
2. `Failure` (in catch block) — with exception
3. `Success` (always, after catch)

**Success log ≠ operation succeeded.** Check for `Failure` log to detect errors.

### What if Honeygain changes their UI?

Selectors in `HoneygainProcessor.cs` will break. Update the XPath/ID constants:
- `CookieAcceptButtonId`
- `EmailInputId`
- `PasswordInputId`
- `LoginButtonId`
- `DashboardIndicatorXPath`
- `ClaimRewardButtonXPath`
- `OpenLuckyPotButtonXPath`

Run tests to verify: `dotnet test`

### Does it support multiple accounts?

**No.** Single `BotSettings` instance. For multiple accounts, run multiple instances with separate configs.

## Troubleshooting

### "NoSuchElementException" / "Element not found"

Honeygain UI changed. Update selectors in `HoneygainProcessor.cs`.

### "WebDriverTimeoutException"

Page load too slow or element not appearing. Check:
- Network connectivity to `dashboard.honeygain.com`
- Chrome/Chromium installed and compatible
- Selectors still valid

### "InvalidOperationException: There is no reward available"

**Normal.** No reward to claim today. Check `Warning` log level.

### "InvalidOperationException: The Lucky Pot cannot be opened"

**Normal.** Pot not available (already opened or not ready). Check `Error` log level.

### Log file shows "Fatal"

Unhandled exception. Check exception details in log. Common causes:
- Invalid `appsettings.json` (malformed JSON)
- Chrome not installed
- WebDriver version mismatch

### Exit code is non-zero

Unguarded exception during startup (config, WebDriver init, DI). Check logs before `StartUp` entry.

## Security

### Is my password safe?

- Stored in `appsettings.json` (plain text) — **secure this file** (`chmod 600`)
- Never logged
- Only in memory during execution
- No encryption at rest (current limitation)

### Is my email in logs?

**Yes.** Logged as `Username` for correlation. This is intentional.

### Can I use a secret manager?

**Not currently supported.** Planned: environment variables, Azure Key Vault, HashiCorp Vault.

## Development

### How do I run tests?

```bash
dotnet test HoneygainPotOpener.slnx -c Release
```

### How do I add a new operation?

1. Add method to `IHoneygainProcessor` and `HoneygainProcessor`
2. Add `Operation` constant to `MyOperation`
3. Add call in `BotService.Run()` with try/catch/logging
4. Add tests in `HoneygainProcessorTests` and `BotServiceTests`
5. Update documentation

### Why no async/await?

Simplicity. Single-threaded, blocking calls. External scheduler handles timing.

## Deployment

### How do I deploy as a daily job?

**Linux (systemd timer):**
```ini
# /etc/systemd/system/honeygain-pot-opener.timer
[Timer]
OnCalendar=daily
Persistent=true
```

**Windows (Task Scheduler):** Daily trigger at desired time.

**Docker:** See `build-and-deployment.md` for Dockerfile.

### Can I run in Kubernetes?

Yes, as a `CronJob`:
```yaml
schedule: "0 6 * * *"
jobTemplate:
  spec:
    template:
      spec:
        containers:
        - name: honeygain-pot-opener
          image: your-registry/honeygain-pot-opener:latest
        restartPolicy: OnFailure
```

## Related Documentation

- [Quick Start](./quick-start.md)
- [Configuration](./configuration.md)
- [Troubleshooting](./troubleshooting.md)
- [Security](./security.md)
- [Build and Deployment](./build-and-deployment.md)