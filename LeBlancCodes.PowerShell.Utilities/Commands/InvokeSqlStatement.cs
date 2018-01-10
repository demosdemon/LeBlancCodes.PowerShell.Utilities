using System.Data.SqlClient;
using System.Management.Automation;
using JetBrains.Annotations;
using LeBlancCodes.PowerShell.Utilities.Models;

namespace LeBlancCodes.PowerShell.Utilities.Commands
{
    /// <summary>
    ///     Invoke-SqlStatement cmdlet
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "SqlStatement")]
    [PublicAPI]
    public class InvokeSqlStatement : Cmdlet
    {
        /// <summary>
        ///     Database parameter, will open and close or use the already open connection
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "SqlConnection")]
        public SqlConnection Database { get; set; }

        /// <summary>
        ///     Will open and close a new connection
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "ConnectionString")]
        public string ConnectionString { get; set; }

        /// <summary>
        ///     The parameterized statement to execute
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        [Alias("SQL", "Query", "Command")]
        public string Statement { get; set; }

        /// <summary>
        ///     An array of parameters to bind to the statement. The statement will be prepared before the first execution to make
        ///     multiple executions more efficient.
        /// </summary>
        [Parameter]
        public SqlStatementParameter[] Parameters { get; set; }
    }
}
