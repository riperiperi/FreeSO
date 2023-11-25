namespace FSO.Server.Database.DA
{
    public class AbstractSqlDA
    {
        protected ISqlContext Context;

        public AbstractSqlDA(ISqlContext context)
        {
            this.Context = context;
        }
    }
}
