# Browse and Search Behaviour

## Overview

This document describes the browser navigation, element location, and interaction patterns used throughout the application. All browser operations are mediated through the `IWebProcessor` abstraction.

## Navigation

### GoToUrl

**Contract:** `void IWebProcessor.GoToUrl(string url)`

**Usage:**
```csharp
webProcessor.GoToUrl(HoneygainProcessor.LogInUrl);
```

**Behaviour:**
- Navigates browser to specified URL
- Waits for page load completion (Selenium default)
- No explicit timeout configuration in this application

**Failure Modes:**
- DNS resolution failure
- Connection timeout
- HTTP error status (4xx, 5xx) — may not throw depending on Selenium config
- Page load timeout

## Element Location

### Selector Factory

All selectors are created via `NuciWeb.Select` factory:

```csharp
// ID selector
Select.ById("email")           // → "id:email"

// XPath selector
Select.ByXPath("//form/button") // → "xpath://form/button"
```

**Selector Format:** `"type:value"` where type is `id` or `xpath`

### Selector Patterns Used

| Pattern | Example | Usage |
|---------|---------|-------|
| ID | `#email`, `#password`, `#traffic-legend-container` | Stable form fields, dashboard landmarks |
| XPath (text-based) | `//span[text()='Claim reward']` | Dynamic buttons with visible text |
| XPath (structural) | `//form/button` | Form submission buttons |
| XPath (relative) | `//img[@alt='cookie consent']/../div/button[2]` | Cookie consent dialog |

## Element Interaction

### SetText

**Contract:** `void IWebProcessor.SetText(string selector, string text)`

**Usage:**
```csharp
webProcessor.SetText(Select.ById("email"), settings.EmailAddress);
```

**Behaviour:**
1. Locate element by selector
2. Clear existing content
3. Send keys (text)
4. No verification of input acceptance

**Failure Modes:**
- Element not found → `NoSuchElementException`
- Element not interactable (hidden, disabled) → `ElementNotInteractableException`
- Stale element reference → `StaleElementReferenceException`

### Click

**Contract:** `void IWebProcessor.Click(string selector)`

**Usage:**
```csharp
webProcessor.Click(Select.ByXPath("//form/button"));
```

**Behaviour:**
1. Locate element by selector
2. Click element
3. No wait for navigation or AJAX completion

**Failure Modes:**
- Element not found → `NoSuchElementException`
- Element not clickable (obscured, disabled) → `ElementClickInterceptedException`
- Stale element reference → `StaleElementReferenceException`
- Click causes navigation → may need explicit wait after

### WaitForElementToBeVisible

**Contract:** `void IWebProcessor.WaitForElementToBeVisible(string selector)`

**Usage:**
```csharp
webProcessor.WaitForElementToBeVisible(Select.ById("traffic-legend-container"));
```

**Behaviour:**
1. Poll for element visibility
2. Return when element is visible
3. Timeout after configured duration (SeleniumWebProcessor default)

**Failure Modes:**
- Timeout → `WebDriverTimeoutException`
- Element never appears in DOM
- Element appears but never becomes visible (hidden, zero size)

### IsElementVisible

**Contract:** `bool IWebProcessor.IsElementVisible(string selector)`

**Usage:**
```csharp
if (!webProcessor.IsElementVisible(selector))
{
    throw new InvalidOperationException("...");
}
```

**Behaviour:**
1. Attempt to locate element
2. Return `Displayed` property (Selenium)
3. Return `false` if element not found (no exception)

**Failure Modes:**
- Returns `false` for: not found, hidden, zero size, obscured
- Does not throw for missing element

## Interaction Patterns

### Pattern 1: Navigate → Wait → Interact

**Used in:** `LogIn()` for dashboard verification

```csharp
webProcessor.GoToUrl(url);
webProcessor.WaitForElementToBeVisible(landmarkSelector);
// Page ready
```

### Pattern 2: Conditional Interaction

**Used in:** `AcceptCookiesIfNeeded()`

```csharp
webProcessor.WaitForElementToBeVisible(selector);
if (webProcessor.IsElementVisible(selector))
{
    webProcessor.Click(selector);
}
```

### Pattern 3: Immediate Visibility Check

**Used in:** `ClaimReward()`

```csharp
if (!webProcessor.IsElementVisible(selector))
{
    throw new InvalidOperationException("...");
}
webProcessor.Click(selector);
```

### Pattern 4: Wait → Verify → Click

**Used in:** `OpenPot()`

```csharp
webProcessor.WaitForElementToBeVisible(selector);
if (!webProcessor.IsElementVisible(selector))
{
    throw new InvalidOperationException("...");
}
webProcessor.Click(selector);
```

## Timing and Timeouts

### Current Configuration

All timeouts are **defaults from `SeleniumWebProcessor`** (NuciWeb.Automation.Selenium package). No application-level timeout configuration exists.

### Typical Defaults (Selenium)

| Operation | Default Timeout |
|-----------|-----------------|
| Page load | 300s (Selenium default) |
| Implicit wait | 0s (not used) |
| Explicit wait (`WaitForElementToBeVisible`) | 10-30s (package default) |
| Element interaction | No timeout (immediate) |

### Implications

- **No timeout tuning** — cannot adjust for slow networks or heavy pages
- **Fixed wait in OpenPot** — ~30s timeout when pot not available
- **No retry** — single attempt per operation

## Error Handling Patterns

### Infrastructure Exceptions (Propagate)

All `IWebProcessor` methods can throw:
- `NoSuchElementException` — element not found
- `ElementNotInteractableException` — element not clickable/editable
- `ElementClickInterceptedException` — click blocked
- `StaleElementReferenceException` — DOM changed
- `WebDriverTimeoutException` — wait timeout
- `WebDriverException` — base Selenium exception

**Handling:** Propagated to `BotService` → caught → logged → continue

### Business Logic Exceptions (Thrown by Processor)

- `InvalidOperationException` — "no reward available", "pot cannot be opened"

**Handling:** Same as infrastructure — caught by `BotService` → logged → continue

## Selector Maintenance

### Current Selectors (Fragility Assessment)

| Selector | Location | Fragility | Maintenance Trigger |
|----------|----------|-----------|---------------------|
| `#email` | LogIn | Low | Form redesign |
| `#password` | LogIn | Low | Form redesign |
| `//form/button` | LogIn | Medium | Form structure change |
| `#traffic-legend-container` | LogIn | Medium | Dashboard redesign |
| Cookie consent XPath | LogIn | High | Consent UI change, regulation |
| Claim reward XPath | ClaimReward | **Very High** | Text change, DOM restructure |
| Open pot XPath | OpenPot | **Very High** | Text change, DOM restructure |

### Selector Update Process

1. **Identify new selector** — Browser dev tools on Honeygain dashboard
2. **Update inline selector** — In `HoneygainProcessor` method body
3. **Update tests** — Modify `HoneygainProcessorTests` selector matching
4. **Verify manually** — Run application with real credentials

### Recommendation for Future

Consider extracting selectors to constants or configuration for easier maintenance:
```csharp
// Current: inline
string selector = Select.ByXPath("//span[text()='Claim reward']/../../../../../button");

// Potential: constant
private const string ClaimRewardButtonXPath = "//span[text()='Claim reward']/../../../../../button";
```

## Related Documentation

- [Components: Integration Models](./components/integration-models.md)
- [Flows: Authentication](./flows/authentication.md)
- [Flows: Reward Claim](./flows/reward-claim.md)
- [Flows: Lucky Pot](./flows/lucky-pot.md)
- [Error Handling](./error-handling.md)