using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using SimsLib;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Microsoft.Xna.Framework;
using TSOClient.LUI;
using TSOClient.Code.Utils;

namespace TSOClient.Code.UI.Framework.Parser
{
    public class UIScript
    {
        private static Color MASK_COLOR = new Color(0xFF, 0x00, 0xFF);


        /// <summary>
        /// Nodes which represent functions
        /// </summary>
        private static string[] FUNCTIONS = new string[]{ "DefineString", "DefineImage", "AddButton" };
        private Dictionary<string, string> Strings;
        private Dictionary<string, Texture2D> Textures;
        private GraphicsDevice gd;

        private UIContainer target;
        private Type targetType;

        public UIScript(GraphicsDevice gd, UIContainer target)
        {
            this.gd = gd;
            this.target = target;
            this.targetType = target.GetType();
            Strings = new Dictionary<string, string>();
            Textures = new Dictionary<string, Texture2D>();
        }


        /// <summary>
        /// Handles AddButton nodes in a UIScript.
        /// This method will:
        ///  * Create a button,
        ///  * Assign all the properties from the UIScript
        ///  * Add to the display list
        ///  * Wire up against any members with the same name in the target class
        /// </summary>
        /// <param name="node"></param>
        public void AddButton(UINode node)
        {
            UIButton btn = new UIButton();
            DoSetControlProperties(btn, node);
            target.Add(btn);
            WireUp(node.ID, btn);
        }


        /// <summary>
        /// Applys the various settings in the UINode to the control
        /// provided
        /// </summary>
        /// <param name="control"></param>
        /// <param name="node"></param>
        private void DoSetControlProperties(object control, UINode node)
        {
            if (control == null) { return; }

            /** Get UIAttribute decorations **/
            var atts = GetTypeFields(control.GetType());
            
            foreach (var att in node.Attributes)
            {
                if (atts.ContainsKey(att.Key))
                {
                    var uiAtt = atts[att.Key];
                    var value = GetAtt(node, att.Key, uiAtt.Converter);
                    uiAtt.Field.SetValue(control, value, new object[] { });
                }
            }
        }


        /// <summary>
        /// Gets 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        private object GetAtt(UINode node, string name, UITypeConverter converter)
        {
            switch (converter)
            {
                case UITypeConverter.Point:
                    return node.GetPoint(name);
                case UITypeConverter.Texture:
                    return Textures[node[name]];
            }
            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public void DefineString(UINode node)
        {
            var stringValue = GameFacade.Strings[node["stringDir"], node["stringTable"], node["stringIndex"]];
            Strings.Add(node.ID, stringValue);
            WireUp(node.ID, stringValue);
        }

        /// <summary>
        /// Handles the DefineImage nodes in the UIScript
        /// </summary>
        /// <param name="node"></param>
        public void DefineImage(UINode node)
        {
            var assetID = node["assetID"];
            var assetNum = ulong.Parse(assetID.Substring(2), NumberStyles.HexNumber);
            try
            {
                var assetData = ContentManager.GetResourceFromLongID(assetNum);
                Texture2D texture = Texture2D.FromFile(gd, new MemoryStream(assetData));
                TextureUtils.ManualTextureMask(ref texture, MASK_COLOR);

                Textures.Add(node.ID, texture);
                WireUp(node.ID, texture);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetString(string id)
        {
            return Strings[id];
        }

        

        /// <summary>
        /// Tries to assign a value created by the UIScript to a
        /// property in the target class
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        private void WireUp(string id, object value)
        {
            var prop = targetType.GetProperty(id);
            if (prop != null)
            {
                prop.SetValue(target, value, new object[] { });
            }
        }



        public void Parse(string path)
        {
            var parser = new UIScriptParser();
            parser.Setup();
            using (var reader = File.OpenText(path))
            {
                var didParse = parser.Parse(reader);
                if (didParse)
                {
                    var children = (List<UINode>)parser.program;
                    this.Process((UIGroup)children.First());
                }
            }
        }


        public void Process(UIGroup content)
        {
            ProcessUIGroup(content, new Dictionary<string, string>());
        }


        private void ProcessUIGroup(UIGroup content, Dictionary<string, string> shared)
        {
            var sharedNode = content.SharedProperties;
            if (sharedNode != null)
            {
                foreach (var att in sharedNode.Attributes)
                {
                    shared.Add(att.Key, att.Value);
                }
            }

            var type = typeof(UIScript);
            var funcSig = new Type[] { typeof(UINode) };

            foreach (var child in content.Children)
            {
                child.AddAtts(shared);
                if (child is UIGroup)
                {
                    ProcessUIGroup((UIGroup)child, CollectionUtils.Clone(shared));
                }
                else
                {
                    if (FUNCTIONS.Contains(child.Name))
                    {
                        var m = type.GetMethod(child.Name, funcSig);
                        m.Invoke(this, new object[] { child });
                    }
                }
            }
        }



        private Dictionary<Type, Dictionary<string, UIAttField>> FieldCache
            = new Dictionary<Type, Dictionary<string, UIAttField>>();

        /// <summary>
        /// Gets a map of string => UIAttField.
        /// The map indicates what UIScript attribute name e.g. "position"
        /// maps to in the target type via a [UIAttribute] decoration
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Dictionary<string, UIAttField> GetTypeFields(Type type)
        {
            if (FieldCache.ContainsKey(type))
            {
                return FieldCache[type];
            }


            PropertyInfo[] infos =
                type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            var idMap = new Dictionary<string, UIAttField>();

            foreach (var field in infos)
            {
                var atts = field.GetCustomAttributes(typeof(UIAttribute), true);
                if (atts.Length > 0)
                {
                    var convertType = UITypeConverter.Unknown;
                    var fieldType = field.PropertyType;

                    if (fieldType.IsAssignableFrom(typeof(Texture2D)))
                    {
                        convertType = UITypeConverter.Texture;
                    }
                    else if (fieldType.IsAssignableFrom(typeof(Point)))
                    {
                        convertType = UITypeConverter.Point;
                    }

                    foreach (UIAttribute att in atts)
                    {


                        idMap.Add(att.Name, new UIAttField
                        {
                            Field = field,
                            Converter = convertType
                        });
                    }

                }
            }

            FieldCache.Add(type, idMap);
            return idMap;
        }
    }


    public enum UITypeConverter
    {
        Point,
        Texture,
        Unknown
    }

    public class UIAttField
    {
        public PropertyInfo Field;
        public UITypeConverter Converter;
    }

}
