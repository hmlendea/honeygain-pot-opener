# Data Model

## Overview

This application has **no persistent data model**. It is a stateless automation tool that:
- Reads configuration at startup
- Performs browser automation
- Logs structured events
- Exits

No database, no file storage (except logs), no cache, no local state.

## In-Memory Objects

### Configuration Objects (Bound at Startup)

#### BotSettings

```csharp
public sealed class BotSettings
{
    public string EmailAddress { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public bool HasValidCredentials
        => !string.IsNullOrWhiteSpace(EmailAddress) && !string.IsNullOrWhiteSpace(Password);
}
```

| Property | Type | Default | Source | Used By |
|----------|------|---------|--------|---------|
| `EmailAddress` | string | `string.Empty` | `appsettings.json:botSettings.emailAddress` | `HoneygainProcessor.LogIn()` |
| `Password` | string | `string.Empty` | `appsettings.json:botSettings.password` | `HoneygainProcessor.LogIn()` |
| `HasValidCredentials` | bool (computed) | Derived | `Program.Main` startup validation |

**Lifecycle:** Created → Bound → Registered as singleton → Injected → Process exit

#### DebugSettings

```csharp
public sealed class DebugSettings
{
    public bool IsDebugMode { get; set; }
    public bool IsHeadless => !IsDebugMode;
}
```

| Property | Type | Default | Source | Used By |
|----------|------|---------|--------|---------|
| `IsDebugMode` | bool | `false` | `appsettings.json:debugSettings.isDebugMode` | `WebDriverInitialiser.InitialiseAvailableWebDriver(isDebugMode)` |
| `IsHeadless` | bool (computed) | `true` | Derived | Not consumed by the entry point; available for consumers that prefer the inverted flag |

**Lifecycle:** Same as BotSettings

### Logging Extensions (Static)

#### MyOperation

```csharp
public static class MyOperation
{
    public static readonly Operation LogIn = new("LogIn");
    public static readonly Operation ClaimReward = new("ClaimReward");
    public static readonly Operation OpenPot = new("OpenPot");
}
```

| Constant | Type | Value | Used In |
|----------|------|-------|---------|
| `LogIn` | `Operation` | "LogIn" | `BotService.LogIn()` |
| `ClaimReward` | `Operation` | "ClaimReward" | `BotService.ClaimReward()` |
| `OpenPot` | `Operation` | "OpenPot" | `BotService.OpenPot()` |

#### MyLogInfoKey

```csharp
public static class MyLogInfoKey
{
    public static readonly LogInfoKey Username = new("Username");
}
```

| Constant | Type | Value | Used In |
|----------|------|-------|---------|
| `Username` | `LogInfoKey` | "Username" | All `BotService` operation methods |

### Selector Properties (HoneygainProcessor)

```csharp
public static string HomePageUrl => "https://honeygain.com";
public static string DashboardUrl => "https://dashboard.honeygain.com";
public static string LogInUrl => $"{DashboardUrl}/login";

private static string EmailAddressInputSelector => Select.ById("email");
private static string PasswordInputSelector => Select.ById("password");
private static string LogInButtonSelector => Select.ByXPath("//form/button");
private static string DashboardReadySelector => Select.ById("traffic-legend-container");
private static string CookiesConsentButtonSelector => Select.ByXPath("//img[@alt='cookie consent']/../div/button[2]");
private static string ClaimRewardButtonSelector => Select.ByXPath("//span[text()='Claim reward']/../../../../../button");
private static string OpenPotButtonSelector => Select.ByXPath("//span[text()='Open Lucky Pot']/../../..");
```

| Property | Selector Type | Target |
|----------|---------------|--------|
| `HomePageUrl` | URL | Marketing home page |
| `DashboardUrl` | URL | Dashboard landing page |
| `LogInUrl` | URL | Login page |
| `EmailAddressInputSelector` | ID | Email field |
| `PasswordInputSelector` | ID | Password field |
| `LogInButtonSelector` | XPath | Login button |
| `DashboardReadySelector` | ID | Dashboard landmark |
| `CookiesConsentButtonSelector` | XPath | Cookie consent acceptance |
| `ClaimRewardButtonSelector` | XPath | Claim reward button |
| `OpenPotButtonSelector` | XPath | Open Lucky Pot button |

## External Data (Honeygain Web UI)

### Pages

| Page | URL | Purpose |
|------|-----|---------|
| Login | `https://dashboard.honeygain.com/login` | Authentication |
| Dashboard | `https://dashboard.honeygain.com/` | Post-login landing |
| Earnings | `https://dashboard.honeygain.com/earnings` | Reward claim, Lucky Pot |

### HTML Elements (Targeted)

| Element | Selector | Action | Page |
|---------|----------|--------|------|
| Cookie consent button | `//img[@alt='cookie consent']/../div/button[2]` | Wait visible / IsVisible / Click | Login |
| Email input | `#email` | SetText | Login |
| Password input | `#password` | SetText | Login |
| Login button | `//form/button` | Click | Login |
| Dashboard landmark | `#traffic-legend-container` | Wait visible | Dashboard |
| Claim reward button | `//span[text()='Claim reward']/../../../../../button` | IsVisible / Click | Dashboard |
| Open Lucky Pot button | `//span[text()='Open Lucky Pot']/../../..` | Wait visible / IsVisible / Click | Dashboard |

## Log Data Model

### Structured Log Entry (NuciLog)

```csharp
// Conceptual structure
{
    Timestamp: DateTimeOffset,
    Level: "Info" | "Warning" | "Error" | "Fatal",
    Operation: "StartUp" | "LogIn" | "ClaimReward" | "OpenPot" | "Unknown",
    Status: "Started" | "Success" | "Failure",  // Success or Failure, never both for one operation
    Message: string,
    Exception: Exception?,  // when present
    LogInfo: [
        { Key: "Username", Value: "user@example.com" }
    ]
}
```

### LogInfo Records

| Key | Type | Value Source | Present In |
|-----|------|--------------|------------|
| `Username` | string | `BotSettings.EmailAddress` | All operation logs |

## No Persistent State

| State Type | Present | Notes |
|------------|---------|-------|
| Database | No | — |
| File storage (non-log) | No | — |
| Cache | No | — |
| Session persistence | No | Browser session ends on `Quit()` |
| User preferences | No | Only config file |
| Credentials storage | No | Only in memory during run |

## Data Flow

```
appsettings.json
       │
       ▼
ConfigurationBuilder.Bind()
       │
       ├─► BotSettings (EmailAddress, Password)
       └─► DebugSettings (IsDebugMode)
              │
              ▼
       DI Container (Singleton instances)
              │
              ▼
    BotService.Run()
              │
              ├─► HoneygainProcessor.LogIn()
              │       └─► IWebProcessor (Selenium) → Honeygain Web UI
              ├─► HoneygainProcessor.ClaimReward()
              │       └─► IWebProcessor → Honeygain Web UI
              └─► HoneygainProcessor.OpenPot()
                      └─► IWebProcessor → Honeygain Web UI
              │
              ▼
       ILogger (NuciLog) → logfile.log
              │
              ▼
       Process Exit (no state retained)
```

## Validation Rules

### Current: Startup Credential Check

- No data annotations and no `IValidatableObject`
- `BotSettings.HasValidCredentials` is evaluated in `Program.Main` before a browser is created
- Value-specific validation (for example, address format) still occurs at the point of use

### Validation

| Field | Validation | Failure Mode |
|-------|------------|--------------|
| `EmailAddress` | Non-blank (startup); address accepted by Honeygain (at use) | Missing → exit code 1 before browser; otherwise login fails |
| `Password` | Non-blank (startup); accepted by Honeygain (at use) | Missing → exit code 1 before browser; otherwise login fails |
| `IsDebugMode` | Boolean | Defaults to `false` |

## Serialization

### Configuration (JSON)

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
    "minimumLevel": "string",
    "isFileOutputEnabled": "boolean"
  }
}
```

**Binder:** `Microsoft.Extensions.Configuration.Binder` (reflection-based)

### Logs (NuciLog Format)

Proprietary structured format (not JSON). Contains timestamp, level, operation, status, message, exception, log info array.

## Related Documentation

- [Configuration](./configuration.md)
- [Logging](./logging.md)
- [Components: Integration Models](./components/integration-models.md)
- [Flows: Authentication](./flows/authentication.md)
- [Security](./security.md)