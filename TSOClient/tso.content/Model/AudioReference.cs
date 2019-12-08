/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.Content.Model
{
    /// <summary>
    /// A reference to an audio file.
    /// </summary>
    public class AudioReference
    {
        /// <summary>
        /// ID of this audio file.
        /// </summary>
        public uint ID { get; set; }

        /// <summary>
        /// Type of this audio.
        /// </summary>
        public AudioType Type { get; set; }

        /// <summary>
        /// Path to this audio file.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Name of the audio file.
        /// </summary>
        public string Name
        {
            get
            {
                return Path.GetFileName(FilePath);
            }
        }
    }
}
