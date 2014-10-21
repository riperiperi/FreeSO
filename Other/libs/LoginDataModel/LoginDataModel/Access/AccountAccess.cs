using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoginDataModel.Entities
{
    public class AccountAccess
    {
        private DataAccess Context;

        public AccountAccess(DataAccess context)
        {
            this.Context = context;
        }

        /// <summary>
        /// Creates an account in the DB.
        /// </summary>
        /// <param name="AccountName">The name of the account to create.</param>
        /// <param name="Password">The password of the account to create.</param>
        public void Create(Account account)
        {
            Context.Context.Accounts.InsertOnSubmit(account);
            Context.Context.SubmitChanges();
        }

        /// <summary>
        /// Gets an account object by its username
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public Account GetByUsername(string username)
        {
            return Context.Context.Accounts.FirstOrDefault(x => x.AccountName == username);
        }
    }
}
