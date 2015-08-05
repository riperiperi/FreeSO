/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace FSO.Vitaboy
{
    /// <summary>
    /// Bones are used to animate characters. They hold rotation and translation data.
    /// </summary>
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
        public int Index;

        public float WiggleValue;
        public float WigglePower;

        public Bone[] Children;

        //Dummy & debug
        public Vector3 AbsolutePosition;
        public Matrix AbsoluteMatrix;

        /// <summary>
        /// Clones this bone.
        /// </summary>
        /// <returns>A Bone instance with the same values as this one.</returns>
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
                WigglePower = this.WigglePower,
                Index = this.Index
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
