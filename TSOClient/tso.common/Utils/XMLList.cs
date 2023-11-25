using System;
using System.Collections.Generic;
using System.Xml;

namespace FSO.Common.Utils
{
    public class XMLList<T> : List<T>, IXMLEntity where T : IXMLEntity
    {
        private string NodeName;

        public XMLList(string nodeName)
        {
            this.NodeName = nodeName;
        }

        public XMLList()
        {
            this.NodeName = "Unknown";
        }

        #region IXMLPrinter Members

        public System.Xml.XmlElement Serialize(System.Xml.XmlDocument doc)
        {
            var element = doc.CreateElement(NodeName);
            foreach (var child in this)
            {
                element.AppendChild(child.Serialize(doc));
            }
            return element;
        }

        public void Parse(System.Xml.XmlElement element)
        {
            var type = typeof(T);

            foreach (XmlElement child in element.ChildNodes)
            {
                var instance = (T)Activator.CreateInstance(type);
                instance.Parse(child);
                this.Add(instance);
            }
        }

        #endregion
    }
}
