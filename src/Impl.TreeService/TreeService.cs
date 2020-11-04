using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Utilities;


namespace ClearBible.Clear3.Impl.TreeService
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Data;
    using ClearBible.Clear3.Miscellaneous;

    public class TreeService : ITreeService
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
                XElement xml = XElement.Load(treeFile);

                foreach (XElement node in xml
                    .Descendants("Sentence")
                    .Select(s => s.Descendants("Node").First()))
                {
                    string verseId =
                        node.Attribute("nodeId").Value.Substring(0, 8);
                    _trees2[verseId] = node;
                }

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

        private Dictionary<string, XElement> _trees2 =
            new Dictionary<string, XElement>();

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
                _trees2.Clear();
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

        public XmlNode GetTreeNode(VerseID start, VerseID end)
        {
            string sStartVerseID = $"{start.Book:D2}{start.Chapter:D3}{start.Verse:D3}";

            string sEndVerseID = $"{end.Book:D2}{end.Chapter:D3}{end.Verse:D3}";

            List<XElement> subTrees = GetSubTrees(start, end);

            if (subTrees.Count == 1)
            {
                return subTrees[0].ToXmlNode();
            }
            else
            {
                return CombineTrees(subTrees).ToXmlNode();
            }
        }

        List<XElement> GetSubTrees(VerseID start, VerseID end)
        {
            List<XElement> subTrees = new List<XElement>();

            int book = start.Book;
            int startChapter = start.Chapter;
            int endChapter = end.Chapter;

            if (startChapter == endChapter)
            {
                GetSubTreesInSameChapter(start, end, book, startChapter, subTrees);
            }
            else
            {
                GetSubTreesInDiffChapter(start, end, book, startChapter, endChapter, ref subTrees);
            }

            return subTrees;
        }

        void GetSubTreesInSameChapter(VerseID sStartVerseID, VerseID sEndVerseID, int book, int chapter, List<XElement> subTrees)
        {
            int startVerse = sStartVerseID.Verse;
            int endVerse = sEndVerseID.Verse;

            for (int i = startVerse; i <= endVerse; i++)
            {
                string verseID = $"{book:D2}{chapter:D3}{i:D3}";

                if (_trees2.TryGetValue(verseID, out XElement subTree))
                {
                    subTrees.Add(subTree);
                }
                else
                {
                    break;
                }
            }
        }

        void GetSubTreesInDiffChapter(VerseID sStartVerseID, VerseID sEndVerseID, int book, int chapter1, int chapter2, ref List<XElement> subTrees)
        {
            VerseID hypotheticalLastVerse = new VerseID(book, chapter1, 100);           
            GetSubTreesInSameChapter(sStartVerseID, hypotheticalLastVerse, book, chapter1, subTrees);

            VerseID hypotheticalFirstVerse = new VerseID(book, chapter2, 1);
            GetSubTreesInSameChapter(hypotheticalFirstVerse, sEndVerseID, book, chapter2, subTrees);
        }


        XElement CombineTrees(List<XElement> trees)
        {
            List<XElement> subTrees =
                trees.SelectMany(t => t.Elements()).ToList();

            int totalLength = subTrees
                .Select(x => Int32.Parse(x.Attribute("Length").Value))
                .Sum();

            string newNodeId =
                subTrees[0].Attribute("nodeId").Value.Substring(0, 11) +
                $"{totalLength:D3}";

            return
                new XElement("Node",
                    new XAttribute("Cat", "S"),
                    new XAttribute("Head", "0"),
                    new XAttribute("nodeId", newNodeId),
                    new XAttribute("Length", totalLength.ToString()),
                    subTrees);
        }
    }
}
