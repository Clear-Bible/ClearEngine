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
        public static Dictionary<string, WordInfo> BuildWordInfoTable(XmlNode tree)
        {
            Dictionary<string, WordInfo> morphTable = new Dictionary<string, WordInfo>();

            List<XmlNode> terminalNodes = GetTerminalXmlNodes(tree);

            foreach (XmlNode terminalNode in terminalNodes)
            {
                WordInfo wi = new WordInfo();
                string id = Utils.GetAttribValue(terminalNode, "morphId");
                if (id.StartsWith("09020042"))
                {
                    ;
                }
                if (id.Length == 11) id += "1";
                wi.Surface = Utils.GetAttribValue(terminalNode, "Unicode");
                wi.Lemma = Utils.GetAttribValue(terminalNode, "UnicodeLemma");
                wi.Lang = Utils.GetAttribValue(terminalNode, "Language");
                wi.Morph = Utils.GetAttribValue(terminalNode, "Analysis");
                wi.Strong = Utils.GetAttribValue(terminalNode, "StrongNumberX");
                wi.Cat = Utils.GetAttribValue(terminalNode, "Cat");
                string type = string.Empty;
                if (wi.Lang == "G")
                {
                    type = Utils.GetAttribValue(terminalNode, "Type");
                }
                else
                {
                    type = Utils.GetAttribValue(terminalNode, "NounType");
                }
                if (wi.Cat == "noun" && type == "Proper") wi.Cat = "Name";

                morphTable.Add(id, wi);
            }

            return morphTable;
        }

        public static List<XmlNode> GetTerminalXmlNodes(XmlNode treeNode)
        {
            List<XmlNode> terminalNodes = new List<XmlNode>();
            GetTerminalXmlNodes(treeNode, terminalNodes);

            return terminalNodes;
        }

        public static void GetTerminalXmlNodes(XmlNode treeNode, List<XmlNode> terminalNodes)
        {
            if (treeNode.NodeType.ToString().Equals("Text")) // Terminal node
            {
                return;
            }

            if (!treeNode.HasChildNodes)
            {
                return;
            }

            if (treeNode.FirstChild.NodeType.ToString().Equals("Text")) // terminal ndoe
            {
                terminalNodes.Add(treeNode);
                return;
            }
            /*           if (treeNode.Attributes.GetNamedItem("Rule") != null && treeNode.Attributes.GetNamedItem("Rule").Value.EndsWith("X")) // Hebrew suffix as part of the word
                       {
                           terminalNodes.Add(treeNode);
                           return;
                       } */

            if (treeNode.HasChildNodes)
            {
                XmlNodeList subNodes = treeNode.ChildNodes;

                for (int i = 0; i < subNodes.Count; i++)
                {
                    GetTerminalXmlNodes(subNodes[i], terminalNodes);
                }
            }
        }
    }


}
