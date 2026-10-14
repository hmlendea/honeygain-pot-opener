# Reward Claim Flow

## Overview

This flow covers the achievement reward claim process: detecting the "Claim reward" button visibility and clicking it if available.

## Sequence Diagram

```
BotService.ClaimReward()
    │
    ▼
logger.Info(ClaimReward, Started, Username)
    │
    ▼
try
    │
    ▼
HoneygainProcessor.ClaimReward()
    │
    ├─► string selector = "//span[text()='Claim reward']/../../../../../button"
    │
    ├─► webProcessor.IsElementVisible(selector)
    │       │
    │       ▼
    │   Selenium finds element, checks visibility
    │       │
    │       ▼
    ├─► if (!visible)
    │       │
    │       ▼
    │   throw InvalidOperationException("There is no reward available to be claimed at this time.")
    │       │
    │       ▼
    └─► if (visible)
            │
            ▼
        webProcessor.Click(selector)
            │
            ▼
        Button clicked, reward claimed
            │
            ▼
catch (Exception ex)
    │
    ▼
logger.Warn(ClaimReward, Failure, ex, Username)  ← WARNING LEVEL
    │
    ▼
logger.Info(ClaimReward, Success, Username)  ← ALWAYS LOGGED
```

## Detailed Steps

### 1. Locate Claim Reward Button

```csharp
string claimRewardButtonSelector = Select.ByXPath("//span[text()='Claim reward']/../../../../../button");
```

**Selector Breakdown:**
- `//span[text()='Claim reward']` — Find span with exact text "Claim reward"
- `/../../../../../` — Traverse up 5 levels in DOM
- `/button` — Find button element at that level

**Rationale:** The button containing the "Claim reward" text is nested deeply; this XPath traverses to the clickable button ancestor.

### 2. Check Visibility

```csharp
if (!webProcessor.IsElementVisible(claimRewardButtonSelector))
{
    throw new InvalidOperationException("There is no reward available to be claimed at this time.");
}
```

- **Method:** `IWebProcessor.IsElementVisible(string)` → `bool`
- **Implementation:** Selenium `FindElement` → `Displayed` property
- **No wait** — immediate visibility check (unlike `OpenPot` which waits first)
- **Business logic exception** — `InvalidOperationException` for "no reward available"

### 3. Click Button

```csharp
webProcessor.Click(claimRewardButtonSelector);
```

- **Method:** `IWebProcessor.Click(string)`
- **Implementation:** Selenium `FindElement` → `Click()`
- **No verification** — assumes click succeeds if element was visible

## Selectors Reference

| Element | Selector | Type | Fragility |
|---------|----------|------|-----------|
| Claim reward button | `//span[text()='Claim reward']/../../../../../button` | XPath | **Very High** |

**Fragility Analysis:**
- **Text-dependent:** Exact text "Claim reward" — any localisation or wording change breaks it
- **Structure-dependent:** 5-level ancestor traversal — any DOM restructuring breaks it
- **No ID/class fallback** — purely text and structure based

## Error Handling

### In HoneygainProcessor.ClaimReward()
```csharp
if (!webProcessor.IsElementVisible(selector))
{
    throw new InvalidOperationException("There is no reward available to be claimed at this time.");
}
webProcessor.Click(selector);
```

- **Business logic failure** → `InvalidOperationException` (expected, not an error)
- **Infrastructure failure** (element not found, click intercepted) → propagates from `IWebProcessor`

### In BotService.ClaimReward()
```csharp
try
{
    processor.ClaimReward();
}
catch (Exception ex)
{
    logger.Warn(MyOperation.ClaimReward, OperationStatus.Failure, ex, logInfos);  // WARNING
}
logger.Info(MyOperation.ClaimReward, OperationStatus.Success, logInfos);
```

- **All exceptions caught** — including `InvalidOperationException`
- **Logged as Warn** — not an error condition; reward may simply not be available
- **Success logged anyway** — unconditional after catch

### Log Events

| Sequence | Level | Operation | Status | Context |
|----------|-------|-----------|--------|---------|
| 1 | Info | ClaimReward | Started | Username |
| 2 | Warn | ClaimReward | Failure | Username + Exception (if failed) |
| 3 | Info | ClaimReward | Success | Username |

## Failure Scenarios

| Scenario | Detection | Result |
|----------|-----------|--------|
| No reward available | `IsElementVisible` returns false | `InvalidOperationException` → logged as Warn |
| Reward already claimed | Button not present or disabled | `IsElementVisible` false → Warn |
| Selector changed | `FindElement` fails | Selenium exception → logged as Warn |
| Network/page error | Any infrastructure exception | Propagated → logged as Warn |
| Click intercepted | `Click()` throws | Exception → logged as Warn |

## Timing Characteristics

| Step | Typical Duration | Variability |
|------|------------------|-------------|
| IsElementVisible | < 500ms | Low |
| Click (if visible) | < 500ms | Low |
| **Total (no reward)** | **< 1s** | **Low** |
| **Total (reward claimed)** | **< 2s** | **Low** |

## Invariants

1. **No wait before visibility check** — immediate check (unlike `OpenPot`)
2. **Business logic failure = Warn** — "no reward" is expected, not an error
3. **Always continues** — `BotService.Run()` proceeds to `OpenPot()` regardless
4. **Selector is inline** — not a constant, defined in method body
5. **No retry** — single check per process invocation

## Modification Guidance

| Change | Files to Modify | Testing |
|--------|-----------------|---------|
| Update selector | `HoneygainProcessor.ClaimReward()` inline selector | Unit test: `GivenClaimReward_WhenButtonVisible_ThenClicksButton` |
| Change exception type | `HoneygainProcessor.ClaimReward()` throw statement | Update tests expecting `InvalidOperationException` |
| Add wait before check | `HoneygainProcessor.ClaimReward()` add `WaitForElementToBeVisible` | New test for wait behaviour |
| Add retry logic | `BotService.ClaimReward()` wrap in loop | Would change single-pass design |
| Change log level | `BotService.ClaimReward()` catch block | Update tests verifying Warn level |

## Related Documentation

- [Components: Integration Models](./components/integration-models.md#claimreward)
- [Components: Application Services](./components/application-services.md#claimreward)
- [Error Handling](./error-handling.md)
- [Logging](./logging.md)
- [Testing](./testing.md)