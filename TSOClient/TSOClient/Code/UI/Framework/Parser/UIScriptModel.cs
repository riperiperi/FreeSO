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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.UI.Framework.Parser
{
    public class UIGroup : UINode
    {
        public UINode SharedProperties { get; set; }
        public List<UINode> Children { get; set; }

        public static UIGroup FromReduction(GOLD.Reduction r)
        {
            UIGroup result = new UIGroup();
            // <Object> ::= BeginLiteral <Content> EndLiteral
            var content = (List<UINode>)r.get_Data(1);
            var sharedProps = content.FirstOrDefault(x => x.Name == "SetSharedProperties");

            result.SharedProperties = sharedProps;
            result.Children = content.Where(x => x != sharedProps).ToList();

            return result;
        }
    }

    public class UISharedProperties
    {
        public static UISharedProperties FromReduction(GOLD.Reduction r)
        {
            UISharedProperties result = new UISharedProperties();
            return result;
        }
    }

    public class UINode
    {
        public string Name { get; set; }
        public string ID { get; set; }

        public Dictionary<string, string> Attributes { get; internal set; }

        public UINode()
        {
            Attributes = new Dictionary<string, string>();
        }

        public Vector2 GetVector2(string name)
        {
            var att = Attributes[name];
            if (att != null)
            {
                /** Remove ( ) **/
                att = att.Substring(1, att.Length - 2);
                var parts = att.Split(new char[] { ',' });

                return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
            }
            return Vector2.Zero;
        }

        public Color GetColor(string name)
        {
            var att = Attributes[name];
            if (att != null)
            {
                return UIScript.ParseRGB(att);
            }
            return default(Color);
        }

        public Point GetPoint(string name)
        {
            var att = Attributes[name];
            if (att != null)
            {
                /** Remove ( ) **/
                att = att.Substring(1, att.Length - 2);
                var parts = att.Split(new char[] { ',' });

                return new Point(int.Parse(parts[0]), int.Parse(parts[1]));
            }
            return Point.Zero;
        }

        public void AddAtts(Dictionary<string, string> attributes)
        {
            foreach (var att in attributes)
            {
                if (!Attributes.ContainsKey(att.Key))
                {
                    this[att.Key] = att.Value;
                }
            }
        }

        public string this[string name]
        {
            get
            {
                return Attributes[name];
            }
            set
            {
                Attributes[name] = value;
            }
        }
    }
}
