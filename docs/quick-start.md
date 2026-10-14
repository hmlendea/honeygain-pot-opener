# Quick Start

## Prerequisites

- .NET 10 SDK (for build) or .NET 10 Runtime (for run)
- Chrome or Chromium browser
- Honeygain account

## 1. Clone & Build

```bash
git clone <repository-url>
cd honeygain-pot-opener
dotnet restore HoneygainPotOpener.slnx
dotnet build HoneygainPotOpener.slnx -c Release
```

## 2. Configure

```bash
# Copy template
cp HoneygainPotOpener/appsettings.json HoneygainPotOpener/appsettings.json.local

# Edit with your credentials
# Required: botSettings.emailAddress, botSettings.password
# Optional: debugSettings.isDebugMode, nuciLoggerSettings.*
```

**appsettings.json.example:**
```json
{
  "botSettings": {
    "emailAddress": "your@email.com",
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

**Secure the config:**
```bash
chmod 600 HoneygainPotOpener/appsettings.json.local
```

## 3. Run

```bash
# From project directory
dotnet run --project HoneygainPotOpener/HoneygainPotOpener.csproj -c Release

# Or publish first
dotnet publish HoneygainPotOpener/HoneygainPotOpener.csproj -c Release -r linux-x64 --self-contained -o ./publish
cp HoneygainPotOpener/appsettings.json.local ./publish/appsettings.json
./publish/HoneygainPotOpener
```

## 4. Verify

Check `logfile.log` for:
```
Operation=StartUp Status=Success
Operation=LogIn Status=Started Username="your@email.com"
Operation=LogIn Status=Success Username="your@email.com"
Operation=ClaimReward Status=Started Username="your@email.com"
Operation=ClaimReward Status=Success Username="your@email.com"
Operation=OpenPot Status=Started Username="your@email.com"
Operation=OpenPot Status=Success Username="your@email.com"
```

## 5. Schedule (Daily)

**Linux (cron):**
```bash
crontab -e
# Add:
0 6 * * * /path/to/publish/HoneygainPotOpener >> /var/log/honeygain.log 2>&1
```

**Linux (systemd):**
```ini
# /etc/systemd/system/honeygain-pot-opener.service
[Service]
Type=oneshot
WorkingDirectory=/path/to/publish
ExecStart=/path/to/publish/HoneygainPotOpener

# /etc/systemd/system/honeygain-pot-opener.timer
[Timer]
OnCalendar=daily
Persistent=true
```
```bash
systemctl enable --now honeygain-pot-opener.timer
```

**Windows (Task Scheduler):**
- Create Basic Task → Daily → Start a Program
- Program: `C:\path\to\publish\HoneygainPotOpener.exe`
- Start in: `C:\path\to\publish\`

## Troubleshooting Quick Checks

| Issue | Check |
|-------|-------|
| "NoSuchElementException" | Honeygain UI changed → update selectors |
| "WebDriverTimeoutException" | Chrome not installed or network issue |
| "Fatal" in log | Config error or Chrome missing |
| No log file | Check `isFileOutputEnabled: true` and permissions |

## Related Documentation

- [Configuration](./configuration.md)
- [Build and Deployment](./build-and-deployment.md)
- [Troubleshooting](./troubleshooting.md)
- [FAQ](./faq.md)