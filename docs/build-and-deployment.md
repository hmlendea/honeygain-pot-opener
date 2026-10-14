# Build and Deployment

## Build

### Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 10.0 | `dotnet --version` |
| Git | Any | `git --version` |

### Commands

```bash
# Restore dependencies
dotnet restore HoneygainPotOpener.slnx

# Build (Debug)
dotnet build HoneygainPotOpener.slnx -c Debug

# Build (Release)
dotnet build HoneygainPotOpener.slnx -c Release

# Run tests
dotnet test HoneygainPotOpener.slnx -c Release --no-build

# Publish (framework-dependent)
dotnet publish HoneygainPotOpener/HoneygainPotOpener.csproj -c Release -o ./publish

# Publish (self-contained, Linux x64)
dotnet publish HoneygainPotOpener/HoneygainPotOpener.csproj -c Release -r linux-x64 --self-contained -o ./publish/linux-x64

# Publish (self-contained, Windows x64)
dotnet publish HoneygainPotOpener/HoneygainPotOpener.csproj -c Release -r win-x64 --self-contained -o ./publish/win-x64
```

### Build Output

**Framework-dependent:**
```
publish/
├── HoneygainPotOpener.dll
├── HoneygainPotOpener.deps.json
├── HoneygainPotOpener.runtimeconfig.json
├── appsettings.json
└── [NuGet dependencies...]
```

**Self-contained:**
```
publish/linux-x64/
├── HoneygainPotOpener          # Native executable
├── HoneygainPotOpener.dll
├── appsettings.json
└── [All .NET runtime + dependencies...]
```

## Configuration for Deployment

### Required Files

| File | Source | Notes |
|------|--------|-------|
| `appsettings.json` | Template in repo | Replace placeholders with real values |
| `HoneygainPotOpener` (or `.exe`) | Build output | Executable |
| `*.dll`, `*.json`, `*.deps.json` | Build output | All files in publish directory |

### appsettings.json Template (Repository)

```json
{
  "botSettings": {
    "emailAddress": "[[HONEYGAIN_EMAIL_ADDRESS]]",
    "password": "[[HONEYGAIN_PASSWORD]]"
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

### Deployment appsettings.json (Production)

```json
{
  "botSettings": {
    "emailAddress": "actual@email.com",
    "password": "actual-password"
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

**File Permissions:**
```bash
chmod 600 appsettings.json
chmod 755 HoneygainPotOpener  # Linux executable
```

## Runtime Requirements

### Linux

```bash
# Ubuntu/Debian
apt-get update && apt-get install -y \
    chromium-browser \
    libgtk-3-0 \
    libnss3 \
    libasound2 \
    libxss1 \
    libgconf-2-4 \
    libx11-xcb1 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxi6 \
    libxtst6 \
    libnss3 \
    libcups2 \
    libxrandr2 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libpango-1.0-0 \
    libcairo2 \
    libgdk-pixbuf2.0-0

# Verify Chrome
chromium-browser --version
```

### Windows

- Chrome or Edge installed (WebDriverInitialiser auto-discovers)
- No additional runtime if self-contained

### macOS

```bash
brew install --cask google-chrome
```

## Deployment Models

### 1. Scheduled Task (Cron / systemd Timer / Task Scheduler)

**Linux (systemd):**

```ini
# /etc/systemd/system/honeygain-pot-opener.service
[Unit]
Description=Honeygain Pot Opener
After=network.target

[Service]
Type=oneshot
User=honeygain
WorkingDirectory=/opt/honeygain-pot-opener
ExecStart=/opt/honeygain-pot-opener/HoneygainPotOpener
Environment=DOTNET_ENVIRONMENT=Production

# /etc/systemd/system/honeygain-pot-opener.timer
[Unit]
Description=Run Honeygain Pot Opener daily

[Timer]
OnCalendar=daily
Persistent=true

[Install]
WantedBy=timers.target
```

```bash
systemctl daemon-reload
systemctl enable --now honeygain-pot-opener.timer
```

**Linux (cron):**

```bash
# crontab -e for honeygain user
0 6 * * * /opt/honeygain-pot-opener/HoneygainPotOpener >> /var/log/honeygain-pot-opener.log 2>&1
```

**Windows (Task Scheduler):**
- Trigger: Daily at 6:00 AM
- Action: `C:\opt\honeygain-pot-opener\HoneygainPotOpener.exe`
- Start in: `C:\opt\honeygain-pot-opener\`
- Run whether user logged on or not

### 2. Container (Docker)

**Dockerfile:**

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY HoneygainPotOpener.slnx .
COPY HoneygainPotOpener/HoneygainPotOpener.csproj HoneygainPotOpener/
RUN dotnet restore HoneygainPotOpener.slnx
COPY . .
RUN dotnet publish HoneygainPotOpener/HoneygainPotOpener.csproj -c Release -r linux-x64 --self-contained -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0
WORKDIR /app
RUN apt-get update && apt-get install -y chromium && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
COPY appsettings.json .
RUN chmod 600 appsettings.json && chmod +x HoneygainPotOpener
ENTRYPOINT ["./HoneygainPotOpener"]
```

**Build & Run:**

```bash
docker build -t honeygain-pot-opener .
docker run --rm -v $(pwd)/appsettings.json:/app/appsettings.json honeygain-pot-opener
```

### 3. Manual Execution

```bash
# Linux/macOS
./HoneygainPotOpener

# Windows
HoneygainPotOpener.exe
```

## CI/CD Pipeline

### GitHub Actions (`.github/workflows/dotnet.yml`)

```yaml
name: .NET Build and Test

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore
        run: dotnet restore HoneygainPotOpener.slnx
      - name: Build
        run: dotnet build HoneygainPotOpener.slnx -c Release --no-restore
      - name: Test
        run: dotnet test HoneygainPotOpener.slnx -c Release --no-build --verbosity normal
```

### Release Workflow (`.github/workflows/github-release.yml`)

```yaml
name: GitHub Release

on:
  push:
    tags: ['v*']

jobs:
  release:
    runs-on: ubuntu-latest
    permissions:
      contents: write
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore
        run: dotnet restore HoneygainPotOpener.slnx
      - name: Build and Publish (Linux)
        run: dotnet publish HoneygainPotOpener/HoneygainPotOpener.csproj -c Release -r linux-x64 --self-contained -o ./publish/linux-x64
      - name: Build and Publish (Windows)
        run: dotnet publish HoneygainPotOpener/HoneygainPotOpener.csproj -c Release -r win-x64 --self-contained -o ./publish/win-x64
      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            ./publish/linux-x64/*
            ./publish/win-x64/*
```

## Verification

### Post-Deployment Checks

```bash
# 1. Verify executable runs
./HoneygainPotOpener --help  # No help, but should not crash immediately

# 2. Verify config loaded
# Check logfile.log for "Operation=StartUp Status=Success"

# 3. Verify Chrome/Chromium accessible
chromium-browser --version

# 4. Verify file permissions
ls -la appsettings.json logfile.log
```

### Health Check (Manual)

```bash
# Run once and check exit code
./HoneygainPotOpener
echo "Exit code: $?"

# Check log
tail -20 logfile.log
```

**Expected Log Pattern:**
```
Operation=StartUp Status=Success
Operation=LogIn Status=Started Username="..."
Operation=LogIn Status=Success Username="..."
Operation=ClaimReward Status=Started Username="..."
Operation=ClaimReward Status=Success/Warn Username="..."
Operation=OpenPot Status=Started Username="..."
Operation=OpenPot Status=Success Username="..."
```

## Rollback

### Binary Rollback

```bash
# Keep previous version
mv /opt/honeygain-pot-opener /opt/honeygain-pot-opener.v1
mv /opt/honeygain-pot-opener.v2 /opt/honeygain-pot-opener

# Or with Docker
docker tag honeygain-pot-opener:latest honeygain-pot-opener:previous
docker tag honeygain-pot-opener:v1.2.3 honeygain-pot-opener:latest
```

### Config Rollback

```bash
cp appsettings.json.backup appsettings.json
```

## Monitoring

### Log-Based Alerts

| Pattern | Alert | Action |
|---------|-------|--------|
| `Operation=LogIn Status=Failure` | Critical | Check credentials, Honeygain status |
| `Operation=OpenPot Status=Failure` | Warning | Check Honeygain UI changes |
| `Fatal` | Critical | Process crashed — check driver, config |
| No log file update for 48h | Warning | Scheduler not running |

### Metrics (Future)

- Run duration
- Success/failure rates per operation
- Selenium command latencies

## Related Documentation

- [Dependencies](./dependencies.md)
- [Configuration](./configuration.md)
- [Security](./security.md)
- [Testing](./testing.md)
- [Architecture](../architecture.md)