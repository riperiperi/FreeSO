using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.HIT;

namespace TSO.HIT.model
{
    /// <summary>
    /// Groups related HIT resources, like the tsov2 series or newmain.
    /// </summary>
    public class HITResourceGroup
    {
        public EVT evt; //don't have support for HIT or HOT yet.
        public HITFile hit;
    }
}
