using FSO.Common.DatabaseService.Model;

namespace FSO.Common.DatabaseService.Framework
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class DatabaseResponse : System.Attribute
    {
        public DBResponseType Type;

        public DatabaseResponse(DBResponseType type)
        {
            this.Type = type;
        }
    }
}
