[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/fund.html) [![Latest Release](https://img.shields.io/github/v/release/hmlendea/honeygain-pot-opener)](https://github.com/hmlendea/honeygain-pot-opener/releases/latest) [![Build Status](https://github.com/hmlendea/honeygain-pot-opener/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/honeygain-pot-opener/actions/workflows/dotnet.yml)

# Honeygain Pot Opener

Small .NET console app that logs into Honeygain and attempts to open the daily Lucky Pot automatically.

## What It Does

The app performs the following flow:

1. Opens Honeygain login page.
2. Accepts cookies if the cookie banner is shown.
3. Logs in with the configured credentials.
4. Waits for dashboard content.
5. Clicks the "Open Lucky Pot" button.

## Requirements

- .NET SDK (project targets `net10.0`)
- A supported Selenium-compatible browser + driver available on your machine (the app uses `NuciWeb.Automation.Selenium` to initialize a WebDriver)
- A Honeygain account

## Configuration

Edit `appsettings.json`:

```json
{
	"botSettings": {
		"emailAddress": "your-email@example.com",
		"password": "your-password"
	},
	"debugSettings": {
		"isDebugMode": false
	},
	"nuciLoggerSettings": {
		"logFilePath": "./logfile.log",
		"minimumLevel": "Info",
		"isFileOutputEnabled": true
	}
}
```

### Config Notes

- `botSettings.emailAddress`: Honeygain email
- `botSettings.password`: Honeygain password
- `debugSettings.isDebugMode`:
	- `true` -> debug mode (non-headless)
	- `false` -> headless mode
- `nuciLoggerSettings.*`: controls logging output/verbosity

## Run Locally

```bash
dotnet restore
dotnet run
```

## Build

```bash
dotnet build -c Release
```

## Publish (optional)

```bash
dotnet publish -c Release -o ./publish
```

There is also a helper script:

```bash
./release.sh
```

This script downloads and executes an external release helper from:
`https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/10.0.sh`

Piping into bash is an intensely controversial topic. Please review external scripts before running them in your environment.

## Logging

- Startup and operation logs are emitted through `NuciLog`.
- Default file output path is `./logfile.log`.
- Key operations logged: login and pot opening.

## Troubleshooting

- If login fails:
	- Verify credentials in `appsettings.json`.
	- Enable debug mode (`"isDebugMode": true`) to watch browser actions.
- If the pot is not opened:
	- The Lucky Pot may be unavailable at that time.
	- UI or selector changes on Honeygain may require code updates.
- If browser initialization fails:
	- Ensure a supported browser/driver is installed and accessible.

## Contributing

Contributions are welcome.

When contributing:

- keep the project cross-platform
- preserve the existing public API unless a breaking change is intentional
- keep changes focused and consistent with the current coding style
- update documentation when behavior changes
- include tests for new behavior when a test project is available

## Security Notes

- Do not commit real credentials.
- Prefer secret injection at runtime or local-only config handling.

## Legal / Terms

Use this project responsibly and ensure your usage complies with Honeygain terms and policies.

## License

Licensed under the GNU General Public License v3.0 or later.
See [LICENSE](./LICENSE) for details.
