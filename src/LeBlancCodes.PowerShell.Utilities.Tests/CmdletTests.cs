using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using LeBlancCodes.PowerShell.Utilities.Commands;
using LeBlancCodes.PowerShell.Utilities.Internal;
using NUnit.Framework;

namespace LeBlancCodes.PowerShell.Utilities.Tests
{
    [TestFixture]
    public abstract class CmdletTests : PowerShellTests
    {
        protected abstract string Command { get; }

        protected IEnumerable<T> Invoke<T>(object parameters = null, IEnumerable input = null)
        {
            Instance.AddCommand(Command);
            Instance.AddParameters(ProcessParameters(parameters));

            return Instance.Invoke<T>(input, new PSInvocationSettings {AddToHistory = false, ErrorActionPreference = ActionPreference.Stop});
        }

        private static IDictionary ProcessParameters(object instance)
        {
            switch (instance)
            {
                case null:
                    return new Dictionary<string, object>();
                case IDictionary dict:
                    return dict;
            }

            return PropertyHelper.GetProperties(instance).Where(p => p.CanRead).ToDictionary(p => p.Name, p => p.GetValue(instance));
        }
    }

    [TestFixture]
    public abstract class PowerShellTests
    {
        [SetUp]
        public void SetUpPowerShell()
        {
            Instance = System.Management.Automation.PowerShell.Create();
            Instance.Commands.AddCommand(ImportModuleCommand).AddStatement();
        }

        [TearDown]
        public void TearDownPowerShell()
        {
            Instance.Dispose();
        }

        private static readonly Assembly Assembly = typeof(MergeDirectoriesCmdlet).Assembly;

        private static readonly Command ImportModuleCommand = new Command("Import-Module")
        {
            Parameters = {new CommandParameter("Assembly", Assembly)}
        };

        protected System.Management.Automation.PowerShell Instance { get; set; }
    }
}
