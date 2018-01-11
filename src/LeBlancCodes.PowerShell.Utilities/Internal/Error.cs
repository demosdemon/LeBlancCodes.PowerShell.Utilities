using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;

namespace LeBlancCodes.PowerShell.Utilities.Internal
{
    internal static class Error
    {
        [DebuggerStepThrough]
        public static ErrorRecord DirectoryNotFound(string directory, string message = null)
        {
            // string.Empty is allowed
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));

            if (string.IsNullOrWhiteSpace(message))
                message = $"The specified directory was not found '{directory}'";

            var exception = new DirectoryNotFoundException(message);
            var errorRecord = new ErrorRecord(exception, nameof(DirectoryNotFound), ErrorCategory.ObjectNotFound, directory);
            return errorRecord;
        }

        [DebuggerStepThrough]
        [ContractAnnotation("argument:null => halt")]
        public static void ArgumentNull<T>(T argument, [InvokerParameterName] string parameterName, [CallerMemberName] string caller = "", [CallerFilePath] string callerFile = "",
            [CallerLineNumber] int line = -1) where T : class
        {
            if (argument != null) return;

            var message = $"Expected value for parameter: '{parameterName}' {FormatCaller(caller, callerFile, line)}";
            throw new ArgumentNullException(parameterName, message);
        }

        private static string FormatCaller(string caller, string callerFile, int line)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(callerFile))
                sb.AppendFormat("[{0}]", Path.GetFileNameWithoutExtension(caller));
            if (!string.IsNullOrWhiteSpace(callerFile) && !string.IsNullOrWhiteSpace(caller))
                sb.Append(" ");
            if (!string.IsNullOrWhiteSpace(caller))
                sb.Append(caller);
            if (line > 0)
                sb.AppendFormat(":{0:3D}", line);

            return sb.ToString();
        }
    }
}
