using NUnit.Framework;

using HoneygainPotOpener.Configuration;

namespace HoneygainPotOpener.UnitTests.Configuration
{
    [TestFixture]
    public sealed class BotSettingsTests
    {
        [Test]
        public void GivenDefaultConstructor_WhenCreatingInstance_ThenPropertiesAreEmpty()
        {
            BotSettings settings = new BotSettings();

            Assert.That(settings.EmailAddress, Is.Empty);
            Assert.That(settings.Password, Is.Empty);
        }

        [Test]
        public void GivenInstance_WhenSettingEmailAddress_ThenValueIsRetrieved()
        {
            BotSettings settings = new BotSettings { EmailAddress = "test@example.com" };

            Assert.That(settings.EmailAddress, Is.EqualTo("test@example.com"));
        }

        [Test]
        public void GivenInstance_WhenSettingPassword_ThenValueIsRetrieved()
        {
            BotSettings settings = new BotSettings { Password = "secret123" };

            Assert.That(settings.Password, Is.EqualTo("secret123"));
        }

        [Test]
        public void GivenInstance_WhenSettingBothProperties_ThenValuesAreRetrievedIndependently()
        {
            BotSettings settings = new BotSettings
            {
                EmailAddress = "user@domain.com",
                Password = "password456"
            };

            Assert.That(settings.EmailAddress, Is.EqualTo("user@domain.com"));
            Assert.That(settings.Password, Is.EqualTo("password456"));
        }

        [TestCase("user@domain.com", "password456", true)]
        [TestCase("user@domain.com", "", false)]
        [TestCase("", "password456", false)]
        [TestCase("   ", "password456", false)]
        [TestCase("user@domain.com", "   ", false)]
        public void GivenCredentials_WhenCheckingValidity_ThenItReflectsCredentialCompleteness(
            string emailAddress,
            string password,
            bool expectedIsValid)
        {
            BotSettings settings = new BotSettings
            {
                EmailAddress = emailAddress,
                Password = password
            };

            Assert.That(settings.HasValidCredentials, Is.EqualTo(expectedIsValid));
        }
    }
}