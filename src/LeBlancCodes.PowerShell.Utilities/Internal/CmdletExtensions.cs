using System.Management.Automation;
using System.Reflection;

namespace LeBlancCodes.PowerShell.Utilities.Internal
{
    internal static class CmdletExtensions
    {
        private static readonly PropertyInfo ParameterSetNamePropertyInfo =
            typeof(Cmdlet).GetProperty("_ParameterSetName", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

        public static string GetParameterSetName(this Cmdlet cmdlet) => (string) ParameterSetNamePropertyInfo.GetValue(cmdlet);
    }
}
