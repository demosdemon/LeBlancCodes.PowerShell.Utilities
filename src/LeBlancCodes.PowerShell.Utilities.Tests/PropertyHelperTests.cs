using System;
using System.Linq;
using JetBrains.Annotations;
using LeBlancCodes.PowerShell.Utilities.Internal;
using NUnit.Framework;

namespace LeBlancCodes.PowerShell.Utilities.Tests
{
    [TestFixture]
    [TestOf(typeof(PropertyHelper))]
    public class PropertyHelperTests
    {
        private class TestModelA
        {
            [UsedImplicitly]
            public int IntValue { get; set; }

            [UsedImplicitly]
            public string StringValue { get; set; }

            [UsedImplicitly]
            public Guid ReadOnlyGuid { get; } = Guid.NewGuid();

            [UsedImplicitly]
            public string PrivateRead { private get; set; }

            [UsedImplicitly]
            protected string ProtectedStringValue { get; set; }
        }

        [Test]
        public void TestAnonymousTypes()
        {
            var propertyNames = new[] {"String", "Guid", "Int", "Complex"};
            Assert.That(delegate
            {
                var value = new
                {
                    String = "test",
                    Guid = Guid.NewGuid(),
                    Int = new Random().Next(),
                    Complex = new
                    {
                        SubProperty = "Test",
                        AnotherProperty = "testing",
                        ICantComeUpWithGoodTestNames = "I'm not that creative."
                    }
                };

                var properties = PropertyHelper.GetProperties(value);
                return properties.Select(p => p.Name);
            }, Is.EquivalentTo(propertyNames));
        }

        [Test]
        public void TestCreation()
        {
            Assert.That(delegate
            {
                var test = new TestModelA();
                return PropertyHelper.GetProperties(test);
            }, Is.Not.Empty);
        }

        [Test]
        public void TestFailedRead()
        {
            var value = Guid.NewGuid().ToString();

            Assert.That(delegate
            {
                var test = new TestModelA();
                var properties = PropertyHelper.GetProperties(test);
                var property = properties.Single(p => p.Name == nameof(TestModelA.PrivateRead));
                Assert.That(() => property.SetValue(test, value), Throws.Nothing);
                return property.GetValue(test); // throw
            }, Throws.InvalidOperationException);
        }

        [Test]
        public void TestFailedWrite()
        {
            Assert.That(delegate
            {
                var test = new TestModelA();
                var properties = PropertyHelper.GetProperties(test);
                var property = properties.Single(p => p.Name == nameof(TestModelA.ReadOnlyGuid));
                property.SetValue(test, Guid.NewGuid()); // throw
            }, Throws.InvalidOperationException);
        }

        [Test]
        public void TestMutability()
        {
            var value = Guid.NewGuid().ToString();

            Assert.That(delegate
            {
                var test = new TestModelA();
                var properties = PropertyHelper.GetProperties(test);
                var property = properties.Single(p => p.Name == nameof(TestModelA.StringValue));
                Assert.That(() => property.GetValue(test), Is.Not.EqualTo(value));
                property.SetValue(test, value);
                return property.GetValue(test);
            }, Is.EqualTo(value));
        }

        [Test]
        public void TestVisibility()
        {
            Assert.That(delegate
            {
                var test = new TestModelA();
                return PropertyHelper.GetProperties(test);
            }, Has.None.Property(nameof(PropertyHelper.Name)).EqualTo("ProtectedStringValue"));
        }
    }
}
