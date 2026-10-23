using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using NuciLog.Core;

using NuciWeb;
using NuciWeb.Automation;

using NUnit.Framework;

using HoneygainPotOpener.Configuration;
using HoneygainPotOpener.Logging;
using HoneygainPotOpener.Processors;
using HoneygainPotOpener.Service;

namespace HoneygainPotOpener.IntegrationTests.Service
{
    [TestFixture]
    public sealed class BotServiceTests
    {
        private static string CookieSelector =>
            Select.ByXPath("//img[@alt='cookie consent']/../div/button[2]");

        private static string RewardSelector =>
            Select.ByXPath("//span[text()='Claim reward']/../../../../../button");

        private static string PotSelector =>
            Select.ByXPath("//span[text()='Open Lucky Pot']/../../..");

        private Mock<IWebProcessor> webProcessor = null!;
        private Mock<ILogger> logger = null!;
        private List<string> interactions = null!;
        private ServiceProvider serviceProvider = null!;
        private IBotService botService = null!;

        [SetUp]
        public void SetUp()
        {
            webProcessor = new(MockBehavior.Strict);
            logger = new(MockBehavior.Strict);
            interactions = [];
            serviceProvider = BuildServiceProvider(webProcessor.Object, logger.Object);
            botService = serviceProvider.GetRequiredService<IBotService>();
            ConfigureLogging();
        }

        [TearDown]
        public void TearDown() => serviceProvider.Dispose();

        [TestCase(true, true, true)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, true)]
        [TestCase(false, true, false)]
        [TestCase(false, false, true)]
        [TestCase(false, false, false)]
        public void GivenDashboardAvailability_WhenRunning_ThenThePipelineHandlesEveryOperation(
            bool hasCookies,
            bool hasReward,
            bool hasPot)
        {
            ConfigureLogin(hasCookies);
            ConfigureDashboard(hasReward, hasPot);
            List<string> expectedInteractions = BuildLoginInteractions(hasCookies);
            expectedInteractions.Add("Inspect reward");

            if (hasReward)
            {
                expectedInteractions.Add("Claim reward");
            }

            expectedInteractions.AddRange(["Wait for pot", "Inspect pot"]);

            if (hasPot)
            {
                expectedInteractions.Add("Open pot");
            }

            botService.Run();

            Assert.That(interactions, Is.EqualTo(expectedInteractions));
            VerifyLogging(false, !hasReward, !hasPot);
            webProcessor.VerifyAll();
        }

        [Test]
        public void GivenNavigationFailure_WhenRunning_ThenRewardsAndPotAreStillProcessed()
        {
            InvalidOperationException exception = new("The dashboard is unavailable.");
            webProcessor.Setup(processor => processor.GoToUrl(HoneygainProcessor.LogInUrl))
                .Callback(() => interactions.Add("Navigate"))
                .Throws(exception);
            ConfigureDashboard(true, true);

            Assert.That(() => botService.Run(), Throws.Nothing);
            Assert.That(
                interactions,
                Is.EqualTo(new[]
                {
                    "Navigate", "Inspect reward", "Claim reward",
                    "Wait for pot", "Inspect pot", "Open pot"
                }));
            VerifyLogging(true, false, false);
            VerifyError(MyOperation.LogIn, exception);
            webProcessor.VerifyAll();
        }

        [Test]
        public void GivenRewardClickFailure_WhenRunning_ThenThePotIsStillOpened()
        {
            InvalidOperationException exception = new("The reward button is unavailable.");
            ConfigureLogin(false);
            ConfigureDashboard(true, true);
            webProcessor.Setup(processor => processor.Click(RewardSelector))
                .Callback(() => interactions.Add("Claim reward"))
                .Throws(exception);
            List<string> expectedInteractions = BuildLoginInteractions(false);
            expectedInteractions.AddRange(
                ["Inspect reward", "Claim reward", "Wait for pot", "Inspect pot", "Open pot"]);

            Assert.That(() => botService.Run(), Throws.Nothing);
            Assert.That(interactions, Is.EqualTo(expectedInteractions));
            VerifyLogging(false, true, false);
            logger.Verify(log => log.Warn(
                It.Is<Operation>(operation => operation.Name == "ClaimReward"),
                It.Is<OperationStatus>(status => status.Name == "Failure"),
                exception,
                It.IsAny<IEnumerable<LogInfo>>()), Times.Once);
            webProcessor.VerifyAll();
        }

        [Test]
        public void GivenPotWaitFailure_WhenRunning_ThenTheFailureIsLoggedWithoutClicking()
        {
            InvalidOperationException exception = new("The pot did not become visible.");
            ConfigureLogin(false);
            webProcessor.Setup(processor => processor.IsElementVisible(RewardSelector))
                .Callback(() => interactions.Add("Inspect reward"))
                .Returns(false);
            webProcessor.Setup(processor => processor.WaitForElementToBeVisible(PotSelector))
                .Callback(() => interactions.Add("Wait for pot"))
                .Throws(exception);
            List<string> expectedInteractions = BuildLoginInteractions(false);
            expectedInteractions.AddRange(["Inspect reward", "Wait for pot"]);

            Assert.That(() => botService.Run(), Throws.Nothing);
            Assert.That(interactions, Is.EqualTo(expectedInteractions));
            VerifyLogging(false, true, true);
            VerifyError(MyOperation.OpenPot, exception);
            webProcessor.VerifyAll();
        }

        private void ConfigureLogin(bool hasCookies)
        {
            webProcessor.Setup(processor => processor.GoToUrl(HoneygainProcessor.LogInUrl))
                .Callback(() => interactions.Add("Navigate"));
            webProcessor.Setup(processor => processor.WaitForElementToBeVisible(CookieSelector))
                .Callback(() => interactions.Add("Wait for cookies"));
            webProcessor.Setup(processor => processor.IsElementVisible(CookieSelector))
                .Callback(() => interactions.Add("Inspect cookies"))
                .Returns(hasCookies);

            if (hasCookies)
            {
                webProcessor.Setup(processor => processor.Click(CookieSelector))
                    .Callback(() => interactions.Add("Accept cookies"));
            }

            webProcessor.Setup(processor => processor.SetText(Select.ById("email"), "dummy@example.com"))
                .Callback(() => interactions.Add("Enter email"));
            webProcessor.Setup(processor => processor.SetText(Select.ById("password"), "dummy-password"))
                .Callback(() => interactions.Add("Enter password"));
            webProcessor.Setup(processor => processor.Click(Select.ByXPath("//form/button")))
                .Callback(() => interactions.Add("Submit login"));
            webProcessor.Setup(processor => processor.WaitForElementToBeVisible(
                Select.ById("traffic-legend-container")))
                .Callback(() => interactions.Add("Wait for dashboard"));
        }

        private void ConfigureDashboard(bool hasReward, bool hasPot)
        {
            webProcessor.Setup(processor => processor.IsElementVisible(RewardSelector))
                .Callback(() => interactions.Add("Inspect reward"))
                .Returns(hasReward);
            webProcessor.Setup(processor => processor.WaitForElementToBeVisible(PotSelector))
                .Callback(() => interactions.Add("Wait for pot"));
            webProcessor.Setup(processor => processor.IsElementVisible(PotSelector))
                .Callback(() => interactions.Add("Inspect pot"))
                .Returns(hasPot);

            if (hasReward)
            {
                webProcessor.Setup(processor => processor.Click(RewardSelector))
                    .Callback(() => interactions.Add("Claim reward"));
            }

            if (hasPot)
            {
                webProcessor.Setup(processor => processor.Click(PotSelector))
                    .Callback(() => interactions.Add("Open pot"));
            }
        }

        private void ConfigureLogging()
        {
            logger.Setup(log => log.Info(
                It.IsAny<Operation>(), It.IsAny<OperationStatus>(), It.IsAny<IEnumerable<LogInfo>>()));
            logger.Setup(log => log.Warn(
                It.IsAny<Operation>(),
                It.IsAny<OperationStatus>(),
                It.IsAny<Exception>(),
                It.IsAny<IEnumerable<LogInfo>>()));
            logger.Setup(log => log.Error(
                It.IsAny<Operation>(),
                It.IsAny<OperationStatus>(),
                It.IsAny<Exception>(),
                It.IsAny<IEnumerable<LogInfo>>()));
        }

        private void VerifyLogging(bool hasLoginFailure, bool hasRewardFailure, bool hasPotFailure)
        {
            Operation[] operations = [MyOperation.LogIn, MyOperation.ClaimReward, MyOperation.OpenPot];

            foreach (Operation operation in operations)
            {
                logger.Verify(log => log.Info(
                    It.Is<Operation>(actual => actual.Name == operation.Name),
                    It.Is<OperationStatus>(status => status.Name == "Started"),
                    It.IsAny<IEnumerable<LogInfo>>()), Times.Once);
                logger.Verify(log => log.Info(
                    It.Is<Operation>(actual => actual.Name == operation.Name),
                    It.Is<OperationStatus>(status => status.Name == "Success"),
                    It.IsAny<IEnumerable<LogInfo>>()), Times.Once);
            }

            logger.Verify(log => log.Warn(
                It.Is<Operation>(operation => operation.Name == "ClaimReward"),
                It.Is<OperationStatus>(status => status.Name == "Failure"),
                It.IsAny<Exception>(),
                It.IsAny<IEnumerable<LogInfo>>()), Times.Exactly(Convert.ToInt32(hasRewardFailure)));
            VerifyErrorCount(MyOperation.LogIn, hasLoginFailure);
            VerifyErrorCount(MyOperation.OpenPot, hasPotFailure);
            logger.VerifyNoOtherCalls();
        }

        private void VerifyErrorCount(Operation operation, bool hasFailure)
            => logger.Verify(log => log.Error(
                It.Is<Operation>(actual => actual.Name == operation.Name),
                It.Is<OperationStatus>(status => status.Name == "Failure"),
                It.IsAny<Exception>(),
                It.IsAny<IEnumerable<LogInfo>>()), Times.Exactly(Convert.ToInt32(hasFailure)));

        private void VerifyError(Operation operation, Exception exception)
            => logger.Verify(log => log.Error(
                It.Is<Operation>(actual => actual.Name == operation.Name),
                It.Is<OperationStatus>(status => status.Name == "Failure"),
                exception,
                It.IsAny<IEnumerable<LogInfo>>()), Times.Once);

        private static List<string> BuildLoginInteractions(bool hasCookies)
        {
            List<string> expectedInteractions = ["Navigate", "Wait for cookies", "Inspect cookies"];

            if (hasCookies)
            {
                expectedInteractions.Add("Accept cookies");
            }

            expectedInteractions.AddRange(
                ["Enter email", "Enter password", "Submit login", "Wait for dashboard"]);

            return expectedInteractions;
        }

        private static ServiceProvider BuildServiceProvider(IWebProcessor webProcessor, ILogger logger)
        {
            using MemoryStream settingsStream = new(Encoding.UTF8.GetBytes("""
                {
                    "BotSettings": {
                        "EmailAddress": "dummy@example.com",
                        "Password": "dummy-password"
                    }
                }
                """));
            using ConfigurationManager configuration = new();
            configuration.AddJsonStream(settingsStream);
            BotSettings settings = new();
            configuration.Bind(nameof(BotSettings), settings);

            return new ServiceCollection()
                .AddSingleton(settings)
                .AddSingleton(webProcessor)
                .AddSingleton(logger)
                .AddSingleton<IHoneygainProcessor, HoneygainProcessor>()
                .AddSingleton<IBotService, BotService>()
                .BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });
        }
    }
}