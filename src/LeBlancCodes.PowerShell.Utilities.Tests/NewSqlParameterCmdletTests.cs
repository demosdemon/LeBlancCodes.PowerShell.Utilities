using System;
using System.Data;
using System.Linq;
using System.Management.Automation;
using LeBlancCodes.PowerShell.Utilities.Commands;
using LeBlancCodes.PowerShell.Utilities.Models;
using NUnit.Framework;

namespace LeBlancCodes.PowerShell.Utilities.Tests
{
    [TestFixture]
    [TestOf(typeof(NewSqlParameterCmdlet))]
    public class NewSqlParameterCmdletTests : CmdletTests
    {
        protected override string Command => "New-SqlStatementParameter";

        [Test]
        public void TestNameAndType()
        {
            var args = new {Name = "TestParameter", DbType = SqlDbType.NVarChar};
            Assert.That(() => Invoke<SqlStatementParameter>(args).Single(), Has
                .Property(nameof(SqlStatementParameter.ParameterName)).EqualTo("TestParameter").And
                .Property(nameof(SqlStatementParameter.DbType)).EqualTo(SqlDbType.NVarChar).And
                .Property(nameof(SqlStatementParameter.Value)).EqualTo(DBNull.Value));
        }

        [Test]
        public void TestNameNoType()
        {
            var args = new {Name = "TestParameter"};
            Assert.That(() => Invoke<SqlStatementParameter>(args).Single(), Throws.InstanceOf<ParameterBindingException>());
        }

        [Test]
        public void TestNoParameters()
        {
            Assert.That(() => Invoke<SqlStatementParameter>().Single(), Has.Property(nameof(SqlStatementParameter.DbType)).EqualTo(SqlDbType.Variant));
        }
    }
}
