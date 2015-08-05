/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.UI.Framework.Parser
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class UIAttribute : System.Attribute
    {
        public string Name { get; set; }
        public Type Parser { get; set; }
        public UIAttributeType DataType = UIAttributeType.Unknown;

        public UIAttribute(string name)
        {
            this.Name = name;
        }

        public UIAttribute(string name, Type parser)
        {
            this.Name = name;
            this.Parser = parser;
        }
    }

    public interface UIAttributeParser
    {
        void ParseAttribute(UINode node);
    }

}
