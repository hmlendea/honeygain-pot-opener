using System;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using HoneygainPotOpener.Processors;
using HoneygainPotOpener.Configuration;
using NuciWeb.Automation;

namespace HoneygainPotOpener.UnitTests.Processors
{
    [TestFixture]
    public sealed class HoneygainProcessorTests
    {
        private Mock<IWebProcessor> mockWebProcessor = null!;
        private BotSettings botSettings = null!;
        private HoneygainProcessor processor = null!;

        [SetUp]
        public void SetUp()
        {
            mockWebProcessor = new Mock<IWebProcessor>();
            botSettings = new BotSettings
            {
                EmailAddress = "test@example.com",
                Password = "password123"
            };
            processor = new HoneygainProcessor(mockWebProcessor.Object, botSettings);
        }

        [Test]
        public void GivenHomePageUrl_WhenAccessed_ThenReturnsCorrectUrl()
        {
            Assert.That(HoneygainProcessor.HomePageUrl, Is.EqualTo("https://honeygain.com"));
        }

        [Test]
        public void GivenDashboardUrl_WhenAccessed_ThenReturnsCorrectUrl()
        {
            Assert.That(HoneygainProcessor.DashboardUrl, Is.EqualTo("https://dashboard.honeygain.com"));
        }

        [Test]
        public void GivenLogInUrl_WhenAccessed_ThenReturnsCorrectUrl()
        {
            Assert.That(HoneygainProcessor.LogInUrl, Is.EqualTo("https://dashboard.honeygain.com/login"));
        }

        [Test]
        public void GivenLogIn_WhenCalled_ThenCallsGoToUrlWithLogInUrl()
        {
            processor.LogIn();

            mockWebProcessor.Verify(wp => wp.GoToUrl(HoneygainProcessor.LogInUrl), Times.Once);
        }

        [Test]
        public void GivenLogIn_WhenCalled_ThenCallsAcceptCookiesIfNeeded()
        {
            processor.LogIn();

            mockWebProcessor.Verify(wp => wp.WaitForElementToBeVisible(
                It.Is<string>(s => s.Contains("cookie consent"))), Times.Once);
        }

        [Test]
        public void GivenLogIn_WhenCalled_ThenSetsEmailAddress()
        {
            processor.LogIn();

            mockWebProcessor.Verify(wp => wp.SetText(
                It.Is<string>(s => s.Contains("email")),
                botSettings.EmailAddress), Times.Once);
        }

        [Test]
        public void GivenLogIn_WhenCalled_ThenSetsPassword()
        {
            processor.LogIn();

            mockWebProcessor.Verify(wp => wp.SetText(
                It.Is<string>(s => s.Contains("password")),
                botSettings.Password), Times.Once);
        }

        [Test]
        public void GivenLogIn_WhenCalled_ThenClicksLogInButton()
        {
            processor.LogIn();

            mockWebProcessor.Verify(wp => wp.Click(
                It.Is<string>(s => s.Contains("form/button"))), Times.Once);
        }

        [Test]
        public void GivenLogIn_WhenCalled_ThenWaitsForTrafficLegendContainer()
        {
            processor.LogIn();

            mockWebProcessor.Verify(wp => wp.WaitForElementToBeVisible(
                It.Is<string>(s => s.Contains("traffic-legend-container"))), Times.Once);
        }

        [Test]
        public void GivenClaimReward_WhenButtonNotVisible_ThenThrowsInvalidOperationException()
        {
            mockWebProcessor.Setup(wp => wp.IsElementVisible(
                It.Is<string>(s => s.Contains("Claim reward"))))
                .Returns(false);

            Assert.Throws<InvalidOperationException>(() => processor.ClaimReward());
        }

        [Test]
        public void GivenClaimReward_WhenButtonVisible_ThenClicksButton()
        {
            mockWebProcessor.Setup(wp => wp.IsElementVisible(
                It.Is<string>(s => s.Contains("Claim reward"))))
                .Returns(true);

            processor.ClaimReward();

            mockWebProcessor.Verify(wp => wp.Click(
                It.Is<string>(s => s.Contains("Claim reward"))), Times.Once);
        }

        [Test]
        public void GivenOpenPot_WhenButtonVisible_ThenWaitsForButtonToBeVisible()
        {
            mockWebProcessor.Setup(wp => wp.IsElementVisible(
                It.Is<string>(s => s.Contains("Open Lucky Pot"))))
                .Returns(true);

            processor.OpenPot();

            mockWebProcessor.Verify(wp => wp.WaitForElementToBeVisible(
                It.Is<string>(s => s.Contains("Open Lucky Pot"))), Times.Once);
        }

        [Test]
        public void GivenOpenPot_WhenButtonNotVisible_ThenThrowsInvalidOperationException()
        {
            mockWebProcessor.Setup(wp => wp.IsElementVisible(
                It.Is<string>(s => s.Contains("Open Lucky Pot"))))
                .Returns(false);

            Assert.Throws<InvalidOperationException>(() => processor.OpenPot());
        }

        [Test]
        public void GivenOpenPot_WhenButtonVisible_ThenClicksButton()
        {
            mockWebProcessor.Setup(wp => wp.IsElementVisible(
                It.Is<string>(s => s.Contains("Open Lucky Pot"))))
                .Returns(true);

            processor.OpenPot();

            mockWebProcessor.Verify(wp => wp.Click(
                It.Is<string>(s => s.Contains("Open Lucky Pot"))), Times.Once);
        }
    }
}