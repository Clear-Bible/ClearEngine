using ClearBible.Engine.Tokenization;
using System.Xml.Linq;

using static ClearBible.Engine.Corpora.IManuscriptText;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Corpora
{
    public class ManuscriptFileTree : IManuscriptText, IManuscriptTree
    {
        private readonly string _manuscriptTreesPath;

        public ManuscriptFileTree(string manuscriptTreesPath)
        {
            _manuscriptTreesPath = manuscriptTreesPath;
        }

        #region IManuscriptText

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns the books that exist in the syntax trees in three letter SIL abbreviation form.</returns>
        public IEnumerable<string> GetBooks()
        {

            var books = Directory.EnumerateFiles(_manuscriptTreesPath, "*.xml")
                .Select(fileName => BookIds
                    .Where(bookId => bookId.clearTreeBookAbbrev.Equals(fileName
                        .Trim().Substring(_manuscriptTreesPath.Length + 1, fileName.Length - _manuscriptTreesPath.Length - 1 - 13)))
                    .FirstOrDefault()?.silCannonBookAbbrev ?? "")
                //names are  in b[bb]ccc.trees.xml format, and we want the b[bb] part, therefore subtracting
                // 13 characters and 1 more for the directory separator.
                .Where(silBookAbbrev => !silBookAbbrev.Trim().Equals(""))
                .Distinct();

            return books;
        }

        /// <summary>
        /// Gets all the segments, e.g. verses, for this book.
        /// </summary>
        /// <param name="bookAbbreviation"></param>
        /// <param name="includeText"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public IEnumerable<BookSegment> GetBookSegments(
            string bookAbbreviation, 
            bool includeText = true)
        {
            return GetBookChapters(bookAbbreviation)
                .SelectMany(c => _getVerseXElementsForChapter(bookAbbreviation, c)
                    .Select(verse => new BookSegment
                        (
                            verse
                                .Descendants()
                                .Where(node => node.FirstNode is XText)
                                .First()
                                ?.Attribute("morphId")?.Value.Substring(2, 3)
                                ?? throw new InvalidDataException($@"Syntax tree for book {bookAbbreviation} chapter {c} has a verse whose first leaf node 
                                                                        doesn't have a nodeId attribute. Cannot determine chapter number"),
                            verse
                                .Descendants()
                                .Where(node => node.FirstNode is XText)
                                .First()
                                ?.Attribute("morphId")?.Value.Substring(5, 3)
                                ?? throw new InvalidDataException($@"Syntax tree {bookAbbreviation} chapter {c} has a verse whose first leaf node doesn't 
                                                                            have a nodeId attribute. Cannot determine verse number"),
                            includeText ?
                                string.Join(
                                    " ",
                                    verse
                                        .Descendants()
                                        .Where(node => node.FirstNode is XText)
                                        .Select(leaf => leaf?
                                            .Attribute("UnicodeLemma")?.Value.Replace(' ', '~') ?? ""))
                                : ""
                        ))
                    );
            /*
            return Directory.EnumerateFiles(_manuscriptTreesPath, $"{bookAbbreviation}*.xml")
                .SelectMany(fileName =>
                    XElement
                        .Load(fileName)
                        .Descendants("Sentence")
                        .Select(verse => new BookSegment
                            (
                                verse
                                    .Descendants()
                                    .Where(node => node.FirstNode is XText)
                                    .First()
                                    ?.Attribute("morphId")?.Value.Substring(2, 3)
                                    ?? throw new InvalidDataException($@"Syntax tree {fileName} has a verse whose first leaf node 
                                                                            doesn't have a nodeId attribute. Cannot determine chapter number"),
                                verse
                                    .Descendants()
                                    .Where(node => node.FirstNode is XText)
                                    .First()
                                    ?.Attribute("morphId")?.Value.Substring(5, 3)
                                    ?? throw new InvalidDataException($@"Syntax tree {fileName} has a verse whose first leaf node doesn't 
                                                                             have a nodeId attribute. Cannot determine verse number"),
                                includeText ? 
                                    string.Join(
                                        " ",
                                        verse
                                            .Descendants()
                                            .Where(node => node.FirstNode is XText)
                                            .Select(leaf => leaf?
                                                .Attribute("UnicodeLemma")?.Value.Replace(' ', '~') ?? "")) 
                                    : ""
                            )
                        )
                );
            */
        }

        public IEnumerable<ManuscriptToken> GetManuscriptTokensForSegment(string bookAbbreviation, int chapterNumber, int verseNumber)
        {
            IEnumerable<XElement> chapterXElements = _getVerseXElementsForChapter(bookAbbreviation, chapterNumber);
            return chapterXElements
                .Where(verse => verse
                    .Descendants("Node")
                    .Where(node => node.FirstNode is XText)
                    .First()
                     ?.Attribute("morphId")?.Value.Substring(5, 3).Equals(verseNumber.ToString("000")) ?? false)
                .SelectMany(chapterElement => chapterElement
                    .Descendants("Node")
                    .Where(node => node.FirstNode is XText)
                    .Select(leaf => new ManuscriptToken(leaf.TokenId(), leaf.Surface(), leaf.Strong(), leaf.Category(), /*leaf.Analysis(),*/ leaf.Lemma())));
        }

        #endregion

        #region IManuscriptTree
        /// <summary>
        /// 
        /// </summary>
        /// <param name="book">Three character book abbreviation (SIL)</param>
        /// <param name="chapterNumber"></param>
        /// <param name="verseNumbers"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public XElement? GetVersesXElementsCombined(string book, int chapterNumber, IEnumerable<int> verseNumbers)
        {
            List<XElement> verseXElements = new List<XElement>();

            foreach (int verseNumber in verseNumbers)
            {
                var verseXElement = _getVerseXElementsForChapter(book, chapterNumber)
                    .Where(x =>
                    {
                        bool success = int.TryParse(x.Attribute("nodeId")?.Value.Substring(5, 3), out int attrVerseNumber);
                        if (success)
                        {
                            return verseNumber == attrVerseNumber;
                        }
                        else
                        {
                            return false;
                        }
                    })
                    .FirstOrDefault();

                if (verseXElement == null)
                {
                    throw new InvalidDataException($"Manuscript book {book} chapterNumber {chapterNumber} verse {verseNumber} not found in syntax trees");
                }
                else
                {
                    verseXElements.Add(verseXElement);
                }
            }
            if (verseXElements.Count > 0)
            {
                return CombineVerseXElementsIntoOne(verseXElements);
            }
            else if (verseXElements.Count == 1)
            {
                return verseXElements[0];
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region bookChapters cache from filenames and access method

        /// <summary>
        /// A cache of chapterNumbers by SIL bookAbreviations.
        /// </summary>
        private Dictionary<string,List<int>> _bookChapters = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bookAbbreviation">SIL book abbreviation</param>
        /// <returns></returns>
        protected IEnumerable<int> GetBookChapters(string bookAbbreviation)
        {
            if (!_bookChapters.ContainsKey(bookAbbreviation))
            {
                string? clearBookAbbreviation = BookIds
                    .Where(bookId => bookId.silCannonBookAbbrev.Equals(bookAbbreviation))
                    .Select(bookId => bookId.clearTreeBookAbbrev)
                    .FirstOrDefault();

                if (clearBookAbbreviation == null)
                {
                    throw new InvalidDataException($"_getBookXElement({bookAbbreviation}) has no mapped clear tree book abbreviation.");
                }

                _bookChapters.Add(bookAbbreviation, Directory.EnumerateFiles(_manuscriptTreesPath, $"{clearBookAbbreviation}???.trees.xml")
                    .Select(fileName =>
                    {
                        int chapterNumber;
                        var success = int.TryParse(fileName.Substring(fileName.Length - 13, 3), out chapterNumber);
                        if (!success)
                        {
                            throw new InvalidDataException($"_getBookXElement({bookAbbreviation}) found tree file whose filename couldn't be parsed for a chapter number {fileName}.");
                        }
                        return chapterNumber;
                    }).ToList());
            }
            return _bookChapters[bookAbbreviation];
        }

        #endregion

        #region chapter XElement cache from files and access method.

        /// <summary>
        /// A cache of loaded verses by chapter.
        /// 
        /// key is tuple if (bookAbbreviation (SIL), chapterNumber) and returns an enumerble of verse XElements.
        /// </summary>
        private Dictionary<(string, int), IEnumerable<XElement>> _loadedChapters = new Dictionary<(string, int), IEnumerable<XElement>>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bookAbbreviation">SIL book abbreviation</param>
        /// <param name="chapterNumber"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="FileLoadException"></exception>
        private IEnumerable<XElement> _getVerseXElementsForChapter(string bookAbbreviation, int chapterNumber)
        {
            if (!_loadedChapters.ContainsKey((bookAbbreviation, chapterNumber)))
            {

                var codes = BookIds
                    .Where(BookId => BookId.silCannonBookAbbrev.Equals(bookAbbreviation))
                    .Select(bookId => bookId.clearTreeBookAbbrev);

                if (codes.Count() != 1)
                {
                    throw new InvalidDataException("Book not found in ManuscriptFileBookToSILBookPrefixes mapping");
                }

                var fileName = $"{_manuscriptTreesPath}{Path.DirectorySeparatorChar}{codes.First()}{chapterNumber.ToString("000")}.trees.xml";

                if (!File.Exists(fileName))
                {
                    throw new FileLoadException($"{fileName} doesn't exist.");
                }
                IEnumerable<XElement> verseXElements = XElement.Load(fileName)
                    .Descendants("Sentence")
                    .Select(s => s.Descendants("Node").First());
                //FIXME: _loadedChapters[(bookAbbreviation, chapterNumber)] = verseXElements;
                return verseXElements;
            }

            return _loadedChapters[(bookAbbreviation, chapterNumber)];


        }
        protected XElement CombineVerseXElementsIntoOne(List<XElement> verseXElements)
        {

            List<XElement> subTrees =
                verseXElements.SelectMany(t => t.Elements()).ToList();

            int totalLength = subTrees
                .Select(x => Int32.Parse(x.Attribute("Length")?.Value ?? "0"))
                .Sum();

            string newNodeId =
                (subTrees[0].Attribute("nodeId")?.Value.Substring(0, 11) ?? throw new InvalidDataException("Node doesn't have nodeId attribute")) +
                $"{totalLength:D3}" +
                "0";

            return
                new XElement("Node",
                    new XAttribute("Cat", "S"),
                    new XAttribute("Head", "0"),
                    new XAttribute("nodeId", newNodeId),
                    new XAttribute("Length", totalLength.ToString()),
                    subTrees);
        }
    }

    #endregion

    #region Extensions
    public static class ManuscriptTreeExtensions
    {
        public static TokenId TokenId(this XElement leaf)
        {
            string? morphId = leaf.Attribute("morphId")?.Value;
            if (morphId == null)
            {
                throw new InvalidDataException("Unable to extract tokenId from leaf element because element does not have a 'morphId' attribute");
            }
            if (morphId.Length != 11)
            {
                throw new InvalidDataException("Unable to extract tokenId from leaf element because element's 'morphId' attribute is not length 11");
            }
            morphId += "1";
            string? silBookNum = BookIds
                .Where(b => b.clearTreeBookNum.Equals(morphId.Substring(0, 2)))
                .Select(b => b.silCannonBookNum)
                .FirstOrDefault();

            if (silBookNum == null)
            {
                throw new InvalidDataException($"Unable to find sil book number for leaf morphId {morphId} with clear tree book number in first two positions");
            }
            int bookSilNumber = int.Parse(silBookNum);
            int chapterNumber = int.Parse(morphId.Substring(2, 3));
            int verseNumber = int.Parse(morphId.Substring(5, 3));
            int wordNumber = int.Parse(morphId.Substring(8, 3));
            int subWordNumber = int.Parse(morphId.Substring(11, 1));
            return new TokenId(bookSilNumber, chapterNumber, verseNumber, wordNumber, subWordNumber);
        }
        public static string Lemma(this XElement leaf) =>
            leaf.Attribute("UnicodeLemma")?.Value ?? throw new InvalidDataException("Manuscript leaf element does not contain the 'UnicodeLemma' attribute");

        public static string Surface(this XElement leaf) =>
            leaf.Attribute("Unicode")?.Value ?? throw new InvalidDataException("Manuscript leaf element does not contain the 'Unicode' attribute");

        public static string Strong(this XElement leaf) =>
            (leaf.Attribute("Language")?.Value ?? throw new InvalidDataException("Manuscript leaf element does not contain the 'Language' attribute")) +
            (leaf.Attribute("StrongNumberX")?.Value ?? throw new InvalidDataException("Manuscript leaf element does not contain the 'StrongNumberX' attribute"));

        public static string English(this XElement leaf) =>
            leaf.Attribute("English")?.Value ?? throw new InvalidDataException("Manuscript leaf element does not contain the 'English' attribute");

        public static string Category(this XElement leaf) =>
            leaf.Attribute("Cat")?.Value ?? throw new InvalidDataException("Manuscript leaf element does not contain the 'Cat' attribute");

        public static int Start(this XElement leaf)
        {
            var attr = leaf.Attribute("Start");
            if (attr == null)
            {
                throw new InvalidDataException("Manuscript leaf element does not contain the 'Start' attribute");
            }

            bool success = int.TryParse(leaf.Attribute("Start")?.Value, out int startNum);
            if (success)
            {
                return startNum;
            }
            else
            {
                throw new InvalidDataException("Manuscript leaf element 'Start' attribute not int parseable.");
            }
        }
        public static string Analysis(this XElement leaf) =>
            leaf.Attribute("Analysis")?.Value ?? throw new InvalidDataException("Manuscript leaf element does not contain the 'Analysis' attribute");
    }
    #endregion
}

