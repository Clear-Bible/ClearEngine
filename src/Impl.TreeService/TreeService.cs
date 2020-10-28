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
            ChapterID chapterID,
            string treeFolder,
            Dictionary<string, XmlNode> trees,
            Dictionary<string, string> bookNames)
        {
            int bookNumber = chapterID.Book;           
            int chapterNumber = chapterID.Chapter;

            string bookName = (string)bookNames[$"{bookNumber:D2}"];

            string treeFile = Path.Combine(treeFolder, $"{bookName}{chapterNumber:D3}.trees.xml");

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
