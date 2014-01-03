using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using TSO_LoginServer.Network.Encryption;
using CityDataModel;

public partial class Account
{
    /// <summary>
    /// Checks if supplied password hash is correct for the specified account
    /// </summary>
    /// <param name="AccountName">The name of the account</param>
    /// <param name="PasswordHash">The hashed password to check.</param>
    /// <returns>True if the password was correct, false otherwise.</returns>
    public bool IsCorrectPassword(string password, string salt, string storedPassword)
    {
        var hash = GetPasswordHash(password, salt);
        return hash == storedPassword;
    }

    /// <summary>
    /// Checks if supplied password hash is correct for the specified account
    /// </summary>
    /// <param name="AccountName">The name of the account</param>
    /// <param name="PasswordHash">The hashed password to check.</param>
    /// <returns>True if the password was correct, false otherwise.</returns>
    public bool IsCorrectPassword(string AccountName, byte[] PasswordHash)
    {
        using (var db = DataAccess.Get())
        {
            Account CorrectAccount = db.Accounts.GetByUsername(AccountName);

            byte[] DBHash = Convert.FromBase64String(CorrectAccount.Password);

            if (PasswordHash.Length == DBHash.Length)
            {
                int i = 0;
                while ((i < PasswordHash.Length) && (PasswordHash[i] == DBHash[i]))
                {
                    i += 1;
                }

                if (i == PasswordHash.Length)
                    return true;
            }
        }

        return false;
    }

    public static string GetPasswordHash(string password, string salt)
    {
        SaltedHash SHash = new SaltedHash(new SHA512Managed(), salt.Length);
        return Convert.ToBase64String(SHash.ComputePasswordHash(salt.ToUpper(), password.ToUpper()));
    }
}



