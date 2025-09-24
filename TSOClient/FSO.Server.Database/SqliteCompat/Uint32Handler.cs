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

            if (value.GetType() == typeof(long))
            {
                long longValue = (long)value;

                // The value might be negative due to sqlite not supporting unsigned values - cast it to uint.
                return (uint)longValue;
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
            // This doesn't seem to trigger all the time, so Parse also handles conversions back from int to uint.
            parameter.DbType = DbType.UInt64;
            parameter.Value = value;
        }
    }
}
