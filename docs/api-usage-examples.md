# API Usage Examples

## Overview

This application has **no public HTTP API, CLI, or library interface**. It is a standalone console application invoked by an external scheduler.

The "API" consists of internal service contracts used for dependency injection and testing.

## Internal Service Usage

### Running the Bot (Program.cs)

```csharp
using HoneygainPotOpener.Configuration;
using HoneygainPotOpener.Processors;
using HoneygainPotOpener.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuciLog;
using NuciWeb.Automation;
using NuciWeb.Automation.Selenium;
using OpenQA.Selenium;

// 1. Load configuration
IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

BotSettings botSettings = new();
DebugSettings debugSettings = new();
config.Bind(nameof(BotSettings), botSettings);
config.Bind(nameof(DebugSettings), debugSettings);

// 2. Reject incomplete credentials before creating a browser
if (!botSettings.HasValidCredentials)
{
    Console.Error.WriteLine("The Honeygain account credentials are missing from the configuration.");
    Environment.ExitCode = 1;
    return;
}

// 3. Initialise WebDriver
IWebDriver webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode);

// 4. Configure DI
IServiceProvider serviceProvider = new ServiceCollection()
    .AddNuciLoggerSettings(config)
    .AddSingleton(botSettings)
    .AddSingleton(debugSettings)
    .AddSingleton<ILogger, NuciLogger>()
    .AddSingleton(webDriver)
    .AddTransient<IWebProcessor, SeleniumWebProcessor>()
    .AddSingleton<IHoneygainProcessor, HoneygainProcessor>()
    .AddSingleton<IBotService, BotService>()
    .BuildServiceProvider();

// 5. Run and derive the exit code from the execution report
ILogger logger = serviceProvider.GetRequiredService<ILogger>();

try
{
    logger.Info(Operation.StartUp, "Application started");
    ExecutionReport report = serviceProvider.GetRequiredService<IBotService>().Run();
    Environment.ExitCode = report.IsSuccessful ? 0 : 1;
}
catch (AggregateException exception)
{
    foreach (Exception innerException in exception.InnerExceptions)
    {
        logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
    }

    Environment.ExitCode = 1;
}
catch (Exception exception)
{
    logger.Fatal(Operation.Unknown, OperationStatus.Failure, exception);
    Environment.ExitCode = 1;
}
finally
{
    webDriver.Quit();
}
```

### Using IHoneygainProcessor Directly

```csharp
// Requires: IWebProcessor, BotSettings
HoneygainProcessor processor = new(webProcessor, botSettings);

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
webProcessor.WaitForElementToBeVisible(Select.ById("my-element"));

// Check visibility
if (webProcessor.IsElementVisible(Select.ById("my-element")))
{
    // Interact
    webProcessor.SetText(Select.ById("input"), "text");
    webProcessor.Click(Select.ById("button"));
}

// Clean up
webProcessor.Quit();
```

## Testing Usage (Moq)

### Mocking IWebProcessor

```csharp
Mock<IWebProcessor> mockWebProcessor = new();
BotSettings settings = new()
{
    EmailAddress = "test@example.com",
    Password = "password123"
};

// Setup expectations using the processor selector properties
mockWebProcessor.Setup(p => p.GoToUrl(HoneygainProcessor.LogInUrl));
mockWebProcessor.Setup(p => p.WaitForElementToBeVisible(
    Select.ByXPath("//img[@alt='cookie consent']/../div/button[2]")));
mockWebProcessor.Setup(p => p.SetText(Select.ById("email"), settings.EmailAddress));
mockWebProcessor.Setup(p => p.SetText(Select.ById("password"), settings.Password));
mockWebProcessor.Setup(p => p.Click(Select.ByXPath("//form/button")));
mockWebProcessor.Setup(p => p.WaitForElementToBeVisible(Select.ById("traffic-legend-container")));

// Create processor
HoneygainProcessor processor = new(mockWebProcessor.Object, settings);

// Execute
processor.LogIn();

// Verify
mockWebProcessor.VerifyAll();
mockWebProcessor.VerifyNoOtherCalls();
```

### Mocking IHoneygainProcessor

```csharp
Mock<IHoneygainProcessor> mockProcessor = new();
Mock<ILogger> mockLogger = new();
BotSettings botSettings = new()
{
    EmailAddress = "test@example.com",
    Password = "password123"
};

BotService botService = new(mockProcessor.Object, botSettings, mockLogger.Object);

// Execute
ExecutionReport report = botService.Run();

// Verify sequence and outcome
mockProcessor.Verify(p => p.LogIn(), Times.Once);
mockProcessor.Verify(p => p.ClaimReward(), Times.Once);
mockProcessor.Verify(p => p.OpenPot(), Times.Once);
Assert.That(report.IsSuccessful, Is.True);
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
        }
        catch (Exception exception)
        {
            _logger.Error(MyOperation.LogIn, OperationStatus.Failure, exception, logInfos);
            return;
        }

        _logger.Info(MyOperation.LogIn, OperationStatus.Success, logInfos);
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