using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Xml;

using Utilities;

namespace GBI_Aligner
{
    class VerseTrees
    {
        public static Dictionary<string, XmlNode> GetVerseTrees(string treeFolder)
        {
            Dictionary<string, XmlNode> verseTrees = new Dictionary<string, XmlNode>();

            string[] files = Directory.GetFiles(treeFolder, "*.trees.xml");

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                GetVerseTrees(file, verseTrees);
            }

            return verseTrees;
        }

        public static void GetChapterTree(string chapterID, string treeFolder, Dictionary<string, XmlNode> trees, Dictionary<string, string> bookNames)
        {
            string bookNumber = chapterID.Substring(0, 2);
            string bookName = (string)bookNames[bookNumber];
            string chapterNumber = chapterID.Substring(2, 3);
            string treeFile = Path.Combine(treeFolder , bookName + chapterNumber + ".trees.xml");      
            if (File.Exists(treeFile))
            {
                GetVerseTrees(treeFile, trees);
            }
        }

        public static void GetChapterTree2(string chapterID, string treeFolder, Dictionary<string, XmlNode> trees, Dictionary<string, string> bookNames)
        {
            string bookNumber = chapterID.Substring(0, 2);
            string bookName = (string)bookNames[bookNumber];
            string chapterNumber = chapterID.Substring(2, 3);
            string treeFile = Path.Combine(treeFolder, bookName + chapterNumber + ".trees.xml");
            if (File.Exists(treeFile))
            {
                GetVerseTrees2(treeFile, trees);
            }
        }

        static void GetVerseTrees(string file, Dictionary<string, XmlNode> verseTrees)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            XmlNodeList verses = xmlDoc.SelectNodes("Sentences/Sentence");

            foreach(XmlNode verse in verses)
            {
                XmlNode treeNode = verse.FirstChild.FirstChild.FirstChild;
                string verseID = Utils.GetAttribValue(treeNode, "nodeId").Substring(0, 8);
                verseTrees.Add(verseID, treeNode);
            }
        }

        static void GetVerseTrees2(string file, Dictionary<string, XmlNode> verseTrees)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            XmlNodeList verses = xmlDoc.SelectNodes("Sentences/Sentence");

            foreach (XmlNode verse in verses)
            {
                XmlNode treeNode = verse.FirstChild;
                string verseID = Utils.GetAttribValue(treeNode.FirstChild.FirstChild, "nodeId").Substring(0, 8);
                verseTrees.Add(verseID, treeNode.FirstChild.FirstChild);
            }
        }

        public static XmlNode CombineSubtrees(List<XmlNode> subTrees)
        {
            int nodeLength = ComputeNodeLength(subTrees);
            string nodeID = GetNodeID(subTrees, nodeLength);
            StringBuilder sb = new StringBuilder();
            XmlTextWriter xw = new XmlTextWriter(new StringWriter(sb));
            xw.Formatting = Formatting.Indented;
            xw.WriteStartElement("Node");
            xw.WriteAttributeString("Cat", "S");
            xw.WriteAttributeString("Head", "0");
            xw.WriteAttributeString("nodeId", nodeID);
            xw.WriteAttributeString("Length", nodeLength.ToString());

            foreach(XmlNode subTree in subTrees)
            {
                subTree.WriteTo(xw);
            }

            xw.WriteEndElement();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(sb.ToString());
            XmlNode root = doc.DocumentElement;

//            ReDoOffSets(ref root);

            return root;
        }

        public static XmlNode CombineTrees(List<XmlNode> trees)
        {
            List<XmlNode> topTreeNodes = new List<XmlNode>();
            foreach(XmlNode tree in trees)
            {
                foreach (XmlNode childNode in tree.ChildNodes)
                {
                    topTreeNodes.Add(childNode);
                }
            }
            XmlNode toptree = CombineSubtrees(topTreeNodes);

            StringBuilder sb = new StringBuilder();
            XmlTextWriter xw = new XmlTextWriter(new StringWriter(sb));
            xw.Formatting = Formatting.Indented;
            toptree.WriteTo(xw);

            /*            xw.WriteStartElement("Trees");
                        xw.WriteStartElement("Tree");

                        xw.WriteEndElement();

                       foreach (XmlNode tree in trees)
                        {
                            for (int i = 1; i < tree.ChildNodes.Count; i++)
                            {
                                XmlNode derivedTree = (XmlNode)tree.ChildNodes[i].FirstChild;
                                xw.WriteStartElement("Tree");
                                derivedTree.WriteTo(xw);
                                xw.WriteEndElement();
                            }
                        } 


                        xw.WriteEndElement(); */

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(sb.ToString());
            XmlNode root = doc.DocumentElement;

            return root;
        }

        static int ComputeNodeLength(List<XmlNode> subTrees)
        {
            int nodeLength = 0;

            foreach(XmlNode subTree in subTrees)
            {
                int len = Int32.Parse(Utils.GetAttribValue(subTree, "Length"));
                nodeLength += len;
            }

            return nodeLength;
        }

        static string GetNodeID(List<XmlNode> subTrees, int nodeLength)
        {
            XmlNode firstNode = subTrees[0];
            string nodeID = Utils.GetAttribValue(firstNode, "nodeId");
            nodeID = nodeID.Substring(0, 11) + Utils.Pad3(nodeLength.ToString()) + "0";
            return nodeID;
        }

        private static void ReDoOffSets(ref XmlNode treeNode)
        {
            int start = 0;
            ReDoOffSets(ref treeNode, ref start);
        }

        private static void ReDoOffSets(ref XmlNode treeNode, ref int start)
        {
            if (treeNode.NodeType.ToString() == "Text")
            {
                return;
            }

            int end = start + treeNode.InnerText.Length - 1;

            XmlAttribute startNode = null;
            XmlAttribute endNode = null;

            if (treeNode.Attributes.GetNamedItem("Start") != null)
            {
                startNode = (XmlAttribute)treeNode.Attributes.GetNamedItem("Start");
                startNode.Value = start.ToString();
            }
            else
            {
                startNode = treeNode.OwnerDocument.CreateAttribute("Start");
                startNode.Value = start.ToString();
                treeNode.Attributes.Append(startNode);
            }
            if (treeNode.Attributes.GetNamedItem("End") != null)
            {
                endNode = (XmlAttribute)treeNode.Attributes.GetNamedItem("End");
                endNode.Value = end.ToString();
            }
            else
            {
                endNode = treeNode.OwnerDocument.CreateAttribute("End");
                endNode.Value = end.ToString();
                treeNode.Attributes.Append(endNode);
            }

            XmlNodeList subNodes = treeNode.ChildNodes;

            for (int i = 0; i < subNodes.Count; i++)
            {
                XmlNode subNode = subNodes[i];
                ReDoOffSets(ref subNode, ref start);
            }

            start = end + 1;
        }
    }
}
