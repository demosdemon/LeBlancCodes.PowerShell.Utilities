using System.Data.SqlClient;
using System.Management.Automation;
using JetBrains.Annotations;

namespace LeBlancCodes.PowerShell.Utilities.Commands
{
    /// <summary>
    ///     New-SqlConnectionString cmdlet
    /// </summary>
    [Cmdlet(VerbsCommon.New, "SqlConnectionString")]
    [OutputType(typeof(string))]
    [PublicAPI]
    public class NewSqlConnectionStringCmdlet : Cmdlet
    {
        /// <summary>
        ///     Data Source (server)
        /// </summary>
        [Alias("Addr", "Address", "NetworkAddress", "Server")]
        [Parameter(Position = 0, Mandatory = true)]
        public string DataSource { get; set; }

        /// <summary>
        ///     Initial Catalog (database)
        /// </summary>
        [Alias("Database")]
        [Parameter(Position = 1, Mandatory = true)]
        public string InitialCatalog { get; set; }

        /// <summary>
        ///     Integrated Security
        /// </summary>
        [Alias("TrustedConnection")]
        [Parameter(ParameterSetName = "IntegratedSecurity", Mandatory = true)]
        public SwitchParameter IntegratedSecurity { get; set; }

        /// <summary>
        ///     Persisty Security Info
        /// </summary>
        [Parameter]
        public SwitchParameter PersistSecurityInfo { get; set; }

        /// <summary>
        ///     User ID
        /// </summary>
        [Alias("Uid", "User")]
        [Parameter(ParameterSetName = "UserPass", Mandatory = true)]
        public string UserID { get; set; }

        /// <summary>
        ///     Password
        /// </summary>
        [Alias("Pwd")]
        [Parameter(ParameterSetName = "UserPass", Mandatory = true)]
        public string Password { get; set; }

        /// <summary>
        ///     MARS
        /// </summary>
        [Parameter]
        [Alias("MARS")]
        public SwitchParameter MultipleActiveResultSets { get; set; }

        /// <summary>
        ///     Connect Timeout
        /// </summary>
        [Alias("ConnectionTimeout", "Timeout")]
        [Parameter]
        public int ConnectTimeout { get; set; } = 15;

        /// <summary>
        ///     Trust invalid ssl certificate
        /// </summary>
        [Parameter]
        public SwitchParameter TrustServerCertificate { get; set; }

        /// <summary>
        ///     Authentication
        /// </summary>
        [Parameter]
        public SqlAuthenticationMethod Authentication { get; set; }

        /// <summary>
        ///     Application Name
        /// </summary>
        [Alias("App")]
        [Parameter]
        public string ApplicationName { get; set; }

        /// <summary>
        ///     Workstation ID
        /// </summary>
        [Alias("Wid")]
        [Parameter]
        public string WorkstationID { get; set; }

        /// <summary>
        ///     Application Intent
        /// </summary>
        [Parameter]
        public ApplicationIntent ApplicationIntent { get; set; } = ApplicationIntent.ReadWrite;

        /// <summary>
        ///     Connect Retry Count
        /// </summary>
        [Parameter]
        public int ConnectRetryCount { get; set; } = 1;

        /// <summary>
        ///     Connect Retry Interval
        /// </summary>
        [Parameter]
        public int ConnectRetryInterval { get; set; } = 10;

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var csBuilder = new SqlConnectionStringBuilder
            {
                DataSource = DataSource,
                InitialCatalog = InitialCatalog,
                PersistSecurityInfo = PersistSecurityInfo,
                MultipleActiveResultSets = MultipleActiveResultSets,
                ConnectTimeout = ConnectTimeout,
                TrustServerCertificate = TrustServerCertificate,
                Authentication = Authentication,
                ApplicationIntent = ApplicationIntent,
                ConnectRetryCount = ConnectRetryCount,
                ConnectRetryInterval = ConnectRetryInterval
            };

            if (!string.IsNullOrWhiteSpace(ApplicationName))
                csBuilder.ApplicationName = ApplicationName;

            if (!string.IsNullOrWhiteSpace(WorkstationID))
                csBuilder.WorkstationID = WorkstationID;

            if (IntegratedSecurity)
                csBuilder.IntegratedSecurity = IntegratedSecurity;
            else
            {
                csBuilder.UserID = UserID;
                csBuilder.Password = Password;
            }

            WriteObject(csBuilder.ConnectionString);
        }
    }
}
