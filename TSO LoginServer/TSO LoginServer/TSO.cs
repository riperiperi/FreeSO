using System.Data;
using System.Linq;
using System.Data.Linq;

namespace TSO_LoginServer
{
    partial class Account
    {
        public static void CreateAccount(string AccountName, string Password)
        {
            using (TSODataContext Context = new TSODataContext())
            {
                Account Acc = new Account();
                Acc.AccountName = AccountName;
                Acc.Password = Password;

                Context.Accounts.InsertOnSubmit(Acc);
                Context.SubmitChanges();
            }
        }

        public static bool DoesAccountExist(string AccountName)
        {
            using (TSODataContext Context = new TSODataContext())
            {
                IQueryable<Account> Accounts = from Acc in Context.Accounts
                                               where (string.Equals(Acc.AccountName, AccountName))
                                          select Acc;
                if (Accounts != null)
                    return true; //The above query should return exactly one account...
            }

            return false;
        }

        public static bool IsCorrectPassword(string AccountName, string Password)
        {
            using (TSODataContext Context = new TSODataContext())
            {
                IQueryable<Account> Accounts = from Acc in Context.Accounts
                                               where (string.Equals(Acc.AccountName, AccountName))
                                               select Acc;
                if (Accounts != null)
                {
                    //WTF?! Acc isn't defined anywhere...
                    IQueryable<Account> CorrectAccount = Accounts.Where(Acc => 
                        string.Equals(Acc.Password, Password));

                    if (CorrectAccount != null)
                        return true;
                }
            }

            return false;
        }

        public static void CreateCharacter(Sim SimCharacter)
        {
            using (TSODataContext Context = new TSODataContext())
            {
                Character Charac = new Character();
                Charac.Name = SimCharacter.Name;
                Charac.Sex = SimCharacter.Sex;
                Charac.LastCached = SimCharacter.Timestamp;
                Charac.GUID = SimCharacter.GUID;

                Context.Characters.InsertOnSubmit(Charac);
                Context.SubmitChanges();
            }
        }
    }
}