/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): Mats 'Afr0' Vederhus.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TSO.Vitaboy
{
    public class Bone
    {
        public int Unknown;
        public string Name;
        public string ParentName;

        public byte HasProps;
        public List<PropertyListItem> Properties = new List<PropertyListItem>();

        public Vector3 Translation;
        public Quaternion Rotation;

        public int CanTranslate;
        public int CanRotate;
        public int CanBlend;

        public float WiggleValue;
        public float WigglePower;

        public Bone[] Children;

        //Dummy & debug
        public Vector3 AbsolutePosition;
        public Matrix AbsoluteMatrix;

        public Bone Clone()
        {
            var result = new Bone
            {
                Unknown = this.Unknown,
                Name = this.Name,
                ParentName = this.ParentName,
                HasProps = this.HasProps,
                Properties = this.Properties,
                Translation = this.Translation,
                Rotation = this.Rotation,
                CanTranslate = this.CanTranslate,
                CanRotate = this.CanRotate,
                CanBlend = this.CanBlend,
                WiggleValue = this.WiggleValue,
                WigglePower = this.WigglePower
            };
            return result;
        }
    }

    /// <summary>
    /// An item in a skeleton's Property List.
    /// </summary>
    public class PropertyListItem
    {
        public List<KeyValuePair<string, string>> KeyPairs = new List<KeyValuePair<string, string>>();
    }
}
