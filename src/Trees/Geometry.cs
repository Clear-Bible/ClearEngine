using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using Utilities;

namespace Trees
{
    public class Geometry
    {
        public static string GetTreeSkeleton(XmlNode treeNode)
        {
            StringBuilder sb = new StringBuilder();
            XmlTextWriter xw = new XmlTextWriter(new StringWriter(sb));
            GetTreeSkeleton(treeNode, ref xw);
            string skeleton = sb.ToString();
            return skeleton;
        }

        private static void GetTreeSkeleton(XmlNode treeNode, ref XmlTextWriter xw)
        {
            if (treeNode.NodeType.ToString().Equals("Text")) // Terminal node
            {
                ;
            }
            else
            {
                xw.WriteStartElement("Node");

                XmlAttributeCollection attribs = treeNode.Attributes;

                if (attribs != null)
                {
                    for (int i = 0; i < attribs.Count; i++)
                    {
                        string attribName = attribs[i].Name;
                        string attribValue = attribs[i].Value;

                        if (attribName == "Cat" || attribName == "Rule")
                        {
                            xw.WriteAttributeString(attribName, attribValue);
                        }
                    }
                }

                if (treeNode.HasChildNodes)
                {
                    XmlNodeList subNodes = treeNode.ChildNodes;

                    for (int i = 0; i < subNodes.Count; i++)
                    {
                        GetTreeSkeleton(subNodes[i], ref xw);
                    }
                }
                xw.WriteEndElement();
            }
        }

        public static int NodeLength(XmlNode treeNode)
        {
            if (Utils.GetAttribValue(treeNode, "Start") == "") return 0;

            int start = Int32.Parse(Utils.GetAttribValue(treeNode, "Start"));
            int end = Int32.Parse(Utils.GetAttribValue(treeNode, "End"));

            return (end - start) + 1;
        }
    }
}
