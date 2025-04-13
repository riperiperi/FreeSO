using Dapper;
using System;
using System.Data;
using System.Globalization;

namespace FSO.Server.Database.SqliteCompat
{
    public class Int32Handler : SqlMapper.TypeHandler<int?>
    {
        /// <inheritdoc />
        public override int? Parse(object value)
        {
            if (value == null)
            {
                return null;
            }

            // Sqlite tends to store int32 as int64.
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public override void SetValue(IDbDataParameter parameter, int? value)
        {
            if (parameter == null)
            {
                return;
            }

            parameter.DbType = DbType.Int32;
            parameter.Value = value;
        }
    }
}
