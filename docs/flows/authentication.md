# Authentication Flow

## Overview

This flow covers the Honeygain login process: navigation to the login page, cookie consent handling, credential submission, and dashboard verification.

## Sequence Diagram

```
BotService.LogIn()
    │
    ▼
logger.Info(LogIn, Started, Username)
    │
    ▼
try
    │
    ▼
HoneygainProcessor.LogIn()
    │
    ├─► webProcessor.GoToUrl("https://dashboard.honeygain.com/login")
    │       │
    │       ▼
    │   Browser navigates to login page
    │       │
    │       ▼
    │   Page loads (HTML, JS, CSS)
    │
    ├─► AcceptCookiesIfNeeded()
    │       │
    │       ├─► webProcessor.WaitForElementToBeVisible(
    │       │       "//img[@alt='cookie consent']/../div/button[2]")
    │       │       │
    │       │       ▼
    │       │   Waits for cookie consent dialog (max timeout)
    │       │       │
    │       │       ▼
    │       ├─► webProcessor.IsElementVisible(cookieButtonSelector)
    │       │       │
    │       │       ├─► Visible → webProcessor.Click(cookieButtonSelector)
    │       │       │
    │       │       └─► Not visible → continue (no consent prompt)
    │       │
    │       ▼
    │   Cookie consent handled (or skipped)
    │
    ├─► webProcessor.SetText("#email", settings.EmailAddress)
    │       │
    │       ▼
    │   Email field populated
    │
    ├─► webProcessor.SetText("#password", settings.Password)
    │       │
    │       ▼
    │   Password field populated
    │
    ├─► webProcessor.Click("//form/button")
    │       │
    │       ▼
    │   Login form submitted
    │       │
    │       ▼
    │   HTTPS POST to Honeygain auth endpoint
    │       │
    │       ▼
    │   Session cookie set, redirect to dashboard
    │
    ├─► webProcessor.WaitForElementToBeVisible("#traffic-legend-container")
    │       │
    │       ▼
    │   Waits for dashboard landmark element
    │       │
    │       ▼
    │   Dashboard loaded successfully
    │
    ▼
catch (Exception ex)
    │
    ▼
logger.Error(LogIn, Failure, ex, Username)
    │
    ▼
logger.Info(LogIn, Success, Username)  ← ALWAYS LOGGED
```

## Detailed Steps

### 1. Navigate to Login Page

```csharp
webProcessor.GoToUrl(LogInUrl);  // "https://dashboard.honeygain.com/login"
```

- **Method:** `IWebProcessor.GoToUrl(string)`
- **Implementation:** Selenium `driver.Navigate().GoToUrl(url)`
- **Waits:** Page load completion (Selenium default)
- **Failure:** Timeout, DNS resolution, network error → propagates exception

### 2. Cookie Consent Handling

```csharp
private void AcceptCookiesIfNeeded()
{
    string selector = Select.ByXPath("//img[@alt='cookie consent']/../div/button[2]");
    webProcessor.WaitForElementToBeVisible(selector);
    if (webProcessor.IsElementVisible(selector))
    {
        webProcessor.Click(selector);
    }
}
```

**Selector Breakdown:**
- `//img[@alt='cookie consent']` — Find cookie consent image
- `/..` — Parent element (consent dialog container)
- `/div/button[2]` — Second button in div (typically "Accept selected" or "Accept all")

**Behaviour:**
1. Wait for consent button to be visible (timeout from SeleniumWebProcessor)
2. Check visibility again (race condition guard)
3. If visible → click
4. If not visible → assume no consent prompt (already accepted, or not shown)

**Failure Modes:**
- Consent dialog not found → `WaitForElementToBeVisible` timeout → exception
- Selector mismatch (UI change) → timeout → exception
- Click intercepted → exception

### 3. Credential Submission

```csharp
webProcessor.SetText(Select.ById("email"), settings.EmailAddress);
webProcessor.SetText(Select.ById("password"), settings.Password);
```

- **Selectors:** `#email`, `#password` (HTML `id` attributes)
- **Method:** `IWebProcessor.SetText(selector, text)`
- **Implementation:** Selenium `FindElement` → `Clear()` → `SendKeys(text)`
- **No validation** — assumes fields exist and accept input

### 4. Login Button Click

```csharp
webProcessor.Click(Select.ByXPath("//form/button"));
```

- **Selector:** First `<button>` inside `<form>`
- **Method:** `IWebProcessor.Click(selector)`
- **Implementation:** Selenium `FindElement` → `Click()`
- **Triggers:** Form submission → HTTPS POST to Honeygain auth endpoint

### 5. Dashboard Verification

```csharp
webProcessor.WaitForElementToBeVisible(Select.ById("traffic-legend-container"));
```

- **Selector:** `#traffic-legend-container` (dashboard landmark)
- **Purpose:** Confirms successful authentication and dashboard load
- **Timeout:** SeleniumWebProcessor default wait timeout
- **Failure:** Element not visible within timeout → exception → login failed

## Selectors Reference

| Step | Element | Selector | Type | Fragility |
|------|---------|----------|------|-----------|
| Navigation | Login page | `https://dashboard.honeygain.com/login` | URL | Low |
| Cookie consent | Accept button | `//img[@alt='cookie consent']/../div/button[2]` | XPath | High |
| Email field | Email input | `#email` | ID | Low |
| Password field | Password input | `#password` | ID | Low |
| Login button | Submit button | `//form/button` | XPath | Medium |
| Dashboard ready | Traffic legend | `#traffic-legend-container` | ID | Medium |

## Error Handling

### In HoneygainProcessor.LogIn()
- All exceptions propagate to caller (`BotService.LogIn()`)
- No try/catch in processor — failures are infrastructure or business logic

### In BotService.LogIn()
```csharp
try
{
    processor.LogIn();
}
catch (Exception ex)
{
    logger.Error(MyOperation.LogIn, OperationStatus.Failure, ex, logInfos);
}
logger.Info(MyOperation.LogIn, OperationStatus.Success, logInfos);
```

- **All exceptions caught** — `Exception` base type
- **Logged as Error** — `logger.Error(..., OperationStatus.Failure, ex, ...)`
- **Success logged anyway** — unconditional after catch

### Log Events

| Sequence | Level | Operation | Status | Context |
|----------|-------|-----------|--------|---------|
| 1 | Info | LogIn | Started | Username |
| 2 | Error | LogIn | Failure | Username + Exception (if failed) |
| 3 | Info | LogIn | Success | Username |

## Failure Scenarios

| Scenario | Detection | Result |
|----------|-----------|--------|
| Invalid credentials | Dashboard not loaded, `#traffic-legend-container` not visible | `WaitForElementToBeVisible` timeout → exception |
| Cookie consent changed | Consent button not found with current selector | `WaitForElementToBeVisible` timeout → exception |
| Network failure | `GoToUrl` or form submission timeout | Selenium timeout → exception |
| Selector changed | Any `FindElement` fails | `NoSuchElementException` → exception |
| Account locked/2FA | Honeygain returns error page | Dashboard landmark not found → timeout → exception |

## Timing Characteristics

| Step | Typical Duration | Variability |
|------|------------------|-------------|
| GoToUrl | 2-10s | High (network, page size) |
| Cookie consent wait | 0-5s | Medium (may not appear) |
| SetText (email) | < 100ms | Low |
| SetText (password) | < 100ms | Low |
| Click login | < 500ms | Low |
| Dashboard wait | 3-15s | High (auth, redirect, dashboard load) |
| **Total** | **5-30s** | **High** |

## Invariants

1. **Login always attempted first** — `BotService.Run()` calls `LogIn()` before other operations
2. **Credentials from settings** — `BotSettings.EmailAddress` and `Password` used directly
3. **Cookie consent optional** — handled if present, skipped if not
4. **Dashboard landmark required** — `#traffic-legend-container` visibility = success
5. **No retry** — single attempt per process invocation
6. **Password never logged** — only email in log context

## Modification Guidance

| Change | Files to Modify | Testing |
|--------|-----------------|---------|
| Update login URL | `HoneygainProcessor.LogInUrl` constant | Manual verification |
| Change email/password selectors | `HoneygainProcessor.LogIn()` inline selectors | Unit test: `GivenLogIn_WhenCalled_ThenSetsEmailAddress` |
| Change login button selector | `HoneygainProcessor.LogIn()` inline selector | Unit test: `GivenLogIn_WhenCalled_ThenClicksLogInButton` |
| Update dashboard landmark | `HoneygainProcessor.LogIn()` wait selector | Unit test: `GivenLogIn_WhenCalled_ThenWaitsForTrafficLegendContainer` |
| Modify cookie consent logic | `HoneygainProcessor.AcceptCookiesIfNeeded()` | Unit test: `GivenLogIn_WhenCalled_ThenCallsAcceptCookiesIfNeeded` |
| Add 2FA handling | New method in `IHoneygainProcessor` + `HoneygainProcessor` | New tests required |

## Related Documentation

- [Components: Integration Models](./components/integration-models.md#honeygainprocessor)
- [Components: Application Services](./components/application-services.md#botservice)
- [Error Handling](./error-handling.md)
- [Logging](./logging.md)
- [Testing](./testing.md)