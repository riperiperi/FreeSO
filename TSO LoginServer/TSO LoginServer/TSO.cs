using System;
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
        /// <summary>
        /// Creates an account in the DB.
        /// </summary>
        /// <param name="AccountName">The name of the account to create.</param>
        /// <param name="Password">The password of the account to create.</param>
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

        /// <summary>
        /// Gets an account from the DB.
        /// </summary>
        /// <param name="AccountName">The name of the account to get.</param>
        /// <returns>The account, or null if the account didn't exist.</returns>
        private static Account GetAccount(string AccountName)
        {
            using (TSODataContext Context = new TSODataContext(DBConnectionManager.DBConnection))
            {
                IQueryable<Account> Accounts = from Acc in Context.Accounts
                                               where (string.Equals(Acc.AccountName, AccountName))
                                               select Acc;
                return Accounts.FirstOrDefault(Acc => string.Equals(Acc.AccountName, AccountName));
            }
        }

        /// <summary>
        /// Gets the password for a specific account.
        /// </summary>
        /// <param name="AccountName">The name of the account.</param>
        /// <returns>The password as a string.</returns>
        public static string GetPassword(string AccountName)
        {
            Account CorrectAccount = GetAccount(AccountName);

            if (CorrectAccount != null)
                return CorrectAccount.Password;
            else
                return "";
        }

        /// <summary>
        /// Checks for the existence of a specified account in the DB.
        /// </summary>
        /// <param name="AccountName">The accountname to check for.</param>
        /// <returns>True if the account existed, false otherwise.</returns>
        public static bool DoesAccountExist(string AccountName)
        {
            if (GetAccount(AccountName) != null)
                return true;

            return false;
        }

        /// <summary>
        /// Checks if supplied password hash is correct for the specified account
        /// </summary>
        /// <param name="AccountName">The name of the account</param>
        /// <param name="PasswordHash">The hashed password to check.</param>
        /// <returns>True if the password was correct, false otherwise.</returns>
        public static bool IsCorrectPassword(string AccountName, byte[] PasswordHash)
        {
            using (TSODataContext Context = new TSODataContext(DBConnectionManager.DBConnection))
            {
                SaltedHash SHash = new SaltedHash(new SHA512Managed(), AccountName.Length);

                //WTF?! Acc isn't defined anywhere...
                Account CorrectAccount = GetAccount(AccountName);

                if (CorrectAccount != null)
                {
                    if (SHash.VerifyHash(Encoding.ASCII.GetBytes(CorrectAccount.Password.ToUpper()), PasswordHash,
                        Encoding.ASCII.GetBytes(AccountName)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    partial class Character
    {
        /// <summary>
        /// Creates a character in the DB.
        /// </summary>
        /// <param name="SimCharacter">The character to create.</param>
        public static void CreateCharacter(Sim SimCharacter)
        {
            using (TSODataContext Context = new TSODataContext(DBConnectionManager.DBConnection))
            {
                //TODO: Attach this character to a specific account...
                Character Charac = new Character();
                Charac.Name = SimCharacter.Name;
                Charac.Sex = SimCharacter.Sex;
                Charac.LastCached = SimCharacter.Timestamp;
                Charac.GUID = SimCharacter.GUID;

                Context.Characters.InsertOnSubmit(Charac);
                Context.SubmitChanges();
            }
        }

        /// <summary>
        /// Gets a character from the DB.
        /// </summary>
        /// <param name="CharacterID">The ID of the character to get.</param>
        /// <returns>The character associated with the ID, or null if no character was found.</returns>
        private static Character GetCharacter(int CharacterID)
        {
            using (TSODataContext Context = new TSODataContext(DBConnectionManager.DBConnection))
            {
                IQueryable<Character> Characters = from Char in Context.Characters
                                                   where (Char.CharacterID == CharacterID)
                                                   select Char;
                return Characters.FirstOrDefault(Char =>
                    Char.CharacterID == CharacterID);
            }
        }

        /// <summary>
        /// Gets all the characters associated with a specified account.
        /// </summary>
        /// <param name="AccountName">The name of the account.</param>
        /// <returns>An array of all the characters for the account, or null if no characters existed.</returns>
        public static Character[] GetCharacters(string AccountName)
        {
            using (TSODataContext Context = new TSODataContext(DBConnectionManager.DBConnection))
            {
                Character Char1, Char2, Char3;

                IQueryable<Account> Accounts = from Acc in Context.Accounts
                                               where (string.Equals(Acc.AccountName, AccountName))
                                               select Acc;
                Account CorrectAccount = Accounts.FirstOrDefault(Acc =>
                    string.Equals(Acc.AccountName, AccountName));

                if (CorrectAccount != null)
                {
                    int NumCharacters = (int)CorrectAccount.NumCharacters;
                    int CharacterID1 = 0, CharacterID2 = 0, CharacterID3 = 0;

                    switch (NumCharacters)
                    {
                        case 1:
                            CharacterID1 = (int)CorrectAccount.Character1;

                            Char1 = GetCharacter(CharacterID1);

                            return new Character[] { Char1 };
                        case 2:
                            CharacterID1 = (int)CorrectAccount.Character1;
                            CharacterID2 = (int)CorrectAccount.Character2;

                            Char1 = GetCharacter(CharacterID1);
                            Char2 = GetCharacter(CharacterID2);

                            return new Character[] { Char1, Char2 };
                        case 3:
                            CharacterID1 = (int)CorrectAccount.Character1;
                            CharacterID2 = (int)CorrectAccount.Character2;
                            CharacterID3 = (int)CorrectAccount.Character3;

                            Char1 = GetCharacter(CharacterID1);
                            Char2 = GetCharacter(CharacterID2);
                            Char3 = GetCharacter(CharacterID3);

                            return new Character[] { Char1, Char2, Char3 };
                        default:
                            return null;
                    }
                }
                else
                    return null;
            }
        }
    }
}