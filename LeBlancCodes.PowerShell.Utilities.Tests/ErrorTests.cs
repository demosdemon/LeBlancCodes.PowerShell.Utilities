using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using JetBrains.Annotations;
using LeBlancCodes.PowerShell.Utilities.Internal;
using NUnit.Framework;

namespace LeBlancCodes.PowerShell.Utilities.Tests
{
    [TestFixture]
    [TestOf(typeof(Error))]
    public class ErrorTests
    {
        [Test]
        public void TestEmptyMessage(
            [ValueSource(typeof(DataSource), nameof(DataSource.TestDirectories))]
            string directory) =>
            Assert.That(() => Error.DirectoryNotFound(directory), Is
                .TypeOf<ErrorRecord>()
                .And
                .Property(nameof(ErrorRecord.Exception))
                .Property(nameof(Exception.Message)).Contains(directory));

        [Test]
        public void TestNonEmptyMessage(
            [ValueSource(typeof(DataSource), nameof(DataSource.NonEmptyDirectories))]
            string directory) =>
            Assert.That(() => Error.DirectoryNotFound(directory, "Test Message"), Is
                .TypeOf<ErrorRecord>()
                .And
                .Property(nameof(ErrorRecord.Exception))
                .Property(nameof(Exception.Message)).Not.Contains(directory));

        [Test]
        public void TestReturnType() => Assert.That(() => Error.DirectoryNotFound("test"), Is.Not.Null.And.InstanceOf<ErrorRecord>());

        [Test]
        public void TestWhenDirectoryIsNull() => Assert.That(() => Error.DirectoryNotFound(null), Throws.ArgumentNullException);
    }

    [PublicAPI]
    public static class DataSource
    {
        private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

        private static readonly IEnumerable<Environment.SpecialFolder> SpecialFolders = Enum
            .GetValues(typeof(Environment.SpecialFolder))
            .Cast<Environment.SpecialFolder>()
            .ToArray();

        public static IEnumerable<string> NonEmptyDirectories => TestDirectories.Where(x => !string.IsNullOrWhiteSpace(x));

        public static IEnumerable<string> EnvironmentFolders => SpecialFolders
            .Select(x => Environment.GetFolderPath(x, Environment.SpecialFolderOption.DoNotVerify));

        public static IEnumerable<string> TestDirectories => new[] {Directory.GetCurrentDirectory()}.Concat(EnvironmentFolders).Distinct(Comparer);

        public static IEnumerable<string> ExistingDirectories => TestDirectories.Where(Directory.Exists);

        public static IEnumerable<string> MissingDirectories => TestDirectories.Except(ExistingDirectories, Comparer);
    }
}
