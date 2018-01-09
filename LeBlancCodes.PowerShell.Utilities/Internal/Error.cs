using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;

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
    }
}
