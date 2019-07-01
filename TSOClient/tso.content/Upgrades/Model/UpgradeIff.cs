using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Upgrades.Model
{
    public class UpgradeIff
    {
        /// <summary>
        /// The name of the iff to upgrade.
        /// </summary>
        public string Name;

        /// <summary>
        /// Default subsitutions to apply to every object in this iff. (BEFORE applying upgrades)
        /// </summary>
        public List<UpgradeSubstitution> Subs = new List<UpgradeSubstitution>();

        /// <summary>
        /// Upgrade Levels that objects in this iff can use.
        /// </summary>
        public List<UpgradeLevel> Upgrades = new List<UpgradeLevel>();

        /// <summary>
        /// Configuration determining which Upgrade Levels can be used for specific objects, which one they start on, and other modifiers.
        /// </summary>
        public List<ObjectUpgradeConfig> Config = new List<ObjectUpgradeConfig>();
    }
}
