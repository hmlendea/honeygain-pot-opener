# Architecture (Detailed)

This document elaborates on the root [ARCHITECTURE.md](../ARCHITECTURE.md) with implementation-level detail for contributors who need to modify components, trace execution, or evaluate changes.

## Architectural Style

The application follows a **dependency-injected synchronous console pipeline** with three principal boundaries:

1. **Composition Root** (`Program.cs`) — Loads configuration, initialises WebDriver, registers services, resolves `IBotService`, records startup, and terminates the driver in `finally`.
2. **Orchestration Service** (`BotService`) — Owns the fixed operation sequence (`LogIn` → `ClaimReward` → `OpenPot`), emits structured log events for each operation (started, failure/warning, success), and applies best-effort continuation: every operation is attempted regardless of prior failures.
3. **Integration Adapter** (`HoneygainProcessor`) — Translates domain operations into browser commands via `IWebProcessor`. Owns all Honeygain-specific URLs, selectors, and interaction logic.

```
Program (composition)
    └─► BotService (orchestration)
            └─► IHoneygainProcessor (contract)
                    └─► HoneygainProcessor (adapter)
                            └─► IWebProcessor (contract)
                                    └─► SeleniumWebProcessor (infrastructure)
                                            └─► IWebDriver (browser)
```

## Component Responsibilities

### Program (Composition Root)

**Location:** `HoneygainPotOpener/Program.cs`

**Responsibilities:**
- Loads `appsettings.json` with optional file and change monitoring
- Binds `BotSettings` and `DebugSettings` once at startup (singleton)
- Initialises `IWebDriver` via `WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode)`
- Registers services in `ServiceCollection`:
  - `BotSettings`, `DebugSettings` — singleton
  - `ILogger` → `NuciLogger` — singleton
  - `IWebDriver` — singleton (the single driver instance)
  - `IWebProcessor` → `SeleniumWebProcessor` — transient (but retained by singleton `HoneygainProcessor`)
  - `IHoneygainProcessor` → `HoneygainProcessor` — singleton
  - `IBotService` → `BotService` — singleton
- Resolves `ILogger`, records `Operation.StartUp`
- Resolves `IBotService`, invokes `Run()` in `try`
- Catches `AggregateException` and `Exception`, logs fatal via `logger.Fatal`
- Calls `webDriver.Quit()` in `finally`

**Key Implementation Details:**
- Configuration binding occurs **once**; runtime changes to `appsettings.json` do not affect running process
- WebDriver initialisation happens **before** DI container construction
- No explicit `IServiceProvider.Dispose()` call; driver cleanup is manual in `finally`
- No exit code is set; process exits 0 unless unhandled exception escapes `Main`

### BotService (Orchestration)

**Location:** `HoneygainPotOpener/Service/BotService.cs`

**Responsibilities:**
- Implements `IBotService.Run()` → calls `LogIn()`, `ClaimReward()`, `OpenPot()` in fixed order
- Each operation method:
  1. Creates `LogInfo` with `MyLogInfoKey.Username` = `settings.EmailAddress`
  2. Logs `OperationStatus.Started`
  3. Invokes corresponding `IHoneygainProcessor` method in `try`
  4. On exception:
     - `LogIn` / `OpenPot` → `logger.Error(..., OperationStatus.Failure, ex, logInfos)`
     - `ClaimReward` → `logger.Warn(..., OperationStatus.Failure, ex, logInfos)`
  5. Logs `OperationStatus.Success` **unconditionally** after `catch`

**Critical Behaviour:**
- Success log is emitted **even when the operation threw** — log-derived success is not evidence of completion
- Operations are independent; failure in one does not stop subsequent operations
- Email address is included in every operation log for correlation

### HoneygainProcessor (Integration Adapter)

**Location:** `HoneygainPotOpener/Processors/HoneygainProcessor.cs`

**Responsibilities:**
- Owns Honeygain URLs:
  - `HomePageUrl` = `https://honeygain.com`
  - `DashboardUrl` = `https://dashboard.honeygain.com`
  - `LogInUrl` = `https://dashboard.honeygain.com/login`
- Implements `IHoneygainProcessor`:
  - `LogIn()` — Navigates to login, accepts cookies if present, fills email/password, clicks login, waits for dashboard
  - `ClaimReward()` — Checks visibility of "Claim reward" button via XPath; throws `InvalidOperationException` if not visible; clicks if visible
  - `OpenPot()` — Waits for "Open Lucky Pot" button visibility; throws `InvalidOperationException` if not visible; clicks if visible
- Private `AcceptCookiesIfNeeded()` — Waits for cookie consent button, clicks if visible

**Selectors (XPath/ID):**
| Operation | Selector | Type |
|-----------|----------|------|
| Email input | `#email` | ID |
| Password input | `#password` | ID |
| Login button | `//form/button` | XPath |
| Dashboard ready | `#traffic-legend-container` | ID |
| Cookie consent | `//img[@alt='cookie consent']/../div/button[2]` | XPath |
| Claim reward | `//span[text()='Claim reward']/../../../../../button` | XPath |
| Open Lucky Pot | `//span[text()='Open Lucky Pot']/../../..` | XPath |

**Dependencies:**
- `IWebProcessor` (injected) — generic browser operations
- `BotSettings` (injected) — credentials

### SeleniumWebProcessor (Browser Infrastructure)

**Location:** Provided by `NuciWeb.Automation.Selenium` package (`IWebProcessor` implementation)

**Responsibilities:**
- Implements `IWebProcessor` contract:
  - `GoToUrl(string url)`
  - `SetText(string selector, string text)`
  - `Click(string selector)`
  - `WaitForElementToBeVisible(string selector)`
  - `IsElementVisible(string selector)` → `bool`
- Wraps Selenium `IWebDriver` with NuciWeb selector abstraction (`Select.ById`, `Select.ByXPath`)

**Integration Boundary:** `HoneygainProcessor` depends on `IWebProcessor`, not Selenium directly. This isolates selector logic from browser implementation.

## Data Flow

```
appsettings.json
    │
    ▼ (bind once)
BotSettings { EmailAddress, Password }
DebugSettings { IsDebugMode }
    │
    ▼ (injected)
Program ──► IWebDriver (created via WebDriverInitialiser)
    │
    ▼ (DI registration)
BotService ◄── IHoneygainProcessor ◄── HoneygainProcessor
    │                    │
    │                    ▼
    │             IWebProcessor ◄── SeleniumWebProcessor
    │                    │
    ▼                    ▼
NuciLogger ◄────────── IWebDriver (browser session)
    │
    ▼
logfile.log (if enabled)
```

**Credential Flow:**
1. `appsettings.json` → `BotSettings` (in-memory, singleton)
2. `BotSettings.EmailAddress` → `HoneygainProcessor.LogIn()` → `IWebProcessor.SetText("#email", email)`
3. `BotSettings.Password` → `HoneygainProcessor.LogIn()` → `IWebProcessor.SetText("#password", password)`
4. `BotSettings.EmailAddress` → `BotService` → `LogInfo(MyLogInfoKey.Username, email)` → `NuciLogger` → log file

**Password is never logged.**

## Runtime Sequence

```
1. Process starts
2. Program.LoadConfiguration() → binds settings
3. WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode)
4. ServiceCollection registration
5. logger.Info(Operation.StartUp)
6. botService.Run()
   ├─ LogIn()
   │   ├─ logger.Info(LogIn, Started, Username)
   │   ├─ processor.LogIn()
   │   │   ├─ webProcessor.GoToUrl(LogInUrl)
   │   │   ├─ AcceptCookiesIfNeeded()
   │   │   ├─ SetText(email, password)
   │   │   ├─ Click(login button)
   │   │   └─ WaitForElement(traffic-legend-container)
   │   ├─ catch → logger.Error(LogIn, Failure, ex, Username)
   │   └─ logger.Info(LogIn, Success, Username)
   ├─ ClaimReward()
   │   ├─ logger.Info(ClaimReward, Started, Username)
   │   ├─ processor.ClaimReward()
   │   │   ├─ IsElementVisible(claim reward button)
   │   │   ├─ if false → throw InvalidOperationException
   │   │   └─ Click(claim reward button)
   │   ├─ catch → logger.Warn(ClaimReward, Failure, ex, Username)
   │   └─ logger.Info(ClaimReward, Success, Username)
   └─ OpenPot()
       ├─ logger.Info(OpenPot, Started, Username)
       ├─ processor.OpenPot()
       │   ├─ WaitForElement(open pot button)
       │   ├─ IsElementVisible(open pot button)
       │   ├─ if false → throw InvalidOperationException
       │   └─ Click(open pot button)
       ├─ catch → logger.Error(OpenPot, Failure, ex, Username)
       └─ logger.Info(OpenPot, Success, Username)
7. webDriver.Quit() in finally
```

## Dependency Direction Rules

| From | To | Rule |
|------|-----|------|
| `Program` | All concrete implementations | Only composition root selects implementations |
| `BotService` | `IHoneygainProcessor` | Orchestration depends on contract, not adapter |
| `HoneygainProcessor` | `IWebProcessor` | Adapter depends on generic web contract |
| `SeleniumWebProcessor` | `IWebDriver` | Infrastructure depends on browser abstraction |
| Settings/Logging | Processors/Services | Configuration flows down; no upward dependencies |

## Extension Points

### Honeygain Processing
- **Interface:** `IHoneygainProcessor`
- **Implementation:** `HoneygainProcessor`
- **To extend:** Add methods to interface and implementation; update `BotService.Run()` to call new operations

### Browser Automation
- **Interface:** `IWebProcessor`
- **Implementation:** `SeleniumWebProcessor` (from NuciWeb.Automation.Selenium)
- **To replace:** Register alternative `IWebProcessor` implementation in `Program.cs`

## Source Map

| Area | Files |
|------|-------|
| Composition | `Program.cs` |
| Configuration | `Configuration/BotSettings.cs`, `Configuration/DebugSettings.cs` |
| Orchestration | `Service/BotService.cs`, `Service/IBotService.cs` |
| Honeygain Integration | `Processors/HoneygainProcessor.cs`, `Processors/IHoneygainProcessor.cs` |
| Logging Extensions | `Logging/MyLogInfoKey.cs`, `Logging/MyOperation.cs` |
| Unit Tests | `HoneygainPotOpener.UnitTests/Processors/HoneygainProcessorTests.cs`, `HoneygainPotOpener.UnitTests/Service/BotServiceTests.cs` |

## Related Documentation

- [Root ARCHITECTURE.md](../ARCHITECTURE.md) — High-level overview
- [components/host-and-composition.md](./components/host-and-composition.md) — Program.cs deep dive
- [components/application-services.md](./components/application-services.md) — BotService deep dive
- [components/integration-models.md](./components/integration-models.md) — HoneygainProcessor and browser automation
- [flows/startup-and-rendering.md](./flows/startup-and-rendering.md) — Startup sequence
- [flows/authentication.md](./flows/authentication.md) — Login flow
- [flows/reward-claim.md](./flows/reward-claim.md) — Reward claim flow
- [flows/lucky-pot.md](./flows/lucky-pot.md) — Lucky Pot flow
- [configuration.md](./configuration.md) — Configuration details
- [logging.md](./logging.md) — Logging details
- [error-handling.md](./error-handling.md) — Error handling patterns