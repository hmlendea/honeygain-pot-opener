using NUnit.Framework;
using HoneygainPotOpener.Logging;
using NuciLog.Core;

namespace HoneygainPotOpener.UnitTests.Logging
{
    [TestFixture]
    public sealed class MyLogInfoKeyTests
    {
        [Test]
        public void GivenUsernameProperty_WhenAccessed_ThenReturnsLogInfoKeyWithCorrectName()
        {
            LogInfoKey key = MyLogInfoKey.Username;

            Assert.That(key, Is.Not.Null);
            Assert.That(key.Name, Is.EqualTo(nameof(MyLogInfoKey.Username)));
        }

        [Test]
        public void GivenUsernameProperty_WhenAccessedMultipleTimes_ThenReturnsNewInstanceEachTime()
        {
            LogInfoKey key1 = MyLogInfoKey.Username;
            LogInfoKey key2 = MyLogInfoKey.Username;

            Assert.That(key1, Is.Not.SameAs(key2));
            Assert.That(key1.Name, Is.EqualTo(key2.Name));
        }

        [Test]
        public void GivenUsernameProperty_WhenAccessed_ThenReturnsLogInfoKeyInstance()
        {
            LogInfoKey key = MyLogInfoKey.Username;

            Assert.That(key, Is.InstanceOf<LogInfoKey>());
        }
    }
}