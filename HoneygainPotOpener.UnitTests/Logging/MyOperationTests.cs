using NUnit.Framework;
using HoneygainPotOpener.Logging;
using NuciLog.Core;

namespace HoneygainPotOpener.UnitTests.Logging
{
    [TestFixture]
    public sealed class MyOperationTests
    {
        [Test]
        public void GivenLogInProperty_WhenAccessed_ThenReturnsOperationWithCorrectName()
        {
            Operation operation = MyOperation.LogIn;

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Name, Is.EqualTo(nameof(MyOperation.LogIn)));
        }

        [Test]
        public void GivenClaimRewardProperty_WhenAccessed_ThenReturnsOperationWithCorrectName()
        {
            Operation operation = MyOperation.ClaimReward;

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Name, Is.EqualTo(nameof(MyOperation.ClaimReward)));
        }

        [Test]
        public void GivenOpenPotProperty_WhenAccessed_ThenReturnsOperationWithCorrectName()
        {
            Operation operation = MyOperation.OpenPot;

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Name, Is.EqualTo(nameof(MyOperation.OpenPot)));
        }

        [Test]
        public void GivenLogInProperty_WhenAccessedMultipleTimes_ThenReturnsNewInstanceEachTime()
        {
            Operation op1 = MyOperation.LogIn;
            Operation op2 = MyOperation.LogIn;

            Assert.That(op1, Is.Not.SameAs(op2));
            Assert.That(op1.Name, Is.EqualTo(op2.Name));
        }

        [Test]
        public void GivenClaimRewardProperty_WhenAccessedMultipleTimes_ThenReturnsNewInstanceEachTime()
        {
            Operation op1 = MyOperation.ClaimReward;
            Operation op2 = MyOperation.ClaimReward;

            Assert.That(op1, Is.Not.SameAs(op2));
            Assert.That(op1.Name, Is.EqualTo(op2.Name));
        }

        [Test]
        public void GivenOpenPotProperty_WhenAccessedMultipleTimes_ThenReturnsNewInstanceEachTime()
        {
            Operation op1 = MyOperation.OpenPot;
            Operation op2 = MyOperation.OpenPot;

            Assert.That(op1, Is.Not.SameAs(op2));
            Assert.That(op1.Name, Is.EqualTo(op2.Name));
        }

        [Test]
        public void GivenAllOperations_WhenAccessed_ThenInheritFromOperation()
        {
            Assert.That(MyOperation.LogIn, Is.InstanceOf<Operation>());
            Assert.That(MyOperation.ClaimReward, Is.InstanceOf<Operation>());
            Assert.That(MyOperation.OpenPot, Is.InstanceOf<Operation>());
        }
    }
}