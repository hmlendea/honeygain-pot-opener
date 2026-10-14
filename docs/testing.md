# Testing

## Test Strategy

### Pyramid

| Layer | Count | Framework | Scope |
|-------|-------|-----------|-------|
| Unit | ~30 | NUnit 5 + Moq | Individual classes, mocked dependencies |
| Integration | 0 | — | Not implemented |
| E2E | 0 | — | Not implemented (requires live Honeygain) |

### Principles

- **Given/When/Then** naming convention for test methods
- **AAA** (Arrange/Act/Assert) structure within methods
- **One assertion per test** (mostly)
- **No shared state** between tests
- **Deterministic** — no flaky timing dependencies

## Test Project Structure

```
HoneygainPotOpener.UnitTests/
├── Configuration/
│   ├── BotSettingsTests.cs
│   └── DebugSettingsTests.cs
├── Logging/
│   ├── MyLogInfoKeyTests.cs
│   └── MyOperationTests.cs
├── Processors/
│   └── HoneygainProcessorTests.cs
└── Service/
    └── BotServiceTests.cs
```

## Unit Tests

### Configuration Tests

**BotSettingsTests.cs**
- `GivenDefaultConstructor_WhenCreatingInstance_ThenPropertiesAreNull`
- `GivenValidValues_WhenSettingProperties_ThenPropertiesHoldValues`

**DebugSettingsTests.cs**
- `GivenDefaultConstructor_WhenCreatingInstance_ThenIsDebugModeIsFalse`
- `GivenIsDebugModeTrue_WhenCreatingInstance_ThenIsHeadlessIsFalse`
- `GivenIsDebugModeFalse_WhenCreatingInstance_ThenIsHeadlessIsTrue`

### Logging Tests

**MyLogInfoKeyTests.cs**
- `GivenUsernameKey_WhenAccessing_ThenKeyNameIsUsername`

**MyOperationTests.cs**
- `GivenLogInOperation_WhenAccessing_ThenNameIsLogIn`
- `GivenClaimRewardOperation_WhenAccessing_ThenNameIsClaimReward`
- `GivenOpenPotOperation_WhenAccessing_ThenNameIsOpenPot`

### Processor Tests (HoneygainProcessorTests.cs)

**LogIn**
- `GivenLogIn_WhenCalled_ThenNavigatesToLoginUrl`
- `GivenLogIn_WhenCalled_ThenAcceptsCookies`
- `GivenLogIn_WhenCalled_ThenEntersEmailAndPassword`
- `GivenLogIn_WhenCalled_ThenClicksLoginButton`
- `GivenLogIn_WhenCalled_ThenWaitsForDashboard`

**ClaimReward**
- `GivenClaimReward_WhenButtonVisible_ThenClicksClaimReward`
- `GivenClaimReward_WhenButtonNotVisible_ThenThrowsInvalidOperationException`

**OpenPot**
- `GivenOpenPot_WhenButtonVisible_ThenWaitsAndDoubleClicks`
- `GivenOpenPot_WhenButtonNotVisibleAfterWait_ThenThrowsInvalidOperationException`

**Selectors Verification**
- All XPath/ID selectors match HoneygainProcessor constants

### Service Tests (BotServiceTests.cs)

**Run Sequence**
- `GivenRun_WhenCalled_ThenInvokesLogInThenClaimRewardThenOpenPotInOrder`

**Error Handling**
- `GivenRun_WhenLogInThrows_ThenLogsErrorAndContinuesToClaimReward`
- `GivenRun_WhenClaimRewardThrows_ThenLogsWarningAndContinuesToOpenPot`
- `GivenRun_WhenOpenPotThrows_ThenLogsErrorAndCompletes`

**Logging Verification**
- Each operation logs Started, Failure/Success (in catch), Success (always)
- Correct severity: LogIn=Error, ClaimReward=Warn, OpenPot=Error
- Username LogInfo included in all operation logs

## Test Conventions

### Naming

```csharp
[Test]
public void Given<Precondition>_When<Action>_Then<ExpectedOutcome>()
```

Examples:
- `GivenRun_WhenLogInThrows_ThenLogsErrorAndContinuesToClaimReward`
- `GivenClaimReward_WhenButtonNotVisible_ThenThrowsInvalidOperationException`

### Mocking

```csharp
// Arrange
var mockWebProcessor = new Mock<IWebProcessor>();
var mockLogger = new Mock<ILogger>();
var processor = new HoneygainProcessor(mockWebProcessor.Object, mockLogger.Object);

// Act
processor.LogIn();

// Assert
mockWebProcessor.Verify(p => p.GoToUrl("https://dashboard.honeygain.com/login"), Times.Once);
mockWebProcessor.Verify(p => p.SetText(By.Id("email"), "test@example.com"), Times.Once);
```

### Verification Patterns

| Pattern | Usage |
|---------|-------|
| `Verify(..., Times.Once)` | Expected single call |
| `Verify(..., Times.Never)` | Expected no call |
| `VerifyNoOtherCalls()` | Strict interaction verification |
| `SetupSequence` | Ordered return values |

## Running Tests

```bash
# All tests
dotnet test HoneygainPotOpener.slnx -c Release

# Specific project
dotnet test HoneygainPotOpener.UnitTests/HoneygainPotOpener.UnitTests.csproj -c Release

# With coverage
dotnet test HoneygainPotOpener.slnx -c Release --collect:"XPlat Code Coverage"

# Filter by name
dotnet test --filter "FullyQualifiedName~BotServiceTests"

# Filter by class
dotnet test --filter "FullyQualifiedName~HoneygainProcessorTests"
```

## Coverage

### Current Coverage Targets

| Assembly | Target | Actual |
|----------|--------|--------|
| HoneygainPotOpener | 80%+ | Run `dotnet test --collect` to measure |

### Excluded from Coverage

- `Program.cs` (composition root, hard to unit test)
- `MyLogInfoKey.cs`, `MyOperation.cs` (static constants)
- `BotSettings.cs`, `DebugSettings.cs` (POCOs)

## Test Data

### Standard Test Values

| Value | Use |
|-------|-----|
| `test@example.com` | Email in tests |
| `password123` | Password in tests |
| `https://dashboard.honeygain.com/login` | Login URL |
| `//a[contains(@href, '/earnings')]` | Dashboard selector |

### No External Test Data

- No test fixtures files
- No database seeds
- No external API mocks (all mocked via Moq)

## CI Integration

### GitHub Actions

```yaml
- name: Test
  run: dotnet test HoneygainPotOpener.slnx -c Release --no-build --verbosity normal
```

### Test Results

- NUnit XML output consumed by GitHub Actions
- Coverage via `coverlet.collector` → Cobertura XML
- No quality gate enforced (no minimum coverage threshold)

## Gaps & Future Work

| Gap | Priority | Effort |
|-----|----------|--------|
| Integration tests (real WebDriver) | Medium | High |
| E2E tests (test Honeygain account) | Low | High |
| Contract tests for IWebProcessor | Low | Medium |
| Property-based testing for selectors | Low | Medium |
| Mutation testing | Low | Medium |
| Performance benchmarks | Low | Low |

## Related Documentation

- [Components: Application Services](../components/application-services.md)
- [Components: Integration Models](../components/integration-models.md)
- [Error Handling](../error-handling.md)
- [Build and Deployment](../build-and-deployment.md)
- [API Reference](../api-reference/INDEX.md)