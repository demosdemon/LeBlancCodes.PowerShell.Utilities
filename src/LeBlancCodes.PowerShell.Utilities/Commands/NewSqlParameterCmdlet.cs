using System.Data;
using System.Management.Automation;
using JetBrains.Annotations;
using LeBlancCodes.PowerShell.Utilities.Internal;
using LeBlancCodes.PowerShell.Utilities.Models;

namespace LeBlancCodes.PowerShell.Utilities.Commands
{
    /// <summary>
    ///     New-SqlStatementParameter cmdlet
    /// </summary>
    [Cmdlet(VerbsCommon.New, nameof(SqlStatementParameter), DefaultParameterSetName = Nameless)]
    [OutputType(typeof(SqlStatementParameter))]
    [PublicAPI]
    public class NewSqlParameterCmdlet : Cmdlet
    {
        private const string Named = "Named";
        private const string NamedDecimal = "Named-Decimal";
        private const string NamedString = "Named-String";
        private const string Nameless = "Nameless";

        /// <summary>
        ///     The SQL Parameter Name
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = Named, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = NamedDecimal, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = NamedString, ValueFromPipelineByPropertyName = true)]
        [Alias("Name")]
        public string ParameterName { get; set; }

        /// <summary>
        ///     The database type
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = Named, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = NamedDecimal, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = NamedString, ValueFromPipelineByPropertyName = true)]
        [Alias("Type", "SqlDbType")]
        public SqlDbType DbType { get; set; }

        /// <summary>
        ///     The variable length (varchar/varbinary) element size
        /// </summary>
        [Parameter(Position = 2, Mandatory = true, ParameterSetName = NamedString, ValueFromPipelineByPropertyName = true)]
        public int Size { get; set; }

        /// <summary>
        ///     The decimal precision
        /// </summary>
        [Parameter(Position = 2, Mandatory = true, ParameterSetName = NamedDecimal, ValueFromPipelineByPropertyName = true)]
        public byte Precision { get; set; }

        /// <summary>
        ///     The numeric scale
        /// </summary>
        [Parameter(Position = 3, Mandatory = true, ParameterSetName = NamedDecimal, ValueFromPipelineByPropertyName = true)]
        public byte Scale { get; set; }

        /// <summary>
        ///     The sql parameter value
        /// </summary>
        [Parameter]
        public object Value { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            SqlStatementParameter rv;
            switch (this.GetParameterSetName())
            {
                case Named:
                    rv = new SqlStatementParameter(ParameterName, DbType);
                    break;
                case NamedString:
                    rv = new SqlStatementParameter(ParameterName, DbType, Size);
                    break;
                case NamedDecimal:
                    rv = new SqlStatementParameter(ParameterName, DbType, Precision, Scale);
                    break;
                default:
                    rv = new SqlStatementParameter();
                    break;
            }

            rv.Value = Value;

            WriteObject(rv);
        }
    }
}
