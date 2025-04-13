using Dapper;
using System;
using System.Data;
using System.Globalization;

namespace FSO.Server.Database.SqliteCompat
{
    public class ByteHandler : SqlMapper.TypeHandler<byte?>
    {
        /// <inheritdoc />
        public override byte? Parse(object value)
        {
            if (value == null)
            {
                return null;
            }

            return Convert.ToByte(value, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public override void SetValue(IDbDataParameter parameter, byte? value)
        {
            if (parameter == null)
            {
                return;
            }

            parameter.DbType = DbType.Byte;
            parameter.Value = value;
        }
    }
}
