# API Usage Examples

## Overview

This application has **no public HTTP API, CLI, or library interface**. It is a standalone console application invoked by an external scheduler.

The "API" consists of internal service contracts used for dependency injection and testing.

## Internal Service Usage

### Running the Bot (Program.cs)

```csharp
using HoneygainPotOpener.Configuration;
using HoneygainPotOpener.Logging;
using HoneygainPotOpener.Processors;
using HoneygainPotOpener.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuciLog;
using NuciWeb.Automation;
using NuciWeb.Automation.Selenium;
using OpenQA.Selenium;

// 1. Load configuration
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

var botSettings = new BotSettings();
var debugSettings = new DebugSettings();
config.Bind(nameof(BotSettings), botSettings);
config.Bind(nameof(DebugSettings), debugSettings);

// 2. Initialise WebDriver
var webDriver = WebDriverInitialiser.Initialise(debugSettings.IsHeadless);

// 3. Configure DI
var services = new ServiceCollection();
services.AddSingleton(botSettings);
services.AddSingleton(debugSettings);
services.AddNuciLoggerSettings(config).AddSingleton<ILogger, NuciLogger>();
services.AddSingleton<IWebProcessor>(_ => new SeleniumWebProcessor(webDriver));
services.AddTransient<IHoneygainProcessor, HoneygainProcessor>();
services.AddSingleton<IBotService, BotService>();

var serviceProvider = services.BuildServiceProvider();

// 4. Run
var logger = serviceProvider.GetRequiredService<ILogger>();
try
{
    logger.Info(Operation.StartUp, OperationStatus.Success);
    serviceProvider.GetRequiredService<IBotService>().Run();
}
catch (AggregateException ex)
{
    foreach (var inner in ex.InnerExceptions)
        logger.Fatal(Operation.Unknown, OperationStatus.Failure, inner);
}
catch (Exception ex)
{
    logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
}
finally
{
    webDriver.Quit();
}
```

### Using IHoneygainProcessor Directly

```csharp
// Requires: IWebProcessor, ILogger
var processor = new HoneygainProcessor(webProcessor, logger);

// Log in
processor.LogIn();

// Claim reward (throws if not available)
processor.ClaimReward();

// Open Lucky Pot (throws if not available)
processor.OpenPot();
```

### Using IWebProcessor Directly

```csharp
// Navigate
webProcessor.GoToUrl("https://example.com");

// Wait for element
webProcessor.WaitForElementToBeVisible(By.Id("my-element"), 10);

// Check visibility
if (webProcessor.IsElementVisible(By.Id("my-element")))
{
    // Interact
    webProcessor.SetText(By.Id("input"), "text");
    webProcessor.Click(By.Id("button"));
    webProcessor.DoubleClick(By.Id("double-click-target"));
}

// Clean up
webProcessor.Quit();
```

## Testing Usage (Moq)

### Mocking IWebProcessor

```csharp
var mockWebProcessor = new Mock<IWebProcessor>();
var mockLogger = new Mock<ILogger>();

// Setup expectations
mockWebProcessor.Setup(p => p.GoToUrl("https://dashboard.honeygain.com/login"));
mockWebProcessor.Setup(p => p.WaitForElementToBeVisible(By.Id("onetrust-accept-btn-handler"), 5));
mockWebProcessor.Setup(p => p.SetText(By.Id("email"), "test@example.com"));
mockWebProcessor.Setup(p => p.SetText(By.Id("password"), "password123"));
mockWebProcessor.Setup(p => p.Click(By.Id("login-btn")));
mockWebProcessor.Setup(p => p.WaitForElementToBeVisible(By.XPath("//a[contains(@href, '/earnings')]"), 10));

// Create processor
var processor = new HoneygainProcessor(mockWebProcessor.Object, mockLogger.Object);

// Execute
processor.LogIn();

// Verify
mockWebProcessor.VerifyAll();
mockWebProcessor.VerifyNoOtherCalls();
```

### Mocking IHoneygainProcessor

```csharp
var mockProcessor = new Mock<IHoneygainProcessor>();
var mockLogger = new Mock<ILogger>();

var botService = new BotService(mockProcessor.Object, mockLogger.Object, botSettings);

// Execute
botService.Run();

// Verify sequence
mockProcessor.Verify(p => p.LogIn(), Times.Once);
mockProcessor.Verify(p => p.ClaimReward(), Times.Once);
mockProcessor.Verify(p => p.OpenPot(), Times.Once);
```

## Configuration Usage

### Reading Settings

```csharp
// In any service with BotSettings injected
public class MyService
{
    private readonly BotSettings _settings;

    public MyService(BotSettings settings)
    {
        _settings = settings;
    }

    public void DoSomething()
    {
        var email = _settings.EmailAddress;
        var password = _settings.Password;
        // Use credentials...
    }
}
```

### Debug Settings

```csharp
// In any service with DebugSettings injected
public class MyService
{
    private readonly DebugSettings _settings;

    public MyService(DebugSettings settings)
    {
        _settings = settings;
    }

    public void ConfigureBrowser()
    {
        var headless = _settings.IsHeadless;  // Computed: !IsDebugMode
        // Use headless flag...
    }
}
```

## Logging Usage

### Structured Logging

```csharp
// In any service with ILogger injected
public class MyService
{
    private readonly ILogger _logger;

    public MyService(ILogger logger)
    {
        _logger = logger;
    }

    public void DoOperation()
    {
        var logInfos = new[]
        {
            new LogInfo(MyLogInfoKey.Username, "user@example.com")
        };

        _logger.Info(MyOperation.LogIn, OperationStatus.Started, logInfos);
        try
        {
            // ... do work ...
            _logger.Info(MyOperation.LogIn, OperationStatus.Success, logInfos);
        }
        catch (Exception ex)
        {
            _logger.Error(MyOperation.LogIn, OperationStatus.Failure, ex, logInfos);
            _logger.Info(MyOperation.LogIn, OperationStatus.Success, logInfos);  // Always
        }
    }
}
```

## Scheduler Integration

### Cron Entry

```bash
# Run daily at 6:00 AM
0 6 * * * /opt/honeygain-pot-opener/HoneygainPotOpener >> /var/log/honeygain.log 2>&1
```

### systemd Service + Timer

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

### Kubernetes CronJob

```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: honeygain-pot-opener
spec:
  schedule: "0 6 * * *"
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: honeygain-pot-opener
            image: your-registry/honeygain-pot-opener:latest
            env:
            - name: HONEYGAIN_EMAIL
              valueFrom:
                secretKeyRef:
                  name: honeygain-secrets
                  key: email
            - name: HONEYGAIN_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: honeygain-secrets
                  key: password
            volumeMounts:
            - name: config
              mountPath: /app/appsettings.json
              subPath: appsettings.json
          volumes:
          - name: config
            configMap:
              name: honeygain-config
          restartPolicy: OnFailure
```

## Docker Usage

### Build

```bash
docker build -t honeygain-pot-opener .
```

### Run

```bash
# With config file mounted
docker run --rm \
  -v $(pwd)/appsettings.json:/app/appsettings.json:ro \
  honeygain-pot-opener
```

### Docker Compose

```yaml
version: '3.8'
services:
  honeygain-pot-opener:
    build: .
    volumes:
      - ./appsettings.json:/app/appsettings.json:ro
    environment:
      - DOTNET_ENVIRONMENT=Production
```

## Related Documentation

- [API Reference](../api-reference/INDEX.md)
- [Components: Application Services](../components/application-services.md)
- [Components: Integration Models](../components/integration-models.md)
- [Build and Deployment](../build-and-deployment.md)
- [Testing](../testing.md)