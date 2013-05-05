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

namespace TSOClient.Code.UI.Framework.Parser
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
