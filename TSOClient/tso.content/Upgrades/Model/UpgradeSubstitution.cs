using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Upgrades.Model
{
    public class UpgradeSubstitution
    {
        /// <summary>
        /// Tuning value to replace. Format: table:id... eg 4097:0
        /// </summary>
        public string Old;

        /// <summary>
        /// Target value. Constants are prefixed with C, eg C4097:1... and Values are prefixed with V, eg V25, V0, V-600.
        /// </summary>
        public string New;
    }
}
