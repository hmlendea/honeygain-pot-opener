[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/honeygain-pot-opener)](https://github.com/hmlendea/honeygain-pot-opener/releases/latest)
[![Build Status](https://github.com/hmlendea/honeygain-pot-opener/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/honeygain-pot-opener/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/github/license/hmlendea/honeygain-pot-opener)](https://github.com/hmlendea/honeygain-pot-opener/blob/master/LICENSE)

# Honeygain Pot Opener

A .NET console application that authenticates with Honeygain, claims an available achievement reward, and attempts to open the daily Lucky Pot automatically.

## 📑 Table of Contents

- [Table of Contents](#table-of-contents)
- [Capabilities](#capabilities)
- [Usage](#usage)
- [Known Limitations](#known-limitations)
- [System Requirements](#system-requirements)
- [Installation](#installation)
  - [Manual Installation](#manual-installation)
- [Configuration](#configuration)
  - [Configuration Files](#configuration-files)
  - [Settings](#settings)
  - [Reload Behaviour](#reload-behaviour)
  - [Secret Management](#secret-management)
- [Compatibility](#compatibility)
- [Integrations](#integrations)
- [Authentication and Authorisation](#authentication-and-authorisation)
- [Privacy and Data](#privacy-and-data)
  - [Data Locations](#data-locations)
- [Development](#development)
  - [Requirements](#requirements)
  - [Setup](#setup)
  - [Build](#build)
  - [Run](#run)
  - [Continuous Integration](#continuous-integration)
  - [Release](#release)
  - [Dependencies](#dependencies)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [Project Engagement](#project-engagement)
- [License](#license)

## ✨ Capabilities

- Authenticates with the Honeygain dashboard by using configured account credentials.
- Accepts the Honeygain cookie consent prompt when it is displayed.
- Claims an available achievement reward and continues when no reward can be claimed.
- Attempts to open the daily Lucky Pot after authentication.
- Records structured start, authentication, reward, and pot operation events through NuciLog.

## 🚀 Usage

Populate the credential settings in `appsettings.json`, then execute one automation pass from the repository root:

```bash
dotnet run --project HoneygainPotOpener.csproj
```

The application exits after the reward and Lucky Pot operations have been attempted.

## ⚠️ Known Limitations

- The application performs one automation pass per invocation and does not include a scheduler.
- Lucky Pot availability is determined by Honeygain and the operation cannot proceed when the dashboard does not present the corresponding control.
- Dashboard automation depends on the current Honeygain element identifiers and text, so interface revisions can require selector modifications.

## 🖥️ System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| Honeygain | An active account | N/A |
| Browser automation | A locally available Selenium-compatible browser and driver | N/A |
| Operating system | Linux, macOS, or Windows on an architecture with a published release archive | N/A |

## 📦 Installation

[![Obtain it from GitHub](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/github.png)](https://github.com/hmlendea/honeygain-pot-opener/releases)

### Manual Installation

1. Download the archive for your operating system and processor architecture from the [latest release](https://github.com/hmlendea/honeygain-pot-opener/releases/latest). Published archives support Linux (`arm`, `arm64`, and `x64`), macOS (`arm64` and `x64`), and Windows (`arm64` and `x64`).
2. Extract the archive to a local directory.
3. Populate the credential settings in the extracted `appsettings.json`.
4. Ensure that a compatible browser and WebDriver are available.
5. Execute the application from the extraction directory so that it can access its configuration and relative log path.

## ⚙️ Configuration

The application reads `appsettings.json` from its working directory. Replace the credential placeholders before execution and revise the remaining defaults when required.

### Configuration Files

| File | Scope | Purpose |
|------|-------|---------|
| `appsettings.json` | Application | Configures Honeygain credentials, browser visibility, and logging. |

### Settings

The subsequent settings are recognised:
| Section | Key | Type | Default | Required | Description |
|---------|-----|------|---------|----------|-------------|
| `botSettings` | `emailAddress` | `string` | Placeholder value | Yes | E-mail address used to authenticate with Honeygain. |
| `botSettings` | `password` | `string` | Placeholder value | Yes | Password used to authenticate with Honeygain. |
| `debugSettings` | `isDebugMode` | `boolean` | `false` | No | Displays the automated browser when enabled; otherwise, automation is headless. |
| `nuciLoggerSettings` | `logFilePath` | `string` | `./logfile.log` | No | Destination for file-based logs. |
| `nuciLoggerSettings` | `minimumLevel` | `string` | `Info` | No | Minimum recorded logging level. |
| `nuciLoggerSettings` | `isFileOutputEnabled` | `boolean` | `true` | No | Activates file-based log production. |

### Reload Behaviour

Settings are bound when the application starts. Restart the application after modifying `appsettings.json` so that the subsequent automation pass uses the revised values.

### Secret Management

Honeygain credentials are read directly from `appsettings.json`. Keep populated configuration files local, restrict their file permissions, and never commit genuine credentials.

## 🧩 Compatibility

| Component | Supported Versions | Notes |
|-----------|--------------------|-------|
| .NET | 10.0 | Required when compiling or executing from source. |
| Linux | `arm`, `arm64`, `x64` | Published v1.1.0 release archives are available. |
| macOS | `arm64`, `x64` | Published v1.1.0 release archives are available. |
| Windows | `arm64`, `x64` | Published v1.1.0 release archives are available. |

## 🔌 Integrations

| Integration | Compatibility | Purpose | Required |
|-------------|---------------|---------|----------|
| Honeygain dashboard | Current web interface | Authenticates, claims an available achievement reward, and opens the Lucky Pot. | Yes |

## 🔐 Authentication and Authorisation

The application authenticates directly with the Honeygain login form by using the e-mail address and password configured in `appsettings.json`. It does not define additional local roles or access scopes.

## 🛡️ Privacy and Data

| Data | Purpose | Storage | Retention | Optional |
|------|---------|---------|-----------|----------|
| Honeygain e-mail address and password | Honeygain authentication | Local `appsettings.json`; transmitted to the Honeygain login form during execution | Retained until the user modifies or deletes the configuration file | No |
| Honeygain e-mail address in operation logs | Associates automation events with the configured account | Configured NuciLog destinations, including `./logfile.log` by default | No automatic retention policy is defined | File storage can be deactivated |

### Data Locations

| Platform or Scope | Location | Contents |
|-------------------|----------|----------|
| Application working directory | `./appsettings.json` | Honeygain credentials and runtime configuration. |
| Application working directory | `./logfile.log` | Operational records when file production is enabled. |

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A Honeygain account
- A Selenium-compatible browser and corresponding WebDriver

### Setup

Clone the repository and restore its dependencies:

```bash
git clone https://github.com/hmlendea/honeygain-pot-opener.git
cd honeygain-pot-opener
dotnet restore
```

Populate the credential placeholders in `appsettings.json` before executing the application.

### Build

```bash
dotnet build --no-restore
```

### Run

```bash
dotnet run --project HoneygainPotOpener.csproj
```

### Continuous Integration

The `.github/workflows/dotnet.yml` workflow restores dependencies, compiles the project, and invokes `dotnet test` with .NET 10.x for pushes and pull requests to `master`.

### Release

The repository includes `release.sh`, which delegates to the upstream deployment script used by the project maintainer.

```bash
bash ./release.sh 1.1.0
```

This script downloads and executes an external release helper from `https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/10.0.sh`.

**Note:** Piping into `bash` is an intensely controversial topic. Please review any external scripts before running them in your environment!

### Dependencies

| Package | Version | Scope | Purpose |
|---------|---------|-------|---------|
| `Microsoft.Extensions.Configuration` | 10.0.5 | Runtime | Provides the configuration abstraction. |
| `Microsoft.Extensions.Configuration.Binder` | 10.0.5 | Runtime | Binds configuration sections to settings objects. |
| `Microsoft.Extensions.Configuration.FileExtensions` | 10.0.5 | Runtime | Supplies file-based configuration support. |
| `Microsoft.Extensions.Configuration.Json` | 10.0.5 | Runtime | Reads settings from `appsettings.json`. |
| `Microsoft.Extensions.DependencyInjection` | 10.0.5 | Runtime | Constructs and resolves application services. |
| `NuciLog` | 1.2.0 | Runtime | Integrates NuciLog with application configuration. |
| `NuciLog.Core` | 2.6.0 | Runtime | Provides structured application logging. |
| `NuciWeb` | 4.0.0 | Runtime | Provides web selector utilities. |
| `NuciWeb.Automation` | 1.0.0 | Runtime | Provides browser automation abstractions. |
| `NuciWeb.Automation.Selenium` | 1.0.1 | Runtime | Implements browser automation through Selenium. |

## 🩺 Troubleshooting

| Symptom | Probable Cause | Resolution |
|---------|----------------|------------|
| Browser initialisation fails | No compatible browser and WebDriver can be initialised. | Install a compatible browser and corresponding WebDriver, then ensure they are accessible to the application. |
| Authentication does not reach the dashboard | Credential placeholders remain, credentials are invalid, or Honeygain revised its login interface. | Verify `botSettings`, enable `debugSettings.isDebugMode`, and inspect the visible browser and logs. |
| No reward is claimed | Honeygain does not present a claimable achievement reward. | No action is required; the application records a warning and continues to the Lucky Pot. |
| The Lucky Pot is not opened | The pot is unavailable or Honeygain revised its dashboard interface. | Confirm availability in the dashboard, enable debug mode, and inspect the operation log. |

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes

## 💝 Project Engagement

Discovered a problem or have a suggestion? [Open an issue](https://github.com/hmlendea/honeygain-pot-opener/issues)!

If you find this project useful, consider [funding it](https://hmlendea.go.ro/funding) or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/funding)

## 📄 License

This project is being distributed under the `GNU General Public License v3.0`.
See [LICENSE](./LICENSE) for further information.
