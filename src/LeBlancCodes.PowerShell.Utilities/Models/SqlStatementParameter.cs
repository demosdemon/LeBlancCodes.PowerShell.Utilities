using System;
using System.Data;
using System.Data.SqlClient;
using JetBrains.Annotations;

namespace LeBlancCodes.PowerShell.Utilities.Models
{
    /// <summary>
    ///     An sql parameter represented in a light data structure to pass around in PowerShell
    /// </summary>
    [PublicAPI]
    public class SqlStatementParameter
    {
        /// <summary>
        ///     Default .ctor. Sets the <see cref="DbType" /> to <see cref="SqlDbType.Variant" />.
        /// </summary>
        public SqlStatementParameter() => DbType = SqlDbType.Variant;

        /// <summary>
        ///     Preferred .ctor.
        /// </summary>
        /// <param name="parameterName">The sql parameter name</param>
        /// <param name="type">sql database type</param>
        public SqlStatementParameter(string parameterName, SqlDbType type)
        {
            ParameterName = parameterName;
            DbType = type;
        }

        /// <summary>
        ///     Initialize parameter with size.
        /// </summary>
        /// <param name="parameterName">The sql parameter name</param>
        /// <param name="type">sql database type</param>
        /// <param name="size">sql field sizez</param>
        /// <inheritdoc />
        public SqlStatementParameter(string parameterName, SqlDbType type, int size) : this(parameterName, type) => Size = size;

        /// <summary>
        ///     Initialize parameter with precision and scale
        /// </summary>
        /// <param name="parameterName">The sql parameter name</param>
        /// <param name="type">sql database type</param>
        /// <param name="precision">decimal precision</param>
        /// <param name="scale">numeric scale</param>
        /// <inheritdoc />
        public SqlStatementParameter(string parameterName, SqlDbType type, byte precision, byte scale) : this(parameterName, type)
        {
            Precision = precision;
            Scale = scale;
        }

        /// <summary>
        ///     The parameter name
        /// </summary>
        public string ParameterName { get; set; }

        /// <summary>
        ///     The parameter value
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        ///     The parameter type
        /// </summary>
        public SqlDbType DbType { get; set; }

        /// <summary>
        ///     Optional, used when <see cref="DbType" /> is set to <see cref="SqlDbType.VarBinary" />,
        ///     <see cref="SqlDbType.VarChar" />, <see cref="SqlDbType.NVarChar" />
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        ///     Optional, used when <see cref="DbType" /> is set to <see cref="SqlDbType.Decimal" />
        /// </summary>
        public byte? Precision { get; set; }

        /// <summary>
        ///     Optional, used when <see cref="DbType" /> is set to <see cref="SqlDbType.Decimal" />
        /// </summary>
        public byte? Scale { get; set; }

        internal SqlParameter Apply()
        {
            var parameter = new SqlParameter(ParameterName, DbType);

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (DbType)
            {
                case SqlDbType.VarBinary:
                case SqlDbType.VarChar:
                case SqlDbType.NVarChar:
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.Text:
                case SqlDbType.NText:
                    if (Size.HasValue) parameter.Size = Size.Value;
                    break;
                case SqlDbType.Decimal:
                    if (Precision.HasValue) parameter.Precision = Precision.Value;
                    if (Scale.HasValue) parameter.Scale = Scale.Value;
                    break;
            }

            parameter.Value = Value ?? DBNull.Value;

            return parameter;
        }
    }
}
