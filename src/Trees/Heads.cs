using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Data;
using System.Xml;
using Utilities;

namespace Trees
{
    public class Heads
    {
        public static XmlNode GetHead(XmlNode treeNode)
        {
            ArrayList terminalCats = Terminals.ListTerminalCats();
            return GetHeadNode(treeNode, terminalCats);
        }
            
        private static XmlNode GetHeadNode( XmlNode treeNode, ArrayList terminalCats )
		{
			XmlNode headNode = null;

			string cat = treeNode.Attributes.GetNamedItem("Cat").Value.ToString();
			if ( terminalCats.Contains(cat) )
			{
				headNode = treeNode;
			}
            else if (treeNode.Attributes.GetNamedItem("Rule") != null && treeNode.Attributes.GetNamedItem("Rule").Value.EndsWith("X")) // Hebrew suffix as part of the word
            {
                headNode = treeNode;
            }
            else
            {
                if (Utils.GetAttribValue(treeNode, "Head") != string.Empty)
                {
                    int head = Int32.Parse(treeNode.Attributes.GetNamedItem("Head").Value);
                    XmlNodeList subNodes = treeNode.ChildNodes;
                    if (head >= treeNode.ChildNodes.Count) // for peculiarities resulting from the coversion of sentence-trees to verse trees
                    {
                        head = head - treeNode.ChildNodes.Count;
                    }

                    headNode = GetHeadNode(subNodes[head], terminalCats);
                }
            }

			return headNode;
		}

        public static ArrayList GetChineseHeads(XmlNode treeNode)
        {
            ArrayList terminalCats = Terminals.ListTerminalCats();
            ArrayList terminalRules = Terminals.ListTerminalRules();
            ArrayList terminalWordTypes = Terminals.ListTerminalterminalWordTypes();
            ArrayList heads = new ArrayList();

            GetChineseHeadNodes(treeNode, ref heads, terminalCats, terminalRules, terminalWordTypes);

            return heads;
        }

        private static void GetChineseHeadNodes(XmlNode treeNode, ref ArrayList heads, ArrayList terminalCats, ArrayList terminalRules, ArrayList terminalWordTypes)
        {
            string cat = treeNode.Attributes.GetNamedItem("Cat").Value.ToString();
            string rule = string.Empty;
            if (treeNode.Attributes.GetNamedItem("Rule") != null) rule = treeNode.Attributes.GetNamedItem("Rule").Value;
            string wordType = string.Empty;
            if (treeNode.Attributes.GetNamedItem("WordType") != null) wordType = treeNode.Attributes.GetNamedItem("WordType").Value;
            string lemma = string.Empty;
            if (treeNode.Attributes.GetNamedItem("Lemma") != null) lemma = treeNode.Attributes.GetNamedItem("Lemma").Value;
            int start = Int32.Parse(Utils.GetAttribValue(treeNode, "Start"));
            int end = Int32.Parse(Utils.GetAttribValue(treeNode, "End"));
            int length = end - start + 1;

            if (terminalCats.Contains(cat) && treeNode.FirstChild.NodeType.ToString().Equals("Text"))
            {
                heads.Add(treeNode);
                return;
            }
            else 
            {
                if (treeNode.ChildNodes.Count > 1 && (terminalRules.Contains(rule) || terminalWordTypes.Contains(wordType)))
                {
                    heads.Add(treeNode);
                }

                if (rule == "CP-NP" && length <= 5)
                {
                    heads.Add(treeNode);
                }
                if (rule == "VbuDir") heads.Add(treeNode);

                if (treeNode.Attributes.GetNamedItem("Coord") != null)
                {
                    ArrayList coordNodes = GetCoords(treeNode);
                    foreach (XmlNode coordNode in coordNodes)
                    {
                        GetChineseHeadNodes(coordNode, ref heads, terminalCats, terminalRules, terminalWordTypes);
                    }
                }
                else
                {
                    int head = Int32.Parse(treeNode.Attributes.GetNamedItem("Head").Value);
                    if (rule == "CP-NP" && lemma == "人") head = 0;
                    if (rule == "Cause-VP") head = 1;
                    if (rule == "CD-Mezhr") head = 0;
                    
                    XmlNodeList subNodes = treeNode.ChildNodes;

                    if (rule == "NrSfx" || rule == "NP-LC" || rule == "CD-Mezhr")
                    {
                        GetChineseHeadNodes(subNodes[0], ref heads, terminalCats, terminalRules, terminalWordTypes);
                        GetChineseHeadNodes(subNodes[1], ref heads, terminalCats, terminalRules, terminalWordTypes);
                    }
                    else
                    {
                        GetChineseHeadNodes(subNodes[head], ref heads, terminalCats, terminalRules, terminalWordTypes);
                    }
                }
            }
        }

        private static ArrayList GetCoords(XmlNode treeNode)
        {
            ArrayList coordNodes = new ArrayList();

            XmlNodeList subNodes = treeNode.ChildNodes;

            for (int i = 0; i < subNodes.Count; i++)
            {
                string cat = subNodes[i].Attributes.GetNamedItem("Cat").Value;

                if (!(cat == "cjp" || cat == "cj" || cat == "conj" || cat == "CC" || cat == "PU"))
                {
                    coordNodes.Add(subNodes[i]);
                }
            }

            return coordNodes;
        }
    }
}
