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
    public string EmailAddress { get; set; }
    public string Password { get; set; }
}
```

| Property | Type | Nullable | Source | Used By |
|----------|------|----------|--------|---------|
| `EmailAddress` | string | Yes | `appsettings.json:botSettings.emailAddress` | `HoneygainProcessor.LogIn()` |
| `Password` | string | Yes | `appsettings.json:botSettings.password` | `HoneygainProcessor.LogIn()` |

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
| `IsDebugMode` | bool | `false` | `appsettings.json:debugSettings.isDebugMode` | `WebDriverInitialiser.Initialise()` |
| `IsHeadless` | bool (computed) | `true` | Derived | `WebDriverInitialiser.Initialise()` |

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

### Selector Constants (HoneygainProcessor)

```csharp
private const string LoginUrl = "https://dashboard.honeygain.com/login";
private const string CookieAcceptButtonId = "onetrust-accept-btn-handler";
private const string EmailInputId = "email";
private const string PasswordInputId = "password";
private const string LoginButtonId = "login-btn";
private const string DashboardIndicatorXPath = "//a[contains(@href, '/earnings')]";
private const string ClaimRewardButtonXPath = "//button[contains(text(), 'Claim reward')]";
private const string OpenLuckyPotButtonXPath = "//button[contains(text(), 'Open Lucky Pot')]";
```

| Constant | Type | Selector Type | Target |
|----------|------|---------------|--------|
| `LoginUrl` | string | URL | Login page |
| `CookieAcceptButtonId` | string | ID | Cookie banner |
| `EmailInputId` | string | ID | Email field |
| `PasswordInputId` | string | ID | Password field |
| `LoginButtonId` | string | ID | Login button |
| `DashboardIndicatorXPath` | string | XPath | Earnings link (logged-in check) |
| `ClaimRewardButtonXPath` | string | XPath | Claim reward button |
| `OpenLuckyPotButtonXPath` | string | XPath | Open Lucky Pot button |

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
| Cookie accept button | `#onetrust-accept-btn-handler` | Click | Login |
| Email input | `#email` | SetText | Login |
| Password input | `#password` | SetText | Login |
| Login button | `#login-btn` | Click | Login |
| Earnings link | `//a[contains(@href, '/earnings')]` | Wait visible | Dashboard |
| Claim reward button | `//button[contains(text(), 'Claim reward')]` | Click / IsVisible | Earnings |
| Open Lucky Pot button | `//button[contains(text(), 'Open Lucky Pot')]` | DoubleClick / IsVisible | Earnings |

## Log Data Model

### Structured Log Entry (NuciLog)

```csharp
// Conceptual structure
{
    Timestamp: DateTimeOffset,
    Level: "Info" | "Warning" | "Error" | "Fatal",
    Operation: "StartUp" | "LogIn" | "ClaimReward" | "OpenPot" | "Unknown",
    Status: "Started" | "Success" | "Failure",
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

### Current: None at Model Level

- No data annotations
- No `IValidatableObject`
- Validation occurs at **point of use** (Selenium throws on null/empty)

### Implicit Validation

| Field | Validation | Failure Mode |
|-------|------------|--------------|
| `EmailAddress` | Non-null, non-empty, contains "@" | `SetText` throws or login fails |
| `Password` | Non-null, non-empty | `SetText` throws or login fails |
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