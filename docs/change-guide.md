# Change Guide

## Versioning

**Scheme:** Semantic Versioning (SemVer) — `MAJOR.MINOR.PATCH`

| Version | Meaning |
|---------|---------|
| MAJOR | Breaking changes to config, selectors, or public interfaces |
| MINOR | New features, new operations, non-breaking config additions |
| PATCH | Bug fixes, selector updates, logging improvements |

**Current:** `1.0.0` (initial)

## Release Process

1. Update version in `HoneygainPotOpener.csproj` (`<Version>`)
2. Update `CHANGELOG.md` (if exists) or release notes
3. Tag: `git tag v1.2.3`
4. Push tag: `git push origin v1.2.3`
5. GitHub Actions builds and creates release

## Breaking Changes

### Configuration

| Change | Version | Migration |
|--------|---------|-----------|
| Rename config section | MAJOR | Update `appsettings.json` and `Program.cs` bind calls |
| Remove config property | MAJOR | Provide default or make optional |
| Change property type | MAJOR | Update JSON and binding class |

### Selectors (HoneygainProcessor)

| Change | Version | Migration |
|--------|---------|-----------|
| XPath/ID change | PATCH (if fix) / MINOR (if UI change) | Update constant in `HoneygainProcessor.cs` |
| New selector added | MINOR | Add constant, update method |
| Selector removed | MAJOR | Remove constant, update method |

### Interfaces

| Change | Version | Migration |
|--------|---------|-----------|
| Add method to `IHoneygainProcessor` | MINOR | Implement in `HoneygainProcessor`, update tests |
| Remove method from `IHoneygainProcessor` | MAJOR | Remove implementation, update consumers |
| Change `IWebProcessor` (NuciWeb) | Depends on NuciWeb version | Update NuciWeb package, adapt usage |

### Logging

| Change | Version | Migration |
|--------|---------|-----------|
| Add `LogInfoKey` | MINOR | Add to `MyLogInfoKey`, use in operations |
| Change `Operation` name | PATCH | Update `MyOperation` constant |
| Change log level for operation | PATCH | Update `BotService` catch block |

## Non-Breaking Changes

- Add new config section (optional)
- Add new optional config property with default
- Improve error messages
- Add logging fields
- Update dependencies (patch/minor)
- Improve test coverage

## Deprecation Policy

1. **Mark obsolete:** `[Obsolete("Use X instead. Removed in v2.0.")]`
2. **Keep for 1 MINOR version**
3. **Remove in next MAJOR**

## Configuration Migration

### Adding a New Setting

```csharp
// 1. Add to settings class
public sealed class BotSettings
{
    public string EmailAddress { get; set; }
    public string Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;  // New with default
}

// 2. No code change needed — binding picks up default
// 3. Document in configuration.md
```

### Removing a Setting

```csharp
// 1. Mark obsolete
public sealed class BotSettings
{
    public string EmailAddress { get; set; }
    public string Password { get; set; }

    [Obsolete("No longer used. Removed in v2.0.")]
    public int OldSetting { get; set; }
}

// 2. Remove after 1 minor version
```

## Selector Migration (Honeygain UI Changes)

### Process

1. **Detect:** Test fails with `NoSuchElementException` or `WebDriverTimeoutException`
2. **Inspect:** Open Honeygain dashboard manually, find new selector
3. **Update:** Change constant in `HoneygainProcessor.cs`
4. **Test:** Run unit tests (verify selector constants), run manual test
5. **Release:** PATCH version

### Selector Constants

```csharp
// In HoneygainProcessor.cs
private const string LoginUrl = "https://dashboard.honeygain.com/login";
private const string CookieAcceptButtonId = "onetrust-accept-btn-handler";
private const string EmailInputId = "email";
private const string PasswordInputId = "password";
private const string LoginButtonId = "login-btn";
private const string DashboardIndicatorXPath = "//a[contains(@href, '/earnings')]";
private const string ClaimRewardButtonXPath = "//button[contains(text(), 'Claim reward')]";
private const string OpenLuckyPotButtonXPath = "//button[contains(text(), 'Open Lucky Pot')]";
```

## Dependency Updates

### Microsoft.Extensions.*

- Update with .NET SDK releases
- Usually PATCH (9.0.0 → 9.0.1)
- Test: `dotnet build`, `dotnet test`

### NuciLog / NuciWeb

- Check release notes for breaking changes
- Usually MINOR for new features
- Test: logging output, WebDriver operations

### Selenium

- Updated via NuciWeb.Automation.Selenium
- Major version = breaking WebDriver API
- Test: all browser operations

## Testing Changes

### Adding Tests

- Follow Given/When/Then naming
- Mock external dependencies (IWebProcessor, ILogger)
- Verify interactions, not implementation

### Modifying Tests

- Update when behaviour changes intentionally
- Don't update to match broken behaviour

## Documentation Updates

| Change | Docs to Update |
|--------|----------------|
| Config change | `configuration.md`, `README.md` |
| New operation | `flows/*.md`, `api-reference/INDEX.md` |
| Selector change | `components/integration-models.md`, `flows/*.md` |
| Logging change | `logging.md`, `api-reference/INDEX.md` |
| Error handling change | `error-handling.md` |
| Security change | `security.md`, `PRIVACY.md` |
| Dependency change | `dependencies.md`, `build-and-deployment.md` |

## Rollback Procedure

1. **Config:** Restore previous `appsettings.json`
2. **Binary:** Deploy previous release artifact
3. **Selector:** Revert `HoneygainProcessor.cs` constants
4. **Data:** No persistent data to rollback

## Related Documentation

- [Build and Deployment](./build-and-deployment.md)
- [Dependencies](./dependencies.md)
- [Configuration](./configuration.md)
- [Components: Integration Models](./components/integration-models.md)
- [Testing](./testing.md)