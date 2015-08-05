/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Vitaboy
{
    /// <summary>
    /// Wrapper class for SimAvatar with a default skeleton, "adult.skel".
    /// </summary>
    public class AdultVitaboyModel : SimAvatar
    {
        /// <summary>
        /// Constructs a new AdultVitaboyModel instance with a default skeleton, "adult.skel".
        /// </summary>
        public AdultVitaboyModel() : base(FSO.Content.Content.Get().AvatarSkeletons.Get("adult.skel"))
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
