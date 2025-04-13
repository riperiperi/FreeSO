using Dapper;
using System;
using System.Data;
using System.Globalization;

namespace FSO.Server.Database.SqliteCompat
{
    public class Uint32Handler : SqlMapper.TypeHandler<uint?>
    {
        /// <inheritdoc />
        public override uint? Parse(object value)
        {
            if (value == null)
            {
                return null;
            }

            // Sqlite tends to store uint32 as int64.
            return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public override void SetValue(IDbDataParameter parameter, uint? value)
        {
            if (parameter == null)
            {
                return;
            }

            // Sending as an Int32 seems to make the result negative if it overflows 31 bits, so send as a larger type.
            parameter.DbType = DbType.UInt64;
            parameter.Value = value;
        }
    }
}
