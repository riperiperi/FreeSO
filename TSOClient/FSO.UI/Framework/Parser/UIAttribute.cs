using System;

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
