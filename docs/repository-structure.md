# Repository Structure

## Source Tree Layout

```
honeygain-pot-opener/
├── .github/
│   └── workflows/
│       └── dotnet.yml              # CI: build, test on push/PR to master
├── HoneygainPotOpener/             # Main console application
│   ├── Configuration/
│   │   ├── BotSettings.cs          # EmailAddress, Password
│   │   └── DebugSettings.cs        # IsDebugMode, IsHeadless
│   ├── Logging/
│   │   ├── MyLogInfoKey.cs         # Username log context key
│   │   └── MyOperation.cs          # LogIn, ClaimReward, OpenPot operations
│   ├── Processors/
│   │   ├── IHoneygainProcessor.cs  # Contract: LogIn, ClaimReward, OpenPot
│   │   └── HoneygainProcessor.cs   # Implementation with selectors
│   ├── Service/
│   │   ├── IBotService.cs          # Contract: Run()
│   │   └── BotService.cs           # Orchestration, logging, continuation
│   ├── appsettings.json            # Configuration (copied to output)
│   ├── HoneygainPotOpener.csproj   # Project file, dependencies
│   └── Program.cs                  # Composition root, entry point
├── HoneygainPotOpener.UnitTests/   # Unit test project
│   ├── Configuration/
│   │   ├── BotSettingsTests.cs
│   │   └── DebugSettingsTests.cs
│   ├── Logging/
│   │   ├── MyLogInfoKeyTests.cs
│   │   └── MyOperationTests.cs
│   ├── Processors/
│   │   └── HoneygainProcessorTests.cs
│   ├── Service/
│   │   └── BotServiceTests.cs
│   └── HoneygainPotOpener.UnitTests.csproj
├── ARCHITECTURE.md                 # Root architecture document
├── CONTRIBUTING.md                 # Contribution guidelines
├── HoneygainPotOpener.slnx         # Solution file
├── LICENSE                         # MIT License
├── PRIVACY.md                      # Privacy policy
├── README.md                       # Project readme
├── SECURITY.md                     # Security policy
├── release.sh                      # Release script (fetches remote helper)
└── docs/                           # This documentation
```

## Module Organisation

### HoneygainPotOpener (Main Application)

| Namespace | Purpose | Key Types |
|-----------|---------|-----------|
| `HoneygainPotOpener` | Composition root | `Program` |
| `HoneygainPotOpener.Configuration` | Settings transport | `BotSettings`, `DebugSettings` |
| `HoneygainPotOpener.Logging` | NuciLog extensions | `MyLogInfoKey`, `MyOperation` |
| `HoneygainPotOpener.Processors` | Honeygain integration | `IHoneygainProcessor`, `HoneygainProcessor` |
| `HoneygainPotOpener.Service` | Orchestration | `IBotService`, `BotService` |

### HoneygainPotOpener.UnitTests (Test Project)

| Namespace | Purpose | Key Types |
|-----------|---------|-----------|
| `HoneygainPotOpener.UnitTests.Configuration` | Settings validation | `BotSettingsTests`, `DebugSettingsTests` |
| `HoneygainPotOpener.UnitTests.Logging` | Log extensions validation | `MyLogInfoKeyTests`, `MyOperationTests` |
| `HoneygainPotOpener.UnitTests.Processors` | Processor behaviour | `HoneygainProcessorTests` |
| `HoneygainPotOpener.UnitTests.Service` | Orchestration behaviour | `BotServiceTests` |

## Project Files

### HoneygainPotOpener.csproj
- **Target:** `net10.0`
- **Output:** Exe
- **ImplicitUsings:** Disabled
- **Key Dependencies:**
  - `Microsoft.Extensions.Configuration` (10.0.13)
  - `Microsoft.Extensions.DependencyInjection` (10.0.13)
  - `NuciLog` (1.2.1), `NuciLog.Core` (3.1.0)
  - `NuciWeb` (4.0.0), `NuciWeb.Automation` (1.0.0), `NuciWeb.Automation.Selenium` (1.0.2)
- **Content:** `appsettings.json` copied to output (`PreserveNewest`)

### HoneygainPotOpener.UnitTests.csproj
- **Target:** `net10.0`
- **Test Framework:** NUnit 5.0.0 + NUnit3TestAdapter 6.3.1
- **Mocking:** Moq 4.21.0
- **Coverage:** coverlet.collector 10.1.0
- **Project Reference:** `../HoneygainPotOpener/HoneygainPotOpener.csproj`

### HoneygainPotOpener.slnx
- **Projects:** Main app + Unit tests

## Configuration Files

| File | Location | Purpose |
|------|----------|---------|
| `appsettings.json` | `HoneygainPotOpener/` | Runtime configuration (credentials, debug, logging) |
| `appsettings.json` | `HoneygainPotOpener/bin/Debug/net10.0/` | Copied output for debugging |
| `appsettings.json` | `HoneygainPotOpener.UnitTests/bin/Debug/net10.0/` | Copied for test execution |

## Build Outputs

| Path | Contents |
|------|----------|
| `HoneygainPotOpener/bin/Debug/net10.0/` | Main executable, deps, runtimeconfig, appsettings.json |
| `HoneygainPotOpener.UnitTests/bin/Debug/net10.0/` | Test assembly, test runner, main app deps |

## Documentation Files (Root)

| File | Purpose |
|------|---------|
| `ARCHITECTURE.md` | High-level architecture |
| `CONTRIBUTING.md` | Contribution guidelines |
| `PRIVACY.md` | Privacy policy |
| `README.md` | Project overview, usage, installation |
| `SECURITY.md` | Security policy |
| `ROADMAP.md` | Project roadmap (if present) |
| `LICENSE` | MIT License |

## Documentation Files (docs/)

See [INDEX.md](./INDEX.md) for complete catalogue.

## Naming Conventions

- **Projects:** PascalCase (`HoneygainPotOpener`, `HoneygainPotOpener.UnitTests`)
- **Namespaces:** Match project structure (`HoneygainPotOpener.Service`)
- **Classes:** PascalCase (`BotService`, `HoneygainProcessor`)
- **Interfaces:** `I` prefix + PascalCase (`IBotService`, `IHoneygainProcessor`)
- **Methods:** PascalCase (`Run`, `LogIn`, `ClaimReward`)
- **Properties:** PascalCase (`EmailAddress`, `IsDebugMode`)
- **Private Fields (tests):** camelCase with null-forgiving (`mockWebProcessor = null!`)
- **Test Methods:** `GivenX_WhenY_ThenZ` pattern

## Related Documentation

- [Repository Overview](./repository-overview.md)
- [Architecture (Detailed)](./architecture.md)
- [Components: Host and Composition](./components/host-and-composition.md)
- [Components: Application Services](./components/application-services.md)
- [Components: Integration Models](./components/integration-models.md)