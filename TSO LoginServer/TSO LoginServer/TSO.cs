using System.Data;
using System.Linq;
using System.Data.Linq;
using System.Text;
using System.Security.Cryptography;
using TSO_LoginServer.Network;
using TSO_LoginServer.Network.Encryption;

namespace TSO_LoginServer
{
    partial class Account
    {
        public static void CreateAccount(string AccountName, string Password)
        {
            using (TSODataContext Context = new TSODataContext(DBConnectionManager.DBConnection))
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
            using (TSODataContext Context = new TSODataContext(DBConnectionManager.DBConnection))
            {
                IQueryable<Account> Accounts = from Acc in Context.Accounts
                                               where (string.Equals(Acc.AccountName, AccountName))
                                          select Acc;
                if (Accounts != null)
                    return true; //The above query should return exactly one account...
            }

            return false;
        }

        public static bool IsCorrectPassword(string AccountName, byte[] PasswordHash)
        {
            using (TSODataContext Context = new TSODataContext(DBConnectionManager.DBConnection))
            {
                IQueryable<Account> Accounts = from Acc in Context.Accounts
                                               where (string.Equals(Acc.AccountName, AccountName))
                                               select Acc;
                if (Accounts != null)
                {
                    SaltedHash SHash = new SaltedHash(new SHA512Managed(), AccountName.Length);

                    //WTF?! Acc isn't defined anywhere...
                    IQueryable<Account> AccountQueryable = Accounts.Where(Acc => 
                        string.Equals(Acc.AccountName, AccountName));
                    Account CorrectAccount = (Account)AccountQueryable;

                    if (SHash.VerifyHash(Encoding.ASCII.GetBytes(CorrectAccount.Password.ToUpper()), PasswordHash,
                        Encoding.ASCII.GetBytes(AccountName)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void CreateCharacter(Sim SimCharacter)
        {
            using (TSODataContext Context = new TSODataContext(DBConnectionManager.DBConnection))
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