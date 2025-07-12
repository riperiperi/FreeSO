using Dapper;
using FSO.Server.Database.DA;
using System;
using System.Data;

namespace FSO.Server.Database.SqliteCompat
{
    public class DbEnumHandler<T> : SqlMapper.TypeHandler<DbUppercaseEnum<T>?> where T : Enum
    {
        /// <inheritdoc />
        public override DbUppercaseEnum<T>? Parse(object value)
        {
            if (value == null || !(value is string strValue))
            {
                return null;
            }

            return new DbUppercaseEnum<T>((T)Enum.Parse(typeof(T), strValue));
        }

        /// <inheritdoc />
        public override void SetValue(IDbDataParameter parameter, DbUppercaseEnum<T>? value)
        {
            if (parameter == null || value == null)
            {
                return;
            }

            parameter.DbType = DbType.String;
            parameter.Value = Enum.GetName(typeof(T), (T)(value));
        }
    }
}
