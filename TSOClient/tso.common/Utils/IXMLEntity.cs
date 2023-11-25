using System;
using System.Xml;

namespace FSO.Common.Utils
{
    public interface IXMLEntity
    {
        XmlElement Serialize(XmlDocument doc);
        void Parse(XmlElement element);
    }

    public static class XMLUtils
    {
        public static T Parse<T>(string data) where T : IXMLEntity
        {
            var doc = new XmlDocument();
            doc.LoadXml(data);

            T result = (T)Activator.CreateInstance(typeof(T));
            result.Parse((XmlElement)doc.FirstChild);
            return result;
        }

        public static void AppendTextNode(this XmlElement e, string nodeName, string value)
        {
            var node = e.OwnerDocument.CreateElement(nodeName);
            node.AppendChild(e.OwnerDocument.CreateTextNode(value));
            e.AppendChild(node);
        }

        public static string ReadTextNode(this XmlElement e, string nodeName)
        {
            foreach (XmlElement child in e.ChildNodes)
            {
                if (child.Name == nodeName && child.FirstChild != null)
                {
                    return child.FirstChild?.Value;
                }
            }
            return null;
        }
    }
}
