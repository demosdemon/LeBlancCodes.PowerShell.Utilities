using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Management.Automation;
using System.Xml;
using JetBrains.Annotations;
using LeBlancCodes.PowerShell.Utilities.Internal;
using LeBlancCodes.PowerShell.Utilities.Models;
using LeBlancCodes.PowerShell.Utilities.Properties;

namespace LeBlancCodes.PowerShell.Utilities.Commands
{
    /// <inheritdoc cref="Cmdlet" />
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    ///     Invoke-SqlStatement cmdlet
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "SqlStatement")]
    [PublicAPI]
    public sealed class InvokeSqlStatementCmdlet : Cmdlet, IDisposable
    {
        private bool _dispose;

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
        ///     The sql parameters are taken from the input values
        /// </summary>
        [Parameter(Position = 2, ValueFromPipeline = true)]
        [AllowNull]
        public PSObject Parameters { get; set; }

        /// <summary>
        ///     Include a sentinel object at the end of every result set
        /// </summary>
        [Parameter]
        public SwitchParameter ResultSetSentinel { get; set; }

        private SqlCommand Command { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            Command?.Dispose();
            Command = null;

            if (_dispose)
                Database?.Dispose();
            Database = null;
        }

        /// <inheritdoc />
        protected override void BeginProcessing()
        {
            if (!string.IsNullOrWhiteSpace(ConnectionString))
            {
                _dispose = true;
                Database = new SqlConnection(ConnectionString);
                Database.Open();
            }

            Error.ArgumentNull(Database, nameof(Database));

            if ((Database.State & ConnectionState.Open) == 0)
            {
                _dispose = true;
                Database.Open();
            }

            Command = Database.CreateCommand();
            Command.CommandText = Statement; // Setting the CommandText unchecked is intentional.
        }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            Command.Parameters.Clear();
            ProcessParameters(Parameters).Select(MapSqlParameter).ForEach(Command.Parameters.Add);
            var sentinel = new ResultSetSentinel();

            using (var reader = Command.ExecuteReader())
            {
                do
                {
                    var headers = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();

                    while (reader.Read())
                    {
                        var row = ProcessRow(headers, reader);
                        WriteObject(row);
                    }

                    if (ResultSetSentinel)
                        WriteObject(sentinel);
                }
                while (reader.NextResult());
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() => Dispose();

        [NotNull]
        private static SqlParameter MapSqlParameter([NotNull] SqlStatementParameter param)
        {
            Error.ArgumentNull(param, nameof(param));

            var parameter = new SqlParameter(param.ParameterName, param.DbType);

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (param.DbType)
            {
                case SqlDbType.VarBinary:
                case SqlDbType.VarChar:
                case SqlDbType.NVarChar:
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.Text:
                case SqlDbType.NText:
                    if (param.Size.HasValue) parameter.Size = param.Size.Value;
                    break;
                case SqlDbType.Decimal:
                    if (param.Precision.HasValue) parameter.Precision = param.Precision.Value;
                    if (param.Scale.HasValue) parameter.Scale = param.Scale.Value;
                    break;
            }

            parameter.Value = param.Value ?? DBNull.Value;

            return parameter;
        }

        [NotNull]
        private static IEnumerable<SqlStatementParameter> ProcessParameters([NotNull] PSObject parameters)
        {
            var prefix = ProcessParameters(parameters.BaseObject as Hashtable);

            return parameters.Properties.Where(x => x.IsGettable).Select(prop => MapParameter(prop.Name, prop.Value)).Concat(prefix);
        }

        [NotNull]
        private static IEnumerable<SqlStatementParameter> ProcessParameters([CanBeNull] Hashtable hashtable) => hashtable.AsEnumerable().Map(MapParameter);

        [NotNull]
        private static SqlStatementParameter MapParameter([NotNull] string key, [CanBeNull] object value)
        {
            Error.ArgumentNull(key, nameof(key));

            if (value is SqlStatementParameter parameter) return parameter;

            var type = SqlDbType.Variant;
            switch (value)
            {
                case long _:
                    type = SqlDbType.BigInt;
                    break;
                case byte[] bytes when bytes.Length == 8:
                    type = SqlDbType.Timestamp;
                    break;
                case byte[] _:
                    type = SqlDbType.VarBinary;
                    break;
                case bool _:
                    type = SqlDbType.Bit;
                    break;
                case char _:
                    type = SqlDbType.NChar;
                    break;
                case string s when s.TrimStart().StartsWith("<?xml", StringComparison.Ordinal):
                    type = SqlDbType.Xml;
                    break;
                case string _:
                    type = SqlDbType.NVarChar;
                    break;
                case decimal _:
                    type = SqlDbType.Decimal;
                    break;
                case float _:
                    type = SqlDbType.Float;
                    break;
                case double _:
                    type = SqlDbType.Real;
                    break;
                case int _:
                    type = SqlDbType.Int;
                    break;
                case Guid _:
                    type = SqlDbType.UniqueIdentifier;
                    break;
                case short _:
                    type = SqlDbType.SmallInt;
                    break;
                case byte _:
                    type = SqlDbType.TinyInt;
                    break;
                case DateTimeOffset _:
                    type = SqlDbType.DateTimeOffset;
                    break;
                case TimeSpan _:
                case DateTime dt when dt.Date == DateTime.MinValue:
                    type = SqlDbType.Time;
                    break;
                case DateTime dt when dt.TimeOfDay == TimeSpan.Zero:
                    type = SqlDbType.Date;
                    break;
                case DateTime _:
                    type = SqlDbType.DateTime2;
                    break;
                case XmlDocument doc:
                    value = doc.OuterXml;
                    type = SqlDbType.Xml;
                    break;
            }

            var name = key.StartsWith("@", StringComparison.Ordinal) ? key : $"@{key}";

            return new SqlStatementParameter(name, type) {Value = value};
        }

        [NotNull]
        private static PSObject ProcessRow([NotNull] string[] headers, [NotNull] IDataReader reader)
        {
            Error.ArgumentNull(headers, nameof(headers));
            Error.ArgumentNull(reader, nameof(reader));

            var obj = new PSObject();

            var values = new object[headers.Length];

            if (headers.Length != reader.GetValues(values))
                throw new ArgumentException(Strings.HeaderValueMismatch);

            try
            {
                headers
                    .Zip(values, (name, value) => new PSNoteProperty(name, value))
                    .SafeForEach(obj.Properties.Add);
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Count > 1)
                    throw;
            }

            return obj;
        }
    }
}
