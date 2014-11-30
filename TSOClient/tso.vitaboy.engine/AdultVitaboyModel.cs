/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Vitaboy
{
    /// <summary>
    /// Wrapper class for SimAvatar with a default skeleton, "adult.skel".
    /// </summary>
    public class AdultVitaboyModel : SimAvatar
    {
        /// <summary>
        /// Constructs a new AdultVitaboyModel instance with a default skeleton, "adult.skel".
        /// </summary>
        public AdultVitaboyModel() : base(TSO.Content.Content.Get().AvatarSkeletons.Get("adult.skel"))
        {
        }

        /// <summary>
        /// Constructs a new AdultVitaboyModel instance from an old one.
        /// </summary>
        /// <param name="old">The old instance.</param>
        public AdultVitaboyModel(AdultVitaboyModel old) : base(old) {
        }
    }
}
