using ClearBible.Engine.Exceptions;
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
        /// <exception cref="InvalidTreeEngineException"></exception>
        public IEnumerable<(string chapter, string verse, IEnumerable<ManuscriptToken> manuscriptTokens, bool isSentenceStart)> GetTokensTextRowInfos(string bookAbbreviation)
        {
            return GetBookChapters(bookAbbreviation)
                .SelectMany(c => _getVerseXElementsForChapter(bookAbbreviation, c)
                    .Select(verse =>
                        (
                            chapter: verse
                                .Descendants()
                                .Where(node => node.FirstNode is XText)
                                .First()
                                ?.Chapter()
                                ?? throw new InvalidTreeEngineException($"Doesn't have a first textNode", new Dictionary<string, string> 
                                {
                                    {"bookAbbreviation", bookAbbreviation },
                                    {"chapter", c.ToString()}
                                }),
                            verse: verse
                                .Descendants()
                                .Where(node => node.FirstNode is XText)
                                .First()
                                ?.Verse()
                                ?? throw new InvalidTreeEngineException($"Doesn't have a first textNode", new Dictionary<string, string>
                                {
                                    {"bookAbbreviation", bookAbbreviation },
                                    {"chapter", c.ToString()}
                                }),
                            /*FIXME: remove? text: string.Join(
                                    " ",
                                    verse
                                        .Descendants()
                                        .Where(node => node.FirstNode is XText)
                                        .Select(textNode => textNode.Lemma().Replace(' ', '~'))),*/
                            tokens: verse
                                .Descendants()
                                .Where(node => node.FirstNode is XText)
                                .Select((leaf, index) => new ManuscriptToken(
                                    leaf.TokenId(),
                                    leaf.Surface(),
                                    leaf.Strong(),
                                    leaf.Category(),
                                    leaf.Lemma()
                                    )),
                            isSentenceStart: true
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
                     ?.Verse().Equals(verseNumber.ToString("000")) ?? false)
                .SelectMany(chapterElement => chapterElement
                    .Descendants("Node")
                    .Where(node => node.FirstNode is XText)
                    .Select(textNode => new ManuscriptToken(textNode.TokenId(), textNode.Surface(), textNode.Strong(), textNode.Category(), /*leaf.Analysis(),*/ textNode.Lemma())));
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
        /// <exception cref="InvalidTreeEngineException"></exception>
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
                    throw new InvalidTreeEngineException($"Not found", new Dictionary<string, string>
                        {
                            {"book", book },
                            {"chapterNumber", chapterNumber.ToString()},
                            {"verseNumber", verseNumber.ToString()}
                        });
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
        /// <exception cref="InvalidTreeEngineException"></exception>
        /// <exception cref="InvalidBookMappingEngineException"></exception>
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
                    throw new InvalidBookMappingEngineException(message: "Doesn't exist", name: "silCannonBookAbbrev", value: bookAbbreviation);
                }

                _bookChapters.Add(bookAbbreviation, Directory.EnumerateFiles(_manuscriptTreesPath, $"{clearBookAbbreviation}???.trees.xml")
                    .Select(fileName =>
                    {
                        int chapterNumber;
                        var success = int.TryParse(fileName.Substring(fileName.Length - 13, 3), out chapterNumber);
                        if (!success)
                        {
                            throw new InvalidTreeEngineException($"Filename couldn't be parsed for chapter number", new Dictionary<string, string>
                                {
                                    {"bookAbbreviation", bookAbbreviation },
                                    {"fileName", fileName}
                                });
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
        /// <exception cref="InvalidTreeEngineException"></exception>
        /// <exception cref="InvalidBookMappingEngineException"></exception>
        private IEnumerable<XElement> _getVerseXElementsForChapter(string bookAbbreviation, int chapterNumber)
        {
            if (!_loadedChapters.ContainsKey((bookAbbreviation, chapterNumber)))
            {

                var codes = BookIds
                    .Where(BookId => BookId.silCannonBookAbbrev.Equals(bookAbbreviation))
                    .Select(bookId => bookId.clearTreeBookAbbrev);

                if (codes.Count() != 1)
                {
                    throw new InvalidBookMappingEngineException(message: "Doesn't exist", name: "silCannonBookAbbrev", value: bookAbbreviation);
                }

                var path = Path.GetFullPath(_manuscriptTreesPath);
                var fileName = Path.Combine(path, $"{codes.First()}{chapterNumber.ToString("000")}.trees.xml");

                if (!File.Exists(fileName))
                {
                    throw new InvalidTreeEngineException($"File doesn't Exist.", new Dictionary<string, string>
                        {
                            {"fileName", fileName },
                            {"bookAbbreviation", bookAbbreviation },
                            {"chapterNumber", chapterNumber.ToString() }
                        });
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
                (subTrees[0].Attribute("nodeId")?.Value.Substring(0, 11) ?? throw new InvalidTreeEngineException($"Node doesn't have attribute.", new Dictionary<string, string>
                        {
                            {"attribute", "nodeId" }
                        })) +
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

}

