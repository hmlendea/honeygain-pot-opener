# Lucky Pot Flow

## Overview

This flow covers the Lucky Pot opening process: waiting for the "Open Lucky Pot" button to become visible, verifying visibility, and clicking it.

## Sequence Diagram

```
BotService.OpenPot()
    │
    ▼
logger.Info(OpenPot, Started, Username)
    │
    ▼
try
    │
    ▼
HoneygainProcessor.OpenPot()
    │
    ├─► string selector = "//span[text()='Open Lucky Pot']/../../.."
    │
    ├─► webProcessor.WaitForElementToBeVisible(selector)
    │       │
    │       ▼
    │   Selenium waits for element to be visible (with timeout)
    │       │
    │       ▼
    ├─► webProcessor.IsElementVisible(selector)
    │       │
    │       ▼
    ├─► if (!visible)
    │       │
    │       ▼
    │   throw InvalidOperationException("The Lucky Pot cannot be opened at this time.")
    │       │
    │       ▼
    └─► if (visible)
            │
            ▼
        webProcessor.Click(selector)
            │
            ▼
        Button clicked, Lucky Pot opened
            │
            ▼
catch (Exception ex)
    │
    ▼
logger.Error(OpenPot, Failure, ex, Username)  ← ERROR LEVEL
    │
    ▼
logger.Info(OpenPot, Success, Username)  ← ALWAYS LOGGED
```

## Detailed Steps

### 1. Locate Open Lucky Pot Button

```csharp
string openPotButtonSelector = Select.ByXPath("//span[text()='Open Lucky Pot']/../../..");
```

**Selector Breakdown:**
- `//span[text()='Open Lucky Pot']` — Find span with exact text "Open Lucky Pot"
- `/../../..` — Traverse up 3 levels in DOM
- Target: Clickable button/container element

**Rationale:** The clickable element is 3 levels above the text span.

### 2. Wait for Visibility

```csharp
webProcessor.WaitForElementToBeVisible(openPotButtonSelector);
```

- **Method:** `IWebProcessor.WaitForElementToBeVisible(string)`
- **Implementation:** Selenium `WebDriverWait` with `ExpectedConditions.ElementIsVisible`
- **Timeout:** Default from `SeleniumWebProcessor` (typically 10-30s)
- **Purpose:** Handles dynamic loading — pot button may appear after dashboard load

### 3. Verify Visibility (Race Condition Guard)

```csharp
if (!webProcessor.IsElementVisible(openPotButtonSelector))
{
    throw new InvalidOperationException("The Lucky Pot cannot be opened at this time.");
}
```

- **Method:** `IWebProcessor.IsElementVisible(string)` → `bool`
- **Implementation:** Selenium `FindElement` → `Displayed` property
- **Redundant check** — guards against element becoming hidden between wait and click
- **Business logic exception** — `InvalidOperationException` for "pot not available"

### 4. Click Button

```csharp
webProcessor.Click(openPotButtonSelector);
```

- **Method:** `IWebProcessor.Click(string)`
- **Implementation:** Selenium `FindElement` → `Click()`
- **No verification** — assumes click succeeds if element was visible

## Selectors Reference

| Element | Selector | Type | Fragility |
|---------|----------|------|-----------|
| Open Lucky Pot button | `//span[text()='Open Lucky Pot']/../../..` | XPath | **Very High** |

**Fragility Analysis:**
- **Text-dependent:** Exact text "Open Lucky Pot" — any localisation or wording change breaks it
- **Structure-dependent:** 3-level ancestor traversal — any DOM restructuring breaks it
- **No ID/class fallback** — purely text and structure based
- **Dynamic availability** — button only appears when pot is available (daily)

## Error Handling

### In HoneygainProcessor.OpenPot()
```csharp
webProcessor.WaitForElementToBeVisible(selector);
if (!webProcessor.IsElementVisible(selector))
{
    throw new InvalidOperationException("The Lucky Pot cannot be opened at this time.");
}
webProcessor.Click(selector);
```

- **Wait timeout** → Selenium `WebDriverTimeoutException` → propagates
- **Business logic failure** (not visible after wait) → `InvalidOperationException`
- **Infrastructure failure** (click intercepted) → propagates from `IWebProcessor`

### In BotService.OpenPot()
```csharp
try
{
    processor.OpenPot();
}
catch (Exception ex)
{
    logger.Error(MyOperation.OpenPot, OperationStatus.Failure, ex, logInfos);  // ERROR
}
logger.Info(MyOperation.OpenPot, OperationStatus.Success, logInfos);
```

- **All exceptions caught** — including `InvalidOperationException` and timeouts
- **Logged as Error** — pot opening is a primary objective
- **Success logged anyway** — unconditional after catch

### Log Events

| Sequence | Level | Operation | Status | Context |
|----------|-------|-----------|--------|---------|
| 1 | Info | OpenPot | Started | Username |
| 2 | Error | OpenPot | Failure | Username + Exception (if failed) |
| 3 | Info | OpenPot | Success | Username |

## Failure Scenarios

| Scenario | Detection | Result |
|----------|-----------|--------|
| Pot not available today | `WaitForElementToBeVisible` timeout OR `IsElementVisible` false | `InvalidOperationException` or timeout → logged as Error |
| Pot already opened | Button not present or disabled | `IsElementVisible` false → Error |
| Selector changed | `FindElement` fails during wait or check | Selenium exception → logged as Error |
| Network/page error | Any infrastructure exception | Propagated → logged as Error |
| Click intercepted | `Click()` throws | Exception → logged as Error |

## Timing Characteristics

| Step | Typical Duration | Variability |
|------|------------------|-------------|
| WaitForElementToBeVisible | 0-30s (timeout) | High (depends on pot availability) |
| IsElementVisible | < 500ms | Low |
| Click (if visible) | < 500ms | Low |
| **Total (pot available)** | **1-5s** | **Medium** |
| **Total (pot not available)** | **~30s (timeout)** | **Low** |

## Invariants

1. **Wait before check** — unlike `ClaimReward`, waits for element first
2. **Race condition guard** — double-check visibility after wait
3. **Business logic failure = Error** — "pot not available" is logged as Error (not Warn)
4. **Always continues** — `BotService.Run()` completes after `OpenPot()` regardless
5. **Selector is inline** — not a constant, defined in method body
6. **No retry** — single wait/check/click per process invocation

## Modification Guidance

| Change | Files to Modify | Testing |
|--------|-----------------|---------|
| Update selector | `HoneygainProcessor.OpenPot()` inline selector | Unit test: `GivenOpenPot_WhenButtonVisible_ThenClicksButton` |
| Change wait timeout | Requires `IWebProcessor` extension or `SeleniumWebProcessor` config | Not currently configurable |
| Change exception type | `HoneygainProcessor.OpenPot()` throw statement | Update tests expecting `InvalidOperationException` |
| Remove wait | Remove `WaitForElementToBeVisible` call | Would change behaviour; update tests |
| Add retry logic | `BotService.OpenPot()` wrap in loop | Would change single-pass design |
| Change log level | `BotService.OpenPot()` catch block | Update tests verifying Error level |

## Comparison: ClaimReward vs OpenPot

| Aspect | ClaimReward | OpenPot |
|--------|-------------|---------|
| Wait before check | No | Yes (`WaitForElementToBeVisible`) |
| Double-check visibility | No | Yes (after wait) |
| Failure log level | Warn | Error |
| Business logic exception | `InvalidOperationException` | `InvalidOperationException` |
| Selector traversal | 5 levels up | 3 levels up |
| Typical duration (success) | < 2s | 1-5s |
| Typical duration (failure) | < 1s | ~30s (timeout) |

## Related Documentation

- [Components: Integration Models](./components/integration-models.md#openpot)
- [Components: Application Services](./components/application-services.md#openpot)
- [Error Handling](./error-handling.md)
- [Logging](./logging.md)
- [Testing](./testing.md)