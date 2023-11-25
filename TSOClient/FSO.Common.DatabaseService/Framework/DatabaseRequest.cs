using FSO.Common.DatabaseService.Model;

namespace FSO.Common.DatabaseService.Framework
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class DatabaseRequest : System.Attribute
    {
        public DBRequestType Type;

        public DatabaseRequest(DBRequestType type)
        {
            this.Type = type;
        }
    }
}
