using Dapper;
using System;
using System.Data;
using System.Globalization;

namespace FSO.Server.Database.SqliteCompat
{
    public class SbyteHandler : SqlMapper.TypeHandler<sbyte?>
    {
        /// <inheritdoc />
        public override sbyte? Parse(object value)
        {
            if (value == null)
            {
                return null;
            }

            // For some reason, the sqlite connection sees the value as unsigned.
            return (sbyte)Convert.ToByte(value, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public override void SetValue(IDbDataParameter parameter, sbyte? value)
        {
            if (parameter == null)
            {
                return;
            }

            parameter.DbType = DbType.SByte;
            parameter.Value = value;
        }
    }
}
