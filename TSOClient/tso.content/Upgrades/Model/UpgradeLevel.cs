using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Upgrades.Model
{
    public class UpgradeLevel
    {
        /// <summary>
        /// The name of this upgrade level. If it is an integer, the name and description are taken from the default string set.
        /// </summary>
        public string Name;

        /// <summary>
        /// The price of this upgrade level. If it starts with $, it is a literal price. If it starts with R, it is a RELATIVE price.
        /// If neither, it is an 8 character hex GUID pointing to an object - whose catalog price should be used.
        /// </summary>
        public string Price;

        /// <summary>
        /// The ads of this upgrade level. A semicolon separated list of motive:value. (hunger, energy, comfort, bladder, hygiene, social, fun, room)
        /// Also includes special ads such as skilling (which can be given value 1)
        /// Can also be an 8 character hex GUID pointing to an object - whose ads should be used.
        /// </summary>
        public string Ad;

        /// <summary>
        /// A description for this upgrade level, shown in the editor. If the level is custom, this description will also be shown ingame.
        /// </summary>
        public string Description;


        /// <summary>
        /// A list of tuning substitutions for this level. For objects on this level, these substituions replace tuning values that govern how quickly
        /// they increase motives, skills or other benefits. The tuning values that are replaced can be from either OTF or BHAV chunks.
        /// </summary>
        public List<UpgradeSubstitution> Subs;
    }
}
