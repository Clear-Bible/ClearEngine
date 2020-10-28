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


        // FIXME would prefer to use newer Linq-to-XML library
        //
        private Dictionary<string, XmlNode> _trees =
            new Dictionary<string, XmlNode>();

        private ChapterID _currentChapterID = ChapterID.None;

        private string _treeFolder;

        // FIXME would prefer Dictionary<int, string>
        //
        private Dictionary<string, string> _bookNames;


        public TreeService(
            string treeFolder,
            Dictionary<string, string> bookNames)
        {
            _treeFolder = treeFolder;
            _bookNames = bookNames;
        }


        public void PreloadTreesForChapter(ChapterID chapterID)
        {
            if (!chapterID.Equals(_currentChapterID))
            {
                _trees.Clear();
                // Get the trees for the current chapter; a verse can cross chapter boundaries

                GetChapterTree(chapterID, _treeFolder, _trees, _bookNames);
                int book = chapterID.Book;
                int chapter = chapterID.Chapter;
                ChapterID prevChapterID = new ChapterID(book, chapter - 1);
                GetChapterTree(prevChapterID, _treeFolder, _trees, _bookNames);
                ChapterID nextChapterID = new ChapterID(book, chapter + 1);
                GetChapterTree(nextChapterID, _treeFolder, _trees, _bookNames);
                _currentChapterID = chapterID;
            }
        }

        public Dictionary<string, XmlNode> Legacy => _trees;
    }
}
