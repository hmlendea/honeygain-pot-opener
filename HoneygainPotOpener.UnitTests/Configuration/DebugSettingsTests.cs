using NUnit.Framework;
using HoneygainPotOpener.Configuration;

namespace HoneygainPotOpener.UnitTests.Configuration
{
    [TestFixture]
    public sealed class DebugSettingsTests
    {
        [Test]
        public void GivenDefaultConstructor_WhenCreatingInstance_ThenIsDebugModeIsFalse()
        {
            DebugSettings settings = new DebugSettings();

            Assert.That(settings.IsDebugMode, Is.False);
        }

        [Test]
        public void GivenIsDebugModeFalse_WhenAccessingIsHeadless_ThenReturnsTrue()
        {
            DebugSettings settings = new DebugSettings { IsDebugMode = false };

            Assert.That(settings.IsHeadless, Is.True);
        }

        [Test]
        public void GivenIsDebugModeTrue_WhenAccessingIsHeadless_ThenReturnsFalse()
        {
            DebugSettings settings = new DebugSettings { IsDebugMode = true };

            Assert.That(settings.IsHeadless, Is.False);
        }

        [Test]
        public void GivenInstance_WhenSettingIsDebugMode_ThenValueIsRetrieved()
        {
            DebugSettings settings = new DebugSettings { IsDebugMode = true };

            Assert.That(settings.IsDebugMode, Is.True);
        }
    }
}