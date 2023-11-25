namespace FSO.Server.Database.DA.Transactions
{
    public interface ITransactions
    {
        void Purge(int day);
    }
}
