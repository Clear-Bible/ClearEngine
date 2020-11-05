using System;
using System.Xml;
using System.Xml.Linq;

namespace ClearBible.Clear3.Miscellaneous
{
    public static class XmlExtensions
    {
        public static XElement ToXElement(this XmlNode node)
        {
            XDocument xDoc = new XDocument();
            using (XmlWriter xmlWriter = xDoc.CreateWriter())
                node.WriteTo(xmlWriter);
            return xDoc.Root;
        }

        public static XmlNode ToXmlNode(this XElement element)
        {
            using (XmlReader xmlReader = element.CreateReader())
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlReader);
                return xmlDoc.DocumentElement;
            }
        }

        public static string AttrAsString(
            this XElement element,
            string attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);
            return attribute is null
                ? ""
                : attribute.Value;
        }
    }
}
