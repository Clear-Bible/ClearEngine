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
            string sourceIDTag = term.Attribute("morphId")?.Value ?? throw new InvalidDataException("terminal xelement doesn't have a morphId attribute.");
            if (sourceIDTag.Length == 11)
            {
                sourceIDTag += "1";
            }
            return new SourceID(sourceIDTag);
        }

        public static string Lemma(this XElement term) =>
            term.Attribute("UnicodeLemma")?.Value ?? throw new InvalidDataException("terminal xelement doesn't have a UnicodeLemma attribute.");

        public static string Surface(this XElement term) =>
            term.Attribute("Unicode")?.Value ?? throw new InvalidDataException("terminal xelement doesn't have a Unicode attribute.");

        public static string Strong(this XElement term) =>
            (term.Attribute("Language")?.Value ?? throw new InvalidDataException("terminal xelement doesn't have a Language attribute.")) +
            (term.Attribute("StrongNumberX")?.Value ?? throw new InvalidDataException("terminal xelement doesn't have a StrongNumberX attribute."));

        public static string English(this XElement term) =>
            term.Attribute("English")?.Value ?? throw new InvalidDataException("terminal xelement doesn't have an English attribute.");

        public static string Category(this XElement term) =>
            term.Attribute("Cat")?.Value ?? throw new InvalidDataException("terminal xelement doesn't have a Cat attribute.");

        public static int Start(this XElement term) =>
            int.Parse(term.Attribute("Start")?.Value ?? throw new InvalidDataException("terminal xelement doesn't have a morphId attribute."));

        public static string Analysis(this XElement term) =>
            term.Attribute("Analysis")?.Value ?? throw new InvalidDataException("terminal xelement doesn't have an Analysis attribute.");

        //public string PartOfSpeech { get; }


        public static TreeNodeID TreeNodeID(this XElement element)
        {
            return new TreeNodeID(element.Attribute("nodeId")?.Value ?? throw new InvalidDataException($"xelement {element.Name.LocalName} doesn't have a nodeId attribute."));
        }

        public static TreeNodeStackID TreeNodeStackID(this XElement node)
        {
            return node.TreeNodeID().TreeNodeStackID;
        }

        public static int AttrAsInt(this XElement element,string attributeName)
        {
            return int.Parse(element.Attribute(attributeName)?.Value ?? throw new InvalidDataException($"xelement {element.Name.LocalName} doesn't have a {attributeName} attribute."));
        }

        public static string SourceId(this XElement element)
        {
            string morphId = element.Attribute("morphId")?.Value ?? throw new InvalidDataException($"xelement {element.Name.LocalName} doesn't have a morphId attribute.");
            if (morphId.Length == 11) morphId += "1";
            return morphId;
        }
    }
}
