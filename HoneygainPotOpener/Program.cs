using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciLog;
using NuciLog.Core;

using NuciWeb.Automation;
using NuciWeb.Automation.Selenium;

using OpenQA.Selenium;

using HoneygainPotOpener.Configuration;
using HoneygainPotOpener.Processors;
using HoneygainPotOpener.Service;

namespace HoneygainPotOpener
{
    public sealed class Program
    {
        private static int SuccessExitCode => 0;

        private static int FailureExitCode => 1;

        private static void Main()
        {
            IConfiguration config = LoadConfiguration();

            BotSettings botSettings = new();
            DebugSettings debugSettings = new();
            config.Bind(nameof(BotSettings), botSettings);
            config.Bind(nameof(DebugSettings), debugSettings);

            if (!botSettings.HasValidCredentials)
            {
                Console.Error.WriteLine(
                    "The Honeygain account credentials are missing from the configuration.");

                Environment.ExitCode = FailureExitCode;

                return;
            }

            IWebDriver webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode);

            try
            {
                IServiceProvider serviceProvider = BuildServiceProvider(config, botSettings, debugSettings, webDriver);

                RunBot(serviceProvider);
            }
            finally
            {
                webDriver.Quit();
            }
        }

        private static void RunBot(IServiceProvider serviceProvider)
        {
            ILogger logger = serviceProvider.GetRequiredService<ILogger>();

            logger.Info(Operation.StartUp, "Application started");

            try
            {
                ExecutionReport report = serviceProvider
                    .GetRequiredService<IBotService>()
                    .Run();

                Environment.ExitCode = ResolveExitCode(report);
            }
            catch (AggregateException exception)
            {
                LogFatalExceptions(logger, exception);

                Environment.ExitCode = FailureExitCode;
            }
            catch (Exception exception)
            {
                logger.Fatal(Operation.Unknown, OperationStatus.Failure, exception);

                Environment.ExitCode = FailureExitCode;
            }
        }

        private static int ResolveExitCode(ExecutionReport report)
        {
            int exitCode = FailureExitCode;

            if (report.IsSuccessful)
            {
                exitCode = SuccessExitCode;
            }

            return exitCode;
        }

        private static void LogFatalExceptions(ILogger logger, AggregateException exception)
        {
            foreach (Exception innerException in exception.InnerExceptions)
            {
                logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
            }
        }

        private static IServiceProvider BuildServiceProvider(
            IConfiguration config,
            BotSettings botSettings,
            DebugSettings debugSettings,
            IWebDriver webDriver) => new ServiceCollection()
                .AddNuciLoggerSettings(config)
                .AddSingleton(botSettings)
                .AddSingleton(debugSettings)
                .AddSingleton<ILogger, NuciLogger>()
                .AddSingleton(webDriver)
                .AddTransient<IWebProcessor, SeleniumWebProcessor>()
                .AddSingleton<IHoneygainProcessor, HoneygainProcessor>()
                .AddSingleton<IBotService, BotService>()
                .BuildServiceProvider();

        private static IConfiguration LoadConfiguration() => new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .Build();
    }
}
