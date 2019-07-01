using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Upgrades.Model
{
    public class ObjectUpgradeConfig
    {
        /// <summary>
        /// The GUID for the target object.
        /// </summary>
        public string GUID;

        /// <summary>
        /// The initial level this object should start at.
        /// </summary>
        public int Level;

        /// <summary>
        /// The upgrade level this object should go up to.
        /// </summary>
        public int? Limit;

        /// <summary>
        /// If true, the initial object level uses its builtin tuning rather than the replacement.
        /// </summary>
        public bool? Special;
    }
}
