using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Utilities;


namespace ClearBible.Clear3.Impl.TreeService
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Data;

    public class TreeService
    {
        public void GetChapterTree(
            string chapterID,
            string treeFolder,
            Dictionary<string, XmlNode> trees,
            Dictionary<string, string> bookNames)
        {
            string bookNumber = chapterID.Substring(0, 2);           
            string chapterNumber = chapterID.Substring(2, 3);

            string bookName = (string)bookNames[bookNumber];

            string treeFile = Path.Combine(treeFolder, $"{bookName}{chapterNumber}.trees.xml");

            if (File.Exists(treeFile))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(treeFile);

                XmlNodeList sentences = xmlDoc.SelectNodes("Sentences/Sentence");

                foreach (XmlNode sentence in sentences)
                {
                    XmlNode treeNode = sentence.FirstChild.FirstChild.FirstChild;

                    string verseID = Utils.GetAttribValue(treeNode, "nodeId").Substring(0, 8);

                    trees.Add(verseID, treeNode);
                }
            }
        }
    }
}
