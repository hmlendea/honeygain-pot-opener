using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Moq;
using HoneygainPotOpener.Service;
using HoneygainPotOpener.Processors;
using HoneygainPotOpener.Configuration;
using HoneygainPotOpener.Logging;
using NuciLog.Core;

namespace HoneygainPotOpener.UnitTests.Service
{
    [TestFixture]
    public sealed class BotServiceTests
    {
        private Mock<IHoneygainProcessor> mockProcessor = null!;
        private Mock<ILogger> mockLogger = null!;
        private BotSettings botSettings = null!;
        private BotService botService = null!;

        [SetUp]
        public void SetUp()
        {
            mockProcessor = new Mock<IHoneygainProcessor>();
            mockLogger = new Mock<ILogger>();
            botSettings = new BotSettings
            {
                EmailAddress = "test@example.com",
                Password = "password123"
            };
            botService = new BotService(mockProcessor.Object, botSettings, mockLogger.Object);
        }

        [Test]
        public void GivenRun_WhenCalled_ThenCallsLogInClaimRewardAndOpenPotInOrder()
        {
            List<string> callOrder = [];

            mockProcessor.Setup(p => p.LogIn()).Callback(() => callOrder.Add("LogIn"));
            mockProcessor.Setup(p => p.ClaimReward()).Callback(() => callOrder.Add("ClaimReward"));
            mockProcessor.Setup(p => p.OpenPot()).Callback(() => callOrder.Add("OpenPot"));

            botService.Run();

            Assert.That(callOrder, Is.EqualTo(new[] { "LogIn", "ClaimReward", "OpenPot" }));
        }

        [Test]
        public void GivenRun_WhenAllSucceed_ThenLogsAllOperations()
        {
            List<Tuple<Operation, OperationStatus>> loggedOperations = [];

            mockLogger.Setup(l => l.Info(It.IsAny<Operation>(), It.IsAny<OperationStatus>(), It.IsAny<IEnumerable<LogInfo>>()))
                .Callback<Operation, OperationStatus, IEnumerable<LogInfo>>((op, status, _) => loggedOperations.Add(Tuple.Create(op, status)));

            botService.Run();

            Assert.That(loggedOperations.Count, Is.EqualTo(6));

            Assert.That(loggedOperations[0].Item1.Name, Is.EqualTo("LogIn"));
            Assert.That(loggedOperations[0].Item2.Name, Is.EqualTo("Started"));

            Assert.That(loggedOperations[1].Item1.Name, Is.EqualTo("LogIn"));
            Assert.That(loggedOperations[1].Item2.Name, Is.EqualTo("Success"));

            Assert.That(loggedOperations[2].Item1.Name, Is.EqualTo("ClaimReward"));
            Assert.That(loggedOperations[2].Item2.Name, Is.EqualTo("Started"));

            Assert.That(loggedOperations[3].Item1.Name, Is.EqualTo("ClaimReward"));
            Assert.That(loggedOperations[3].Item2.Name, Is.EqualTo("Success"));

            Assert.That(loggedOperations[4].Item1.Name, Is.EqualTo("OpenPot"));
            Assert.That(loggedOperations[4].Item2.Name, Is.EqualTo("Started"));

            Assert.That(loggedOperations[5].Item1.Name, Is.EqualTo("OpenPot"));
            Assert.That(loggedOperations[5].Item2.Name, Is.EqualTo("Success"));
        }

        [Test]
        public void GivenRun_WhenLogInThrows_ThenLogsErrorAndContinues()
        {
            InvalidOperationException exception = new("Login failed");
            mockProcessor.Setup(p => p.LogIn()).Throws(exception);

            List<Tuple<Operation, OperationStatus, Exception>> loggedErrors = [];
            mockLogger.Setup(l => l.Error(It.IsAny<Operation>(), It.IsAny<OperationStatus>(), It.IsAny<Exception>(), It.IsAny<IEnumerable<LogInfo>>()))
                .Callback<Operation, OperationStatus, Exception, IEnumerable<LogInfo>>((op, status, ex, _) => loggedErrors.Add(Tuple.Create(op, status, ex)));

            List<Tuple<Operation, OperationStatus>> loggedInfos = [];
            mockLogger.Setup(l => l.Info(It.IsAny<Operation>(), It.IsAny<OperationStatus>(), It.IsAny<IEnumerable<LogInfo>>()))
                .Callback<Operation, OperationStatus, IEnumerable<LogInfo>>((op, status, _) => loggedInfos.Add(Tuple.Create(op, status)));

            botService.Run();

            // Verify error is logged
            Assert.That(loggedErrors.Count, Is.EqualTo(1));
            Assert.That(loggedErrors[0].Item1.Name, Is.EqualTo("LogIn"));
            Assert.That(loggedErrors[0].Item2.Name, Is.EqualTo("Failure"));
            Assert.That(loggedErrors[0].Item3.Message, Is.EqualTo("Login failed"));

            // Verify Success is also logged after error (actual behavior) - filter for LogIn operation
            List<Tuple<Operation, OperationStatus>> logInInfos = loggedInfos.Where(x => x.Item1.Name == "LogIn").ToList();
            Assert.That(logInInfos.Count, Is.EqualTo(2));
            Assert.That(logInInfos[0].Item1.Name, Is.EqualTo("LogIn"));
            Assert.That(logInInfos[0].Item2.Name, Is.EqualTo("Started"));
            Assert.That(logInInfos[1].Item1.Name, Is.EqualTo("LogIn"));
            Assert.That(logInInfos[1].Item2.Name, Is.EqualTo("Success"));

            // Verify it continues to other operations
            mockProcessor.Verify(p => p.ClaimReward(), Times.Once);
            mockProcessor.Verify(p => p.OpenPot(), Times.Once);
        }

        [Test]
        public void GivenRun_WhenClaimRewardThrows_ThenLogsWarningAndContinues()
        {
            InvalidOperationException exception = new("No reward available");
            mockProcessor.Setup(p => p.ClaimReward()).Throws(exception);

            List<Tuple<Operation, OperationStatus, Exception>> loggedWarnings = [];
            mockLogger.Setup(l => l.Warn(It.IsAny<Operation>(), It.IsAny<OperationStatus>(), It.IsAny<Exception>(), It.IsAny<IEnumerable<LogInfo>>()))
                .Callback<Operation, OperationStatus, Exception, IEnumerable<LogInfo>>((op, status, ex, _) => loggedWarnings.Add(Tuple.Create(op, status, ex)));

            List<Tuple<Operation, OperationStatus>> loggedInfos = [];
            mockLogger.Setup(l => l.Info(It.IsAny<Operation>(), It.IsAny<OperationStatus>(), It.IsAny<IEnumerable<LogInfo>>()))
                .Callback<Operation, OperationStatus, IEnumerable<LogInfo>>((op, status, _) => loggedInfos.Add(Tuple.Create(op, status)));

            botService.Run();

            // Verify warning is logged
            Assert.That(loggedWarnings.Count, Is.EqualTo(1));
            Assert.That(loggedWarnings[0].Item1.Name, Is.EqualTo("ClaimReward"));
            Assert.That(loggedWarnings[0].Item2.Name, Is.EqualTo("Failure"));
            Assert.That(loggedWarnings[0].Item3.Message, Is.EqualTo("No reward available"));

            // Verify Success is also logged after warning (actual behavior) - filter for ClaimReward operation
            List<Tuple<Operation, OperationStatus>> claimRewardInfos = loggedInfos.Where(x => x.Item1.Name == "ClaimReward").ToList();
            Assert.That(claimRewardInfos.Count, Is.EqualTo(2));
            Assert.That(claimRewardInfos[0].Item1.Name, Is.EqualTo("ClaimReward"));
            Assert.That(claimRewardInfos[0].Item2.Name, Is.EqualTo("Started"));
            Assert.That(claimRewardInfos[1].Item1.Name, Is.EqualTo("ClaimReward"));
            Assert.That(claimRewardInfos[1].Item2.Name, Is.EqualTo("Success"));

            // Verify it continues to other operations
            mockProcessor.Verify(p => p.LogIn(), Times.Once);
            mockProcessor.Verify(p => p.OpenPot(), Times.Once);
        }

        [Test]
        public void GivenRun_WhenOpenPotThrows_ThenLogsErrorAndContinues()
        {
            InvalidOperationException exception = new("Pot cannot be opened");
            mockProcessor.Setup(p => p.OpenPot()).Throws(exception);

            List<Tuple<Operation, OperationStatus, Exception>> loggedErrors = [];
            mockLogger.Setup(l => l.Error(It.IsAny<Operation>(), It.IsAny<OperationStatus>(), It.IsAny<Exception>(), It.IsAny<IEnumerable<LogInfo>>()))
                .Callback<Operation, OperationStatus, Exception, IEnumerable<LogInfo>>((op, status, ex, _) => loggedErrors.Add(Tuple.Create(op, status, ex)));

            List<Tuple<Operation, OperationStatus>> loggedInfos = [];
            mockLogger.Setup(l => l.Info(It.IsAny<Operation>(), It.IsAny<OperationStatus>(), It.IsAny<IEnumerable<LogInfo>>()))
                .Callback<Operation, OperationStatus, IEnumerable<LogInfo>>((op, status, _) => loggedInfos.Add(Tuple.Create(op, status)));

            botService.Run();

            // Verify error is logged
            Assert.That(loggedErrors.Count, Is.EqualTo(1));
            Assert.That(loggedErrors[0].Item1.Name, Is.EqualTo("OpenPot"));
            Assert.That(loggedErrors[0].Item2.Name, Is.EqualTo("Failure"));
            Assert.That(loggedErrors[0].Item3.Message, Is.EqualTo("Pot cannot be opened"));

            // Verify Success is also logged after error (actual behavior) - filter for OpenPot operation
            List<Tuple<Operation, OperationStatus>> openPotInfos = loggedInfos.Where(x => x.Item1.Name == "OpenPot").ToList();
            Assert.That(openPotInfos.Count, Is.EqualTo(2));
            Assert.That(openPotInfos[0].Item1.Name, Is.EqualTo("OpenPot"));
            Assert.That(openPotInfos[0].Item2.Name, Is.EqualTo("Started"));
            Assert.That(openPotInfos[1].Item1.Name, Is.EqualTo("OpenPot"));
            Assert.That(openPotInfos[1].Item2.Name, Is.EqualTo("Success"));

            // Verify it continues to other operations
            mockProcessor.Verify(p => p.LogIn(), Times.Once);
            mockProcessor.Verify(p => p.ClaimReward(), Times.Once);
        }
    }
}