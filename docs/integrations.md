# Integrations

## External Integrations

### Honeygain Web Dashboard

| Aspect | Detail |
|--------|--------|
| **Type** | Web UI automation (Selenium) |
| **Base URL** | `https://dashboard.honeygain.com` |
| **Authentication** | Email/password form |
| **Endpoints Used** | `/login`, `/`, `/earnings` |
| **Protocol** | HTTPS (browser) |
| **Rate Limits** | Not documented (human-like usage) |
| **API** | None (UI scraping) |

**Integration Points:**
1. Login page → form submission
2. Dashboard → navigation to earnings
3. Earnings page → claim reward button, open lucky pot button

**Fragility:** High — depends on HTML structure, text content, XPath selectors

### NuciLog / NuciLog.Core

| Aspect | Detail |
|--------|--------|
| **Type** | Logging library |
| **Packages** | `NuciLog` 1.2.1, `NuciLog.Core` 3.1.0 |
| **Integration** | `services.AddNuciLoggerSettings()` + `AddSingleton<ILogger, NuciLogger>()` |
| **Configuration** | `nuciLoggerSettings` section in appsettings.json |
| **Output** | File (`logfile.log`) + Console |

### NuciWeb / NuciWeb.Automation / NuciWeb.Automation.Selenium

| Aspect | Detail |
|--------|--------|
| **Type** | Browser automation abstraction |
| **Packages** | `NuciWeb` 4.0.0, `NuciWeb.Automation` 1.0.0, `NuciWeb.Automation.Selenium` 1.0.2 |
| **Integration** | `IWebProcessor` interface implemented by `SeleniumWebProcessor` |
| **WebDriver** | Auto-initialised via `WebDriverInitialiser.Initialise(headless)` |
| **Browser** | Chrome/Chromium (auto-detected) |

### Selenium WebDriver

| Aspect | Detail |
|--------|--------|
| **Type** | Browser automation driver |
| **Version** | Transitive via NuciWeb.Automation.Selenium |
| **Browser** | Chrome/Chromium |
| **Management** | `WebDriverInitialiser` handles driver download/version matching |

### Microsoft.Extensions.*

| Package | Version | Integration |
|---------|---------|-------------|
| `Microsoft.Extensions.Configuration` | 9.0.0 | `ConfigurationBuilder` + JSON provider |
| `Microsoft.Extensions.Configuration.Json` | 9.0.0 | `AddJsonFile("appsettings.json")` |
| `Microsoft.Extensions.DependencyInjection` | 9.0.0 | `ServiceCollection` for DI |

## Internal Integrations (Service Contracts)

### IBotService → BotService

| Method | Called By | Calls |
|--------|-----------|-------|
| `Run()` | `Program.Main` | `LogIn()`, `ClaimReward()`, `OpenPot()` |
| `LogIn()` | `Run()` | `IHoneygainProcessor.LogIn()` |
| `ClaimReward()` | `Run()` | `IHoneygainProcessor.ClaimReward()` |
| `OpenPot()` | `Run()` | `IHoneygainProcessor.OpenPot()` |

### IHoneygainProcessor → HoneygainProcessor

| Method | Called By | Calls (IWebProcessor) |
|--------|-----------|----------------------|
| `LogIn()` | `BotService.LogIn()` | `GoToUrl`, `WaitForElementToBeVisible`, `SetText`, `Click` |
| `ClaimReward()` | `BotService.ClaimReward()` | `IsElementVisible`, `Click` |
| `OpenPot()` | `BotService.OpenPot()` | `WaitForElementToBeVisible`, `IsElementVisible`, `DoubleClick` |

### IWebProcessor → SeleniumWebProcessor

| Method | Called By | Selenium Equivalent |
|--------|-----------|---------------------|
| `GoToUrl(url)` | `HoneygainProcessor.LogIn()` | `driver.Navigate().GoToUrl(url)` |
| `WaitForElementToBeVisible(by, timeout)` | `LogIn`, `OpenPot` | `WebDriverWait` + `ExpectedConditions` |
| `IsElementVisible(by)` | `ClaimReward`, `OpenPot` | `FindElement` + `Displayed` |
| `SetText(by, text)` | `LogIn` | `FindElement` + `Clear` + `SendKeys` |
| `Click(by)` | `LogIn`, `ClaimReward` | `FindElement` + `Click` |
| `DoubleClick(by)` | `OpenPot` | `Actions` + `DoubleClick` |
| `Quit()` | `Program.Main` finally | `driver.Quit()` |

## Integration Patterns

### Adapter Pattern

`HoneygainProcessor` adapts `IWebProcessor` (generic browser automation) to Honeygain-specific operations.

```
IBotService (orchestration)
    │
    ▼
IHoneygainProcessor (domain operations)
    │
    ▼
IWebProcessor (browser abstraction)
    │
    ▼
SeleniumWebProcessor (Selenium implementation)
    │
    ▼
ChromeDriver (browser)
```

### Dependency Injection

All integrations wired in `Program.cs`:

```csharp
// Configuration
services.AddSingleton(botSettings);
services.AddSingleton(debugSettings);

// Logging
services.AddNuciLoggerSettings(config)
    .AddSingleton<ILogger, NuciLogger>();

// Web automation
services.AddSingleton<IWebProcessor>(sp => {
    var driver = WebDriverInitialiser.Initialise(debugSettings.IsHeadless);
    return new SeleniumWebProcessor(driver);
});

// Domain
services.AddTransient<IHoneygainProcessor, HoneygainProcessor>();
services.AddSingleton<IBotService, BotService>();
```

## No Other Integrations

| Integration Type | Present |
|------------------|---------|
| REST API client | No |
| Database | No |
| Message queue | No |
| Cache (Redis, etc.) | No |
| Authentication provider (OAuth, OIDC) | No |
| Metrics (Prometheus, etc.) | No |
| Tracing (OpenTelemetry, etc.) | No |
| Feature flags | No |
| Email/SMS | No |
| File storage (S3, etc.) | No |

## Future Integration Points

| Integration | Purpose | Effort |
|-------------|---------|--------|
| Honeygain API (if published) | Replace UI automation | High |
| Secret manager (Key Vault, Vault) | Secure credentials | Medium |
| Metrics endpoint | Observability | Low |
| Health check endpoint | Kubernetes liveness | Low |
| Multiple account support | Scale to multiple users | Medium |
| Notification (email, Slack, etc.) | Alert on failures | Low |

## Related Documentation

- [Components: Integration Models](./components/integration-models.md)
- [Dependencies](./dependencies.md)
- [Security](./security.md)
- [Flows: Authentication](./flows/authentication.md)
- [Flows: Reward Claim](./flows/reward-claim.md)
- [Flows: Lucky Pot](./flows/lucky-pot.md)