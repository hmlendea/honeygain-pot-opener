# Integration Models

## Overview

The integration models layer bridges the application's orchestration logic with the Honeygain dashboard via browser automation. It consists of:

1. **`IHoneygainProcessor`** — Contract defining Honeygain-specific operations
2. **`HoneygainProcessor`** — Implementation owning URLs, selectors, and interaction logic
3. **`IWebProcessor`** — Generic browser operation contract (from NuciWeb.Automation)
4. **`SeleniumWebProcessor`** — Selenium implementation of `IWebProcessor` (from NuciWeb.Automation.Selenium)

## IHoneygainProcessor

### Location
`HoneygainPotOpener/Processors/IHoneygainProcessor.cs`

### Contract
```csharp
public interface IHoneygainProcessor
{
    void LogIn();
    void ClaimReward();
    void OpenPot();
}
```

### Purpose
- Defines the Honeygain integration boundary
- Isolates orchestration (`BotService`) from dashboard-specific logic
- Enables unit testing with mocks

## HoneygainProcessor

### Location
`HoneygainPotOpener/Processors/HoneygainProcessor.cs`

### Constructor
```csharp
public sealed class HoneygainProcessor(
    IWebProcessor webProcessor,
    BotSettings settings) : IHoneygainProcessor
```

### Constants (URLs)
```csharp
public static string HomePageUrl => "https://honeygain.com";
public static string DashboardUrl => "https://dashboard.honeygain.com";
public static string LogInUrl => $"{DashboardUrl}/login";
```

### Methods

#### LogIn()
```csharp
public void LogIn()
{
    webProcessor.GoToUrl(LogInUrl);
    AcceptCookiesIfNeeded();

    string emailAddressInputSelector = Select.ById("email");
    string passwordInputSelector = Select.ById("password");
    string logInButtonSelector = Select.ByXPath("//form/button");
    string trafficLegendContainerSelector = Select.ById("traffic-legend-container");

    webProcessor.SetText(emailAddressInputSelector, settings.EmailAddress);
    webProcessor.SetText(passwordInputSelector, settings.Password);
    webProcessor.Click(logInButtonSelector);
    webProcessor.WaitForElementToBeVisible(trafficLegendContainerSelector);
}
```

**Flow:**
1. Navigate to `https://dashboard.honeygain.com/login`
2. Accept cookie consent if present
3. Fill email field (`#email`)
4. Fill password field (`#password`)
5. Click login button (`//form/button`)
6. Wait for dashboard to load (`#traffic-legend-container` visible)

#### ClaimReward()
```csharp
public void ClaimReward()
{
    string claimRewardButtonSelector = Select.ByXPath("//span[text()='Claim reward']/../../../../../button");

    if (!webProcessor.IsElementVisible(claimRewardButtonSelector))
    {
        throw new InvalidOperationException("There is no reward available to be claimed at this time.");
    }

    webProcessor.Click(claimRewardButtonSelector);
}
```

**Flow:**
1. Check if "Claim reward" button is visible via XPath
2. If not visible → throw `InvalidOperationException`
3. If visible → click button

#### OpenPot()
```csharp
public void OpenPot()
{
    string openPotButtonSelector = Select.ByXPath("//span[text()='Open Lucky Pot']/../../..");

    webProcessor.WaitForElementToBeVisible(openPotButtonSelector);

    if (!webProcessor.IsElementVisible(openPotButtonSelector))
    {
        throw new InvalidOperationException("The Lucky Pot cannot be opened at this time.");
    }

    webProcessor.Click(openPotButtonSelector);
}
```

**Flow:**
1. Wait for "Open Lucky Pot" button to become visible
2. Check visibility again (race condition guard)
3. If not visible → throw `InvalidOperationException`
4. If visible → click button

#### AcceptCookiesIfNeeded() (Private)
```csharp
private void AcceptCookiesIfNeeded()
{
    string acceptSelectedCookiesButtonSelector = Select.ByXPath("//img[@alt='cookie consent']/../div/button[2]");

    webProcessor.WaitForElementToBeVisible(acceptSelectedCookiesButtonSelector);

    if (webProcessor.IsElementVisible(acceptSelectedCookiesButtonSelector))
    {
        webProcessor.Click(acceptSelectedCookiesButtonSelector);
    }
}
```

**Flow:**
1. Wait for cookie consent button to be visible
2. If visible → click "Accept selected" (button[2])
3. If not visible → continue (no consent prompt)

### Selectors Reference

| Operation | Element | Selector | Type | Notes |
|-----------|---------|----------|------|-------|
| LogIn | Email input | `#email` | ID | Standard HTML id |
| LogIn | Password input | `#password` | ID | Standard HTML id |
| LogIn | Login button | `//form/button` | XPath | First button in form |
| LogIn | Dashboard ready | `#traffic-legend-container` | ID | Dashboard landmark |
| LogIn | Cookie consent | `//img[@alt='cookie consent']/../div/button[2]` | XPath | Second button in consent dialog |
| ClaimReward | Claim button | `//span[text()='Claim reward']/../../../../../button` | XPath | Traverses up 5 levels to button |
| OpenPot | Open button | `//span[text()='Open Lucky Pot']/../../..` | XPath | Traverses up 3 levels |

### Selector Fragility

**All selectors are XPath or ID-based and depend on Honeygain's current DOM structure.**

| Selector | Fragility Risk | Mitigation |
|----------|----------------|------------|
| `#email`, `#password` | Low — standard form IDs | Stable unless form redesigned |
| `//form/button` | Medium — form structure may change | Could use more specific attribute |
| `#traffic-legend-container` | Medium — dashboard layout may change | Dashboard landmark |
| Cookie consent XPath | High — consent UI changes frequently | May need updates per region/regulation |
| Claim reward XPath | High — text-based, deep traversal | Very fragile; text or structure change breaks it |
| Open pot XPath | High — text-based, traversal | Very fragile; text or structure change breaks it |

### Exception Contract

| Method | Exception | Condition |
|--------|-----------|-----------|
| `LogIn` | Propagated from `IWebProcessor` | Navigation, element not found, timeout |
| `ClaimReward` | `InvalidOperationException` | Reward button not visible |
| `OpenPot` | `InvalidOperationException` | Pot button not visible after wait |

**No custom exception types** — uses standard `InvalidOperationException` for business logic failures.

### Dependencies

| Dependency | Type | Purpose |
|------------|------|---------|
| `IWebProcessor` | Injected (transient, retained) | Generic browser operations |
| `BotSettings` | Injected (singleton) | Credentials for login |

## IWebProcessor (NuciWeb.Automation)

### Source
`NuciWeb.Automation` package — not in this repository

### Contract (Inferred from Usage)
```csharp
public interface IWebProcessor
{
    void GoToUrl(string url);
    void SetText(string selector, string text);
    void Click(string selector);
    void WaitForElementToBeVisible(string selector);
    bool IsElementVisible(string selector);
}
```

### Selector Abstraction
Uses `NuciWeb.Select` factory:
- `Select.ById(string id)` → `"id:email"`
- `Select.ByXPath(string xpath)` → `"xpath://form/button"`

### Purpose
- Abstracts browser operations from Honeygain logic
- Enables substitution of browser automation implementation
- Provides consistent wait/visibility semantics

## SeleniumWebProcessor (NuciWeb.Automation.Selenium)

### Source
`NuciWeb.Automation.Selenium` package — not in this repository

### Implementation Details (Inferred)
- Wraps `OpenQA.Selenium.IWebDriver`
- Implements `IWebProcessor` using Selenium commands
- Uses `WebDriverWait` for `WaitForElementToBeVisible`
- Uses `FindElement` for `IsElementVisible`

### Integration Boundary
```
HoneygainProcessor (this repo)
    │
    ▼ depends on
IWebProcessor (NuciWeb.Automation contract)
    │
    ▼ implemented by
SeleniumWebProcessor (NuciWeb.Automation.Selenium package)
    │
    ▼ wraps
IWebDriver (Selenium)
    │
    ▼ controls
Browser (Chrome/Firefox/Edge)
```

## WebDriver Initialisation

### Location
`NuciWeb.Automation.Selenium.WebDriverInitialiser` (external package)

### Called From
`Program.cs`:
```csharp
IWebDriver webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode);
```

### Behaviour
- Auto-discovers available browser and WebDriver
- `IsDebugMode = true` → visible browser window
- `IsDebugMode = false` → headless mode
- Returns configured `IWebDriver` instance

## Testing

### HoneygainProcessorTests
**File:** `HoneygainPotOpener.UnitTests/Processors/HoneygainProcessorTests.cs`

**Test Strategy:**
- Mock `IWebProcessor` with Moq
- Verify correct selectors passed to web processor
- Verify method call sequence
- Test exception throwing for invisible elements

**Key Tests:**
- `GivenLogIn_WhenCalled_ThenCallsGoToUrlWithLogInUrl`
- `GivenLogIn_WhenCalled_ThenCallsAcceptCookiesIfNeeded`
- `GivenLogIn_WhenCalled_ThenSetsEmailAddress`
- `GivenLogIn_WhenCalled_ThenSetsPassword`
- `GivenLogIn_WhenCalled_ThenClicksLogInButton`
- `GivenLogIn_WhenCalled_ThenWaitsForTrafficLegendContainer`
- `GivenClaimReward_WhenButtonNotVisible_ThenThrowsInvalidOperationException`
- `GivenClaimReward_WhenButtonVisible_ThenClicksButton`
- `GivenOpenPot_WhenButtonVisible_ThenWaitsForButtonToBeVisible`
- `GivenOpenPot_WhenButtonNotVisible_ThenThrowsInvalidOperationException`
- `GivenOpenPot_WhenButtonVisible_ThenClicksButton`

### Selector Verification Pattern
```csharp
mockWebProcessor.Verify(wp => wp.SetText(
    It.Is<string>(s => s.Contains("email")),
    botSettings.EmailAddress), Times.Once);
```

Uses `It.Is<string>(s => s.Contains(...))` to match selector strings without exact XPath/ID coupling.

## Modification Guidance

### Adding a New Honeygain Operation
1. Add method to `IHoneygainProcessor`
2. Implement in `HoneygainProcessor` with new selectors
3. Add method to `BotService` with logging pattern
4. Call from `BotService.Run()` in desired order
5. Add tests in `HoneygainProcessorTests`

### Updating Selectors (Honeygain UI Change)
1. Identify new selector in browser dev tools
2. Update constant/inline selector in `HoneygainProcessor`
3. Update corresponding tests
4. Verify with manual run

### Replacing Browser Automation
1. Implement `IWebProcessor` with new backend (e.g., Playwright)
2. Register in `Program.cs`: `.AddTransient<IWebProcessor, NewProcessor>()`
3. No changes to `HoneygainProcessor` or `BotService` required

### Adding Wait/Timeout Configuration
Currently hardcoded in `SeleniumWebProcessor`. To make configurable:
1. Add settings to `DebugSettings` or new `BrowserSettings`
2. Pass to `SeleniumWebProcessor` via constructor
3. Update `WebDriverInitialiser` or processor to use timeouts

## Invariants

1. **All Honeygain URLs are constants** in `HoneygainProcessor`
2. **All selectors are defined inline** in method bodies (not constants)
3. **No Selenium types in `HoneygainProcessor`** — only `IWebProcessor` and `Select`
4. **Business logic exceptions are `InvalidOperationException`**
5. **Infrastructure exceptions propagate from `IWebProcessor`**
6. **Credentials only used in `LogIn()`** — not stored in processor

## Related Documentation

- [Architecture (Detailed)](./architecture.md#honeygainprocessor-integration-adapter)
- [Components: Application Services](./components/application-services.md)
- [Flows: Authentication](./flows/authentication.md)
- [Flows: Reward Claim](./flows/reward-claim.md)
- [Flows: Lucky Pot](./flows/lucky-pot.md)
- [Error Handling](./error-handling.md)
- [Testing](./testing.md)