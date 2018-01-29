using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.EmailConfirmation
{
    /// <summary>
    /// EmailConfirmation model
    /// </summary>
    public class EmailConfirmation
    {
        /// <summary>
        /// Confirmation type. Can be an email confirmation or password
        /// reset confirmation.
        /// </summary>
        public ConfirmationType type { get; set; }
        /// <summary>
        /// The user email address.
        /// </summary>
        public string email { get; set; }
        /// <summary>
        /// Randomized token.
        /// </summary>
        public string token { get; set; }
        /// <summary>
        /// Timestamp when the confirmation token will expire.
        /// </summary>
        public uint expires { get; set; }
    }

    public enum ConfirmationType
    {
        email = 1,
        password
    }
}
