# Configuration

## Schema

### appsettings.json Structure

```json
{
  "botSettings": {
    "emailAddress": "string",
    "password": "string"
  },
  "debugSettings": {
    "isDebugMode": "boolean"
  },
  "nuciLoggerSettings": {
    "logFilePath": "string",
    "minimumLevel": "string (Debug|Info|Warning|Error|Fatal)",
    "isFileOutputEnabled": "boolean"
  }
}
```

### Section Details

#### botSettings

| Key | Type | Required | Default | Description |
|-----|------|----------|---------|-------------|
| `emailAddress` | string | Yes | `null` | Honeygain account email |
| `password` | string | Yes | `null` | Honeygain account password |

#### debugSettings

| Key | Type | Required | Default | Description |
|-----|------|----------|---------|-------------|
| `isDebugMode` | boolean | No | `false` | `true` = visible browser, `false` = headless |

**Computed (not in config):**
- `IsHeadless` = `!IsDebugMode` (in `DebugSettings` class)

#### nuciLoggerSettings

| Key | Type | Required | Default | Description |
|-----|------|----------|---------|-------------|
| `logFilePath` | string | No | `./logfile.log` | Relative or absolute path |
| `minimumLevel` | string | No | `Info` | `Debug`, `Info`, `Warning`, `Error`, `Fatal` |
| `isFileOutputEnabled` | boolean | No | `true` | Enable file sink |

## Sources

### Current: Single JSON File

**File:** `appsettings.json` in working directory

**Provider:** `Microsoft.Extensions.Configuration.Json`

```csharp
new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();
```

- **Optional:** Missing file does not throw
- **Reload on change:** Enabled but ineffective for bound objects

### Not Supported

| Source | Supported |
|--------|-----------|
| Environment variables | No |
| Command-line arguments | No |
| User secrets (development) | No |
| Azure Key Vault | No |
| HashiCorp Vault | No |
| Custom providers | No |

## Precedence

**Single source only** — no precedence chain.

If multiple sources were added, typical precedence (highest first):
1. Command-line arguments
2. Environment variables
3. User secrets
4. `appsettings.{Environment}.json`
5. `appsettings.json`

## Binding

### One-Time Binding at Startup

```csharp
BotSettings botSettings = new();
DebugSettings debugSettings = new();

config.Bind(nameof(BotSettings), botSettings);
config.Bind(nameof(DebugSettings), debugSettings);
```

- Objects created **before** binding
- Bound **once** during `Program.Main`
- Registered as **singleton instances** in DI
- **No `IOptions<T>` or `IOptionsMonitor<T>`** — direct instances

### Binding Behaviour

| Scenario | Result |
|----------|--------|
| Missing section | Properties remain default (`null`/`false`) |
| Missing property | Property remains default |
| Type mismatch | Binding error (exception at bind time) |
| Extra properties in JSON | Ignored |
| `reloadOnChange: true` | Fires reload token but no listeners |

## Validation

### Current: None

- No data annotations on settings classes
- No `IValidatableObject` implementation
- No startup validation
- Failures occur at **point of use** (e.g., `SetText` with null email)

### Recommended Validation (Future)

```csharp
// Option 1: Constructor validation
public sealed class BotSettings
{
    public string EmailAddress { get; }
    public string Password { get; }

    public BotSettings(string emailAddress, string password)
    {
        EmailAddress = emailAddress ?? throw new ArgumentNullException(nameof(emailAddress));
        Password = password ?? throw new ArgumentNullException(nameof(password));

        if (!EmailAddress.Contains("@"))
            throw new ArgumentException("Invalid email format", nameof(emailAddress));
    }
}

// Option 2: IValidatableObject
public sealed class BotSettings : IValidatableObject
{
    public string EmailAddress { get; set; }
    public string Password { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(EmailAddress))
            yield return new ValidationResult("EmailAddress is required", [nameof(EmailAddress)]);
        if (string.IsNullOrWhiteSpace(Password))
            yield return new ValidationResult("Password is required", [nameof(Password)]);
    }
}
```

## Reload Behaviour

### Current: No Effective Reload

```csharp
// JSON provider configured with reloadOnChange: true
.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
```

**But:**
- `BotSettings` and `DebugSettings` are bound to **plain objects**
- No `IOptionsMonitor<T>` registration
- No change callbacks registered
- **Changes to file during execution are ignored**

### To Enable Reload (Future)

```csharp
// In Program.cs
services.AddOptions<BotSettings>()
    .Bind(config.GetSection("botSettings"))
    .ValidateDataAnnotations();

// In consumer
public class BotService(
    IOptionsMonitor<BotSettings> settingsMonitor,
    ...)
{
    // settingsMonitor.CurrentValue reflects latest config
    // settingsMonitor.OnChange(...) for callbacks
}
```

## Secret Management

### Current: Plain Text File

```json
{
  "botSettings": {
    "emailAddress": "user@example.com",
    "password": "actual-password"
  }
}
```

### Risks

| Risk | Mitigation |
|------|------------|
| Accidental commit | `.gitignore` should exclude `appsettings.json` (but doesn't — uses placeholders) |
| File permissions | Operator must `chmod 600` |
| Log exposure | Password never logged (only email) |
| Memory dump | Credentials in managed heap |

### Operator Responsibilities

1. Use placeholder values in repository: `[[HONEYGAIN_EMAIL_ADDRESS]]`
2. Replace with real values in deployment
3. Restrict file permissions: `chmod 600 appsettings.json`
4. Never commit real credentials

### Future Improvements

| Approach | Packages Required |
|----------|-------------------|
| Environment variables | `Microsoft.Extensions.Configuration.EnvironmentVariables` |
| User secrets (dev) | `Microsoft.Extensions.Configuration.UserSecrets` |
| Azure Key Vault | `Azure.Extensions.AspNetCore.Configuration.Secrets` |
| HashiCorp Vault | `VaultSharp.Extensions.Configuration` |
| Encrypted file | Custom provider or `Microsoft.Extensions.Configuration.KeyPerFile` |

## Example Configurations

### Minimal (Production)

```json
{
  "botSettings": {
    "emailAddress": "user@example.com",
    "password": "secure-password"
  },
  "debugSettings": {
    "isDebugMode": false
  },
  "nuciLoggerSettings": {
    "logFilePath": "./logfile.log",
    "minimumLevel": "Info",
    "isFileOutputEnabled": true
  }
}
```

### Debug (Development)

```json
{
  "botSettings": {
    "emailAddress": "user@example.com",
    "password": "secure-password"
  },
  "debugSettings": {
    "isDebugMode": true
  },
  "nuciLoggerSettings": {
    "logFilePath": "./logfile.log",
    "minimumLevel": "Debug",
    "isFileOutputEnabled": true
  }
}
```

### No File Logging

```json
{
  "botSettings": {
    "emailAddress": "user@example.com",
    "password": "secure-password"
  },
  "debugSettings": {
    "isDebugMode": false
  },
  "nuciLoggerSettings": {
    "isFileOutputEnabled": false
  }
}
```

## Related Documentation

- [Behaviour: Inspect, Edit, Delete](./behaviour/inspect-edit-delete.md)
- [Components: Host and Composition](./components/host-and-composition.md)
- [Flows: Startup and Rendering](./flows/startup-and-rendering.md)
- [Security](./security.md)
- [Privacy](../PRIVACY.md)