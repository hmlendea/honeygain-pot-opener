# Inspect, Edit, Delete Behaviour

## Overview

This document describes the configuration binding, settings objects, and runtime modification constraints. The application uses a simple configuration model with no runtime reload capability.

## Configuration Binding

### Startup Binding (Once)

```csharp
// Program.cs
BotSettings botSettings = new();
DebugSettings debugSettings = new();

IConfiguration config = LoadConfiguration();
config.Bind(nameof(BotSettings), botSettings);
config.Bind(nameof(DebugSettings), debugSettings);
```

- **Occurs once** at process startup
- **Objects created first**, then bound
- **No IOptionsMonitor** — no change notifications
- **reloadOnChange: true** in JSON provider has no effect on bound objects

### Settings Objects

#### BotSettings

**File:** `HoneygainPotOpener/Configuration/BotSettings.cs`

```csharp
public sealed class BotSettings
{
    public string EmailAddress { get; set; }
    public string Password { get; set; }
}
```

- **Properties:** `EmailAddress`, `Password` (both nullable strings)
- **Defaults:** `null` (not initialised)
- **Validation:** None — null/empty fails at point of use
- **Lifetime:** Singleton (registered as instance)

#### DebugSettings

**File:** `HoneygainPotOpener/Configuration/DebugSettings.cs`

```csharp
public sealed class DebugSettings
{
    public bool IsDebugMode { get; set; }
    public bool IsHeadless => !IsDebugMode;
}
```

- **Properties:** `IsDebugMode` (bool, default `false`), `IsHeadless` (computed)
- **Computed property:** `IsHeadless` is **not bound** — derived from `IsDebugMode`
- **Lifetime:** Singleton (registered as instance)

## Runtime Modification Constraints

### No Runtime Reload

| Aspect | Behaviour |
|--------|-----------|
| `appsettings.json` changes during execution | **Ignored** — bound objects not refreshed |
| `IConfiguration` reload token | Fires but no consumers listen |
| `BotSettings`/`DebugSettings` mutation | Possible via reflection but not designed for |
| Process restart required | Yes — for any configuration change |

### Configuration Precedence

**Only one source:** `appsettings.json` in working directory

| Source | Priority | Supported |
|--------|----------|-----------|
| `appsettings.json` | 1 (only) | Yes |
| Environment variables | — | No |
| Command-line arguments | — | No |
| User secrets | — | No |
| Azure Key Vault | — | No |
| Custom providers | — | No |

### Secret Management

**Current:** Plain text in `appsettings.json`

```json
{
  "botSettings": {
    "emailAddress": "user@example.com",
    "password": "actual-password"
  }
}
```

**No encryption, no vault integration, no environment variable override.**

**Operator Responsibilities:**
- Restrict file permissions (`chmod 600 appsettings.json`)
- Never commit real credentials to version control
- Use placeholder values in repository (`[[HONEYGAIN_EMAIL_ADDRESS]]`)

## Inspecting Configuration

### At Startup (Logs)

No automatic logging of configuration values. Only `Operation.StartUp` is logged.

### Via Debugger

Attach debugger to inspect:
- `botSettings.EmailAddress`
- `botSettings.Password`
- `debugSettings.IsDebugMode`
- `debugSettings.IsHeadless`

### Via Logs (Email Only)

Email address appears in every operation log as `Username` context:
```
Operation: LogIn, Status: Started, Username: user@example.com
```

Password **never** appears in logs.

## Editing Configuration

### File Location

`HoneygainPotOpener/appsettings.json` (source)
`HoneygainPotOpener/bin/Debug/net10.0/appsettings.json` (copied to output)

### Editable Fields

| Section | Key | Type | Required | Description |
|---------|-----|------|----------|-------------|
| `botSettings` | `emailAddress` | string | Yes | Honeygain account email |
| `botSettings` | `password` | string | Yes | Honeygain account password |
| `debugSettings` | `isDebugMode` | bool | No | `true` = visible browser, `false` = headless |
| `nuciLoggerSettings` | `logFilePath` | string | No | Log file path (default `./logfile.log`) |
| `nuciLoggerSettings` | `minimumLevel` | string | No | `Debug`, `Info`, `Warning`, `Error`, `Fatal` |
| `nuciLoggerSettings` | `isFileOutputEnabled` | bool | No | Enable/disable file logging |

### Example: Enable Debug Mode

```json
{
  "debugSettings": {
    "isDebugMode": true
  }
}
```

**Effect:** Browser window visible during automation (useful for selector debugging).

### Example: Disable File Logging

```json
{
  "nuciLoggerSettings": {
    "isFileOutputEnabled": false
  }
}
```

**Effect:** No `logfile.log` created; logs only to console (if NuciLog console sink enabled).

## Deleting Configuration

### Remove Credentials

Set to empty strings or placeholders:
```json
{
  "botSettings": {
    "emailAddress": "",
    "password": ""
  }
}
```

**Result:** `LogIn()` will fail with empty credentials (element interaction or auth failure).

### Remove File

Delete `appsettings.json` from working directory.

**Result:** Configuration provider accepts missing file (optional); settings remain `null`/`false`; operations fail at point of use.

## Testing Configuration

### Unit Tests

**File:** `HoneygainPotOpener.UnitTests/Configuration/`

- `BotSettingsTests.cs` — verifies property assignment
- `DebugSettingsTests.cs` — verifies `IsHeadless` computation

### Integration Testing

No automated integration tests for configuration loading. Manual verification:
1. Modify `appsettings.json`
2. Run `dotnet run --project HoneygainPotOpener.csproj`
3. Observe behaviour (headless vs visible, log file creation)

## Modification Guidance

| Change | Files | Considerations |
|--------|-------|----------------|
| Add new setting | New class in `Configuration/`, bind in `Program.cs` | Register as singleton; update `appsettings.json` |
| Add environment variable support | `Program.cs` → `AddEnvironmentVariables()` | Precedence: env vars override JSON |
| Add command-line args | `Program.cs` → `AddCommandLine(args)` | Parse before config bind |
| Add secret vault | `Program.cs` → custom provider | Requires additional NuGet packages |
| Add validation | `BotSettings`/`DebugSettings` constructors or `IValidatableObject` | Fail fast at startup |
| Enable reload | Use `IOptionsMonitor<T>` instead of direct bind | Requires DI changes; consumers must inject `IOptionsMonitor` |

## Invariants

1. **Configuration bound once** at startup
2. **No runtime reload** — process restart required for changes
3. **Single source** — `appsettings.json` only
4. **No validation** — failures at point of use
5. **Password never logged** — only email in log context
6. **DebugSettings.IsHeadless computed** — not bound from config

## Related Documentation

- [Configuration](./configuration.md)
- [Components: Host and Composition](./components/host-and-composition.md)
- [Flows: Startup and Rendering](./flows/startup-and-rendering.md)
- [Security](./security.md)
- [Privacy](../PRIVACY.md)