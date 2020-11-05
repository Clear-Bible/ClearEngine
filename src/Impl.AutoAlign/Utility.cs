using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using WordInfo = GBI_Aligner.WordInfo;
using Utils = Utilities.Utils;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Data;
    using ClearBible.Clear3.Impl.TreeService;
    using ClearBible.Clear3.Miscellaneous;

    public class AutoAlignUtility
    {
        public static Dictionary<string, WordInfo> BuildWordInfoTable(
            XElement tree)
        {           
            return
                GetTerminalXmlNodes(tree)
                .ToDictionary(
                    node => GetSourceIdFromTerminalXmlNode(node),
                    node => GetWordInfoFromTerminalXmlNode(node));
        }

        public static List<XElement> GetTerminalXmlNodes(XElement treeNode)
        {
            return treeNode
                .Descendants()
                .Where(e => e.FirstNode is XText)
                .ToList();
        }

        public static string GetSourceIdFromTerminalXmlNode(XElement node)
        {
            string sourceId = node.Attribute("morphId").Value;
            if (sourceId.Length == 11) sourceId += "1";
            return sourceId;
        }

        public static WordInfo GetWordInfoFromTerminalXmlNode(XElement node)
        {
            string language = node.Attribute("Language").Value;

            string type =
                node.AttrAsString(language == "G" ? "Type" : "NounType");

            string category = node.Attribute("Cat").Value;
            if (category == "noun" && type == "Proper")
                category = "Name";

            return new WordInfo()
            {
                Lang = language,
                Strong = node.Attribute("StrongNumberX").Value,
                Surface = node.Attribute("Unicode").Value,
                Lemma = node.Attribute("UnicodeLemma").Value,
                Cat = category,
                Morph = node.Attribute("Analysis").Value
            };               
        }       
    }
}
