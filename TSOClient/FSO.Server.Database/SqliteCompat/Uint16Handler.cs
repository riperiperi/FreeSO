using Dapper;
using System;
using System.Data;
using System.Globalization;

namespace FSO.Server.Database.SqliteCompat
{
    public class Uint16Handler : SqlMapper.TypeHandler<ushort?>
    {
        /// <inheritdoc />
        public override ushort? Parse(object value)
        {
            if (value == null)
            {
                return null;
            }

            return Convert.ToUInt16(value, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public override void SetValue(IDbDataParameter parameter, ushort? value)
        {
            if (parameter == null)
            {
                return;
            }

            // Sending as an Int16 seems to make the result negative if it overflows 31 bits, so send as a larger type.
            parameter.DbType = DbType.UInt64;
            parameter.Value = value;
        }
    }
}
