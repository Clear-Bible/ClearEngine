using System;
using System.Xml.Linq;

namespace ClearBible.Engine.TreeAligner.Legacy
{
    public static class Extensions
    {
        public static List<XElement> GetTerminalNodes(this XElement treeNode)
        {
            return treeNode
                .Descendants()
                .Where(e => e.FirstNode is XText)
                .ToList();
        }

        public static SourceID SourceID(this XElement term)
        {
            string sourceIDTag = term.Attribute("morphId").Value;
            if (sourceIDTag.Length == 11)
            {
                sourceIDTag += "1";
            }
            return new SourceID(sourceIDTag);
        }

        public static string Lemma(this XElement term) =>
            term.Attribute("UnicodeLemma").Value;

        public static string Surface(this XElement term) =>
            term.Attribute("Unicode").Value;

        public static string Strong(this XElement term) =>
            term.Attribute("Language").Value +
            term.Attribute("StrongNumberX").Value;

        public static string English(this XElement term) =>
            term.Attribute("English").Value;

        public static string Category(this XElement term) =>
            term.Attribute("Cat").Value;

        public static int Start(this XElement term) =>
            int.Parse(term.Attribute("Start").Value);

        public static string Analysis(this XElement term) =>
            term.Attribute("Analysis").Value;

        //public string PartOfSpeech { get; }


        public static TreeNodeID TreeNodeID(this XElement node)
        {
            return new TreeNodeID(node.Attribute("nodeId").Value);
        }

        public static TreeNodeStackID TreeNodeStackID(this XElement node)
        {
            return node.TreeNodeID().TreeNodeStackID;
        }

        public static int AttrAsInt(this XElement element,string attributeName)
        {
            return int.Parse(element.Attribute(attributeName).Value);
        }

        public static string SourceId(this XElement element)
        {
            string morphId = element.Attribute("morphId").Value;
            if (morphId.Length == 11) morphId += "1";
            return morphId;
        }
    }
}
