# API Reference Index

## Overview

This application is a **console application** with **no HTTP API, no controllers, no public endpoints**. The "API" consists of internal service contracts (interfaces) used for dependency injection and testing.

## Internal Service Contracts

### IBotService

**Assembly:** HoneygainPotOpener
**Namespace:** HoneygainPotOpener.Service
**Lifetime:** Singleton

```csharp
public interface IBotService
{
    void Run();
}
```

**Implementation:** `BotService`

**Behaviour:** Orchestrates the three operations in fixed sequence: `LogIn()` → `ClaimReward()` → `OpenPot()`. Each operation logs Started, catches exceptions, logs Failure/Success, then logs Success unconditionally.

**See:** [Components: Application Services](../components/application-services.md#ibotservice), [Flows: Startup and Rendering](../flows/startup-and-rendering.md)

---

### IHoneygainProcessor

**Assembly:** HoneygainPotOpener
**Namespace:** HoneygainPotOpener.Processors
**Lifetime:** Transient

```csharp
public interface IHoneygainProcessor
{
    void LogIn();
    void ClaimReward();
    void OpenPot();
}
```

**Implementation:** `HoneygainProcessor`

**Behaviour:** Integration adapter for Honeygain web UI. Owns all URLs and selectors. Uses `IWebProcessor` for browser automation.

**See:** [Components: Integration Models](../components/integration-models.md#ihoneygainprocessor), [Flows: Authentication](../flows/authentication.md), [Flows: Reward Claim](../flows/reward-claim.md), [Flows: Lucky Pot](../flows/lucky-pot.md)

---

### IWebProcessor

**Assembly:** NuciWeb.Automation (NuciWeb.Automation 1.0.0)
**Namespace:** NuciWeb.Automation
**Lifetime:** Singleton (registered in Program.cs)

```csharp
public interface IWebProcessor
{
    void GoToUrl(string url);
    void WaitForElementToBeVisible(By by, int timeoutSeconds = 10);
    bool IsElementVisible(By by);
    void SetText(By by, string text);
    void Click(By by);
    void DoubleClick(By by);
    void Quit();
}
```

**Implementation:** `NuciWeb.Automation.Selenium.SeleniumWebProcessor` (via NuciWeb.Automation.Selenium 1.0.2)

**Behaviour:** Abstraction over Selenium WebDriver. Provides high-level browser operations with implicit waits.

**See:** [Components: Integration Models](../components/integration-models.md#iwebprocessor), [Dependencies](../dependencies.md#nuciwebautomation)

---

## Configuration Models

### BotSettings

**Assembly:** HoneygainPotOpener
**Namespace:** HoneygainPotOpener.Configuration

```csharp
public sealed class BotSettings
{
    public string EmailAddress { get; set; }
    public string Password { get; set; }
}
```

**Configuration Section:** `botSettings`
**Binding:** One-time at startup, registered as singleton instance

**See:** [Configuration](../configuration.md#botsettings)

---

### DebugSettings

**Assembly:** HoneygainPotOpener
**Namespace:** HoneygainPotOpener.Configuration

```csharp
public sealed class DebugSettings
{
    public bool IsDebugMode { get; set; }
    public bool IsHeadless => !IsDebugMode;
}
```

**Configuration Section:** `debugSettings`
**Binding:** One-time at startup, registered as singleton instance

**See:** [Configuration](../configuration.md#debugsettings)

---

## Logging Extensions

### MyOperation

**Assembly:** HoneygainPotOpener
**Namespace:** HoneygainPotOpener.Logging

```csharp
public static class MyOperation
{
    public static readonly Operation LogIn = new("LogIn");
    public static readonly Operation ClaimReward = new("ClaimReward");
    public static readonly Operation OpenPot = new("OpenPot");
}
```

**Base:** `NuciLog.Core.Operation`
**Usage:** Passed to `ILogger.Info/Error/Warn/Fatal` to categorise log entries

**See:** [Logging](../logging.md#structured-fields)

---

### MyLogInfoKey

**Assembly:** HoneygainPotOpener
**Namespace:** HoneygainPotOpener.Logging

```csharp
public static class MyLogInfoKey
{
    public static readonly LogInfoKey Username = new("Username");
}
```

**Base:** `NuciLog.Core.LogInfoKey`
**Usage:** Key for structured log field containing Honeygain email address

**See:** [Logging](../logging.md#structured-fields)

---

## External Dependencies (NuGet)

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Extensions.Configuration` | 9.0.0 | Configuration loading |
| `Microsoft.Extensions.Configuration.Json` | 9.0.0 | JSON config provider |
| `Microsoft.Extensions.DependencyInjection` | 9.0.0 | DI container |
| `NuciLog` | 1.2.1 | Structured logging facade |
| `NuciLog.Core` | 3.1.0 | Logging abstractions (Operation, LogInfoKey) |
| `NuciWeb` | 4.0.0 | Web automation abstractions |
| `NuciWeb.Automation` | 1.0.0 | IWebProcessor interface |
| `NuciWeb.Automation.Selenium` | 1.0.2 | Selenium implementation of IWebProcessor |
| `Selenium.WebDriver` | (transitive) | Browser automation |
| `NUnit` | 5.0.0 | Unit testing framework |
| `Moq` | 4.21.0 | Mocking framework |

**See:** [Dependencies](../dependencies.md)

---

## No Public API

| API Type | Present |
|----------|---------|
| REST endpoints | No |
| GraphQL | No |
| gRPC | No |
| SignalR | No |
| Webhooks | No |
| CLI arguments | No |
| Plugin system | No |

---

## Testing Contracts (Test-Only)

### Mock<IWebProcessor>

**Used in:** `HoneygainProcessorTests`, `BotServiceTests`

**Verified Interactions:**
- `GoToUrl("https://dashboard.honeygain.com/login")`
- `WaitForElementToBeVisible(By.Id("onetrust-accept-btn-handler"), 5)`
- `SetText(By.Id("email"), email)`
- `SetText(By.Id("password"), password)`
- `Click(By.Id("login-btn"))`
- `WaitForElementToBeVisible(By.XPath("//a[contains(@href, '/earnings')]"), 10)`
- `IsElementVisible(By.XPath("//button[contains(text(), 'Claim reward')]"))`
- `Click(By.XPath("//button[contains(text(), 'Claim reward')]"))`
- `WaitForElementToBeVisible(By.XPath("//button[contains(text(), 'Open Lucky Pot')]"), 10)`
- `IsElementVisible(By.XPath("//button[contains(text(), 'Open Lucky Pot')]"))`
- `DoubleClick(By.XPath("//button[contains(text(), 'Open Lucky Pot')]"))`
- `Quit()`

### Mock<IHoneygainProcessor>

**Used in:** `BotServiceTests`

**Verified Interactions:**
- `LogIn()`
- `ClaimReward()`
- `OpenPot()`

**See:** [Testing](../testing.md)

---

## Related Documentation

- [Components: Application Services](../components/application-services.md)
- [Components: Integration Models](../components/integration-models.md)
- [Dependencies](../dependencies.md)
- [Testing](../testing.md)