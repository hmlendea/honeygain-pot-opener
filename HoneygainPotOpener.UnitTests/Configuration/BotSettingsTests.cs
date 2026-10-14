using NUnit.Framework;

using HoneygainPotOpener.Configuration;

namespace HoneygainPotOpener.UnitTests.Configuration
{
    [TestFixture]
    public sealed class BotSettingsTests
    {
        [Test]
        public void GivenDefaultConstructor_WhenCreatingInstance_ThenPropertiesAreNull()
        {
            BotSettings settings = new BotSettings();

            Assert.That(settings.EmailAddress, Is.Null);
            Assert.That(settings.Password, Is.Null);
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
    }
}