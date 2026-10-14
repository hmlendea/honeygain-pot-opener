# Dependencies

## Direct Dependencies (NuGet)

### Runtime

| Package | Version | Purpose | License |
|---------|---------|---------|---------|
| `Microsoft.Extensions.Configuration` | 9.0.0 | Configuration abstraction | MIT |
| `Microsoft.Extensions.Configuration.Json` | 9.0.0 | JSON configuration provider | MIT |
| `Microsoft.Extensions.DependencyInjection` | 9.0.0 | DI container | MIT |
| `NuciLog` | 1.2.1 | Structured logging facade | MIT |
| `NuciLog.Core` | 3.1.0 | Logging abstractions (Operation, LogInfoKey) | MIT |
| `NuciWeb` | 4.0.0 | Web automation abstractions | MIT |
| `NuciWeb.Automation` | 1.0.0 | IWebProcessor interface | MIT |
| `NuciWeb.Automation.Selenium` | 1.0.2 | Selenium implementation of IWebProcessor | MIT |

### Transitive (Key)

| Package | Version | Source |
|---------|---------|--------|
| `Selenium.WebDriver` | 4.x | NuciWeb.Automation.Selenium |
| `Selenium.Support` | 4.x | NuciWeb.Automation.Selenium |
| `System.Text.Json` | 9.0.0 | Microsoft.Extensions.Configuration.Json |

### Development / Test

| Package | Version | Purpose | Scope |
|---------|---------|---------|-------|
| `NUnit` | 5.0.0 | Unit testing framework | Test |
| `NUnit3TestAdapter` | 5.0.0 | VS Test integration | Test |
| `Moq` | 4.21.0 | Mocking framework | Test |
| `Microsoft.NET.Test.Sdk` | 17.x | Test runner | Test |
| `coverlet.collector` | 6.x | Code coverage | Test |

## Dependency Graph

```
HoneygainPotOpener (net10.0)
├── Microsoft.Extensions.Configuration (9.0.0)
│   └── System.Text.Json (9.0.0)
├── Microsoft.Extensions.Configuration.Json (9.0.0)
│   └── Microsoft.Extensions.Configuration (9.0.0)
├── Microsoft.Extensions.DependencyInjection (9.0.0)
├── NuciLog (1.2.1)
│   └── NuciLog.Core (3.1.0)
├── NuciWeb (4.0.0)
│   └── NuciWeb.Automation (1.0.0)
├── NuciWeb.Automation (1.0.0)
└── NuciWeb.Automation.Selenium (1.0.2)
    ├── NuciWeb.Automation (1.0.0)
    ├── Selenium.WebDriver (4.x)
    └── Selenium.Support (4.x)

HoneygainPotOpener.UnitTests (net10.0)
├── HoneygainPotOpener (project reference)
├── NUnit (5.0.0)
├── NUnit3TestAdapter (5.0.0)
├── Moq (4.21.0)
├── Microsoft.NET.Test.Sdk (17.x)
└── coverlet.collector (6.x)
```

## Version Policy

### Target Framework

- **net10.0** — .NET 10 (current preview/LTS)
- All packages compatible with net10.0 / netstandard2.0+

### Update Strategy

| Dependency Type | Policy |
|-----------------|--------|
| Microsoft.Extensions.* | Update with .NET SDK (aligned versions) |
| NuciLog / NuciWeb | Update when new features needed; pin major |
| Selenium | Update with NuciWeb.Automation.Selenium |
| Test packages | Update independently; no runtime impact |

### Version Constraints

```xml
<!-- HoneygainPotOpener.csproj -->
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageReference Include="NuciLog" Version="1.2.1" />
<PackageReference Include="NuciLog.Core" Version="3.1.0" />
<PackageReference Include="NuciWeb" Version="4.0.0" />
<PackageReference Include="NuciWeb.Automation" Version="1.0.0" />
<PackageReference Include="NuciWeb.Automation.Selenium" Version="1.0.2" />
```

**No version ranges** — exact versions for reproducible builds.

## Licence Compliance

### All Dependencies: MIT

| Package | Licence | SPDX | Compatible |
|---------|---------|------|------------|
| Microsoft.Extensions.* | MIT | MIT | Yes |
| NuciLog / NuciLog.Core | MIT | MIT | Yes |
| NuciWeb / NuciWeb.Automation / Selenium | MIT | MIT | Yes |
| Selenium.WebDriver | Apache-2.0 | Apache-2.0 | Yes (MIT-compatible) |
| NUnit / Moq / Test SDK | MIT / BSD-3 | MIT / BSD-3-Clause | Yes |

**No copyleft (GPL, AGPL, LGPL) dependencies.**

### Attribution

No NOTICE files required (MIT only requires licence copy in distribution).

## Vulnerability Management

### Scanning

```bash
# .NET built-in
dotnet list package --vulnerable --include-transitive

# NuGet audit
dotnet nuget audit
```

### Current Status

Run `dotnet list package --vulnerable` in repository root to verify.

### Response Process

1. **Critical/High:** Update immediately (patch version)
2. **Medium:** Update in next sprint
3. **Low:** Update during routine maintenance

## Native Dependencies

### Browser (Runtime Requirement)

| Browser | Minimum Version | Purpose |
|---------|----------------|---------|
| Chrome / Chromium | 110+ | Selenium WebDriver target |
| Firefox | 110+ | Alternative (not tested) |
| Edge | 110+ | Alternative (not tested) |

**WebDriverInitialiser** (from NuciWeb.Automation.Selenium) auto-discovers installed browser and matching driver.

### OS Requirements

| OS | Requirement |
|----|-------------|
| Linux | `libgtk-3`, `libnss3`, `libasound2`, `libxss1`, `libgconf-2-4`, Chrome/Chromium |
| Windows | Chrome/Edge installed |
| macOS | Chrome installed |

### .NET Runtime

- **.NET 10 Runtime** (or SDK for build)
- Self-contained deployment possible (`dotnet publish -r linux-x64 --self-contained`)

## Dependency Injection Registration

```csharp
// Program.cs
var services = new ServiceCollection();

// Configuration
services.AddSingleton(botSettings);
services.AddSingleton(debugSettings);

// Logging
services.AddNuciLoggerSettings(config)
    .AddSingleton<ILogger, NuciLogger>();

// Web automation
services.AddSingleton<IWebProcessor>(sp =>
{
    var driver = WebDriverInitialiser.Initialise(debugSettings.IsHeadless);
    return new SeleniumWebProcessor(driver);
});

// Domain
services.AddTransient<IHoneygainProcessor, HoneygainProcessor>();
services.AddSingleton<IBotService, BotService>();
```

### Lifetime Summary

| Service | Lifetime | Reason |
|---------|----------|--------|
| `BotSettings` | Singleton | Immutable after startup |
| `DebugSettings` | Singleton | Immutable after startup |
| `ILogger` | Singleton | NuciLog logger is thread-safe |
| `IWebProcessor` | Singleton | Wraps single WebDriver instance |
| `IHoneygainProcessor` | Transient | Stateless; new instance per scope (not used) |
| `IBotService` | Singleton | Stateless orchestrator |

## Package Sources

### nuget.org (Primary)

All packages from public NuGet Gallery.

### No Private Feeds

No internal/private package sources configured.

## Build & Restore

```bash
# Restore
dotnet restore HoneygainPotOpener.slnx

# Build
dotnet build HoneygainPotOpener.slnx -c Release

# Publish (self-contained)
dotnet publish HoneygainPotOpener/HoneygainPotOpener.csproj -c Release -r linux-x64 --self-contained -o ./publish
```

## Lock File

`HoneygainPotOpener/obj/project.assets.json` — generated by `dotnet restore`, committed to source control for reproducible builds.

## Outdated Check

```bash
# List outdated packages
dotnet list package --outdated

# Include transitive
dotnet list package --outdated --include-transitive
```

## Related Documentation

- [Build and Deployment](./build-and-deployment.md)
- [Components: Host and Composition](./components/host-and-composition.md)
- [Components: Integration Models](./components/integration-models.md)
- [Architecture](../architecture.md)