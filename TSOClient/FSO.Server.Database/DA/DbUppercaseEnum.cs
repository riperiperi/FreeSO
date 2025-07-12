namespace FSO.Server.Database.DA
{
    public struct DbUppercaseEnum<T> where T : System.Enum
    {
        private readonly T Value;

        public DbUppercaseEnum(T value)
        {
            Value = value;
        }

        public static implicit operator T(DbUppercaseEnum<T> wrapper) => wrapper.Value;

        public static implicit operator DbUppercaseEnum<T>(T value) => new DbUppercaseEnum<T>(value);
    }
}
