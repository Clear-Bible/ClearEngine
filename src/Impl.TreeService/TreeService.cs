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
        public void GetChapterTree(ChapterID chapterID)
        {
            int bookNumber = chapterID.Book;           
            int chapterNumber = chapterID.Chapter;

            string bookName = _bookNames[$"{bookNumber:D2}"];

            string treeFile = Path.Combine(_treeFolder, $"{bookName}{chapterNumber:D3}.trees.xml");

            if (File.Exists(treeFile))
            {
                XElement xml = XElement.Load(treeFile);

                foreach (XElement node in xml
                    .Descendants("Sentence")
                    .Select(s => s.Descendants("Node").First()))
                {
                    string verseIdString =
                        node.Attribute("nodeId").Value.Substring(0, 8);
                    VerseID verseID = new VerseID(verseIdString);
                    _trees2[verseID] = node;
                }
            }
        }


        private Dictionary<VerseID, XElement> _trees2 =
            new Dictionary<VerseID, XElement>();

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
                _trees2.Clear();

                // Get the trees for the current chapter; a verse can cross chapter boundaries
                GetChapterTree(chapterID);
                int book = chapterID.Book;
                int chapter = chapterID.Chapter;
                ChapterID prevChapterID = new ChapterID(book, chapter - 1);
                GetChapterTree(prevChapterID);
                ChapterID nextChapterID = new ChapterID(book, chapter + 1);
                GetChapterTree(nextChapterID);
                _currentChapterID = chapterID;
            }
        }


        public XmlNode GetTreeNode(VerseID start, VerseID end)
        {
            List<XElement> verseTrees = GetVerseTrees(start, end).ToList();

            if (verseTrees.Count == 1)
            {
                return verseTrees[0].ToXmlNode();
            }
            else
            {
                return CombineTrees(verseTrees).ToXmlNode();
            }
        }


        IEnumerable<XElement> GetVerseTrees(VerseID start, VerseID end)
        {
            int book = start.Book;

            if (start.Chapter == end.Chapter)
            {
                return GetChapterSubrange(book, start.Chapter, start.Verse, end.Verse);
            }
            else
            {
                return
                    GetChapterTail(book, start.Chapter, start.Verse)
                    .Concat(GetChapterSubrange(book, end.Chapter, 1, end.Verse));
            }
        }


        IEnumerable<XElement> GetChapterSubrange(
            int book,
            int chapter,
            int startVerse,
            int endVerse)
        {
            for (int i = startVerse; i <= endVerse; i++)
            {
                if (_trees2.TryGetValue(
                    new VerseID(book, chapter, i),
                    out XElement subTree))
                {
                    yield return subTree;
                }
                else
                {
                    yield break;
                }
            }
        }


        IEnumerable<XElement> GetChapterTail(
            int book,
            int chapter,
            int startVerse)
        {
            for (int i = startVerse; true; i++)
            {
                if (_trees2.TryGetValue(
                    new VerseID(book, chapter, i),
                    out XElement subTree))
                {
                    yield return subTree;
                }
                else
                {
                    yield break;
                }
            }
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
