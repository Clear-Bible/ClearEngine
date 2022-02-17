using SIL.Machine.Annotations;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;
using System.Xml.Linq;
using static ClearBible.Engine.Corpora.Mappings;

namespace ClearBible.Engine.Corpora
{
    public class ManuscriptFileText : ScriptureText
    {
        private readonly IManuscriptText _manuscriptCorpus;

        private class ManuscriptTokenizer : WhitespaceTokenizer
        {
            protected override bool IsWhitespace(char c)
            {
                return c == ' ';
            }
        }
		
        /// <summary>
        /// Creates the Text for a manuscript book.
        /// </summary>
        /// <param name="manuscriptCorpus"></param>
        /// <param name="book"></param>
        /// <param name="versification">Defaults to Original</param>
		public ManuscriptFileText(IManuscriptText manuscriptCorpus, string book, ScrVers versification)
			: base(new ManuscriptTokenizer(), book, versification ?? ScrVers.Original)
        {
            _manuscriptCorpus = manuscriptCorpus;
        }

        /// <summary>
        /// Returns verse and text as they are in the document(s).
        /// </summary>
        /// <param name="includeText"></param>
        /// <returns></returns>
        protected override IEnumerable<TextSegment> GetSegmentsInDocOrder(bool includeText = true)
        {
            return _manuscriptCorpus.GetBookSegments(Id, includeText)
                .SelectMany(bookSegment => CreateTextSegments(
                        includeText,
                        bookSegment.chapter,
                        bookSegment.verse,
                        bookSegment.text)
                    );
            /*
            return Directory.EnumerateFiles(_manuscriptTreesPath, $"{_fileNameBookPrefix}*.xml")
                .SelectMany(fileName =>
                    XElement
                        .Load(fileName)
                        .Descendants("Sentence")
                        .SelectMany(verse => CreateTextSegments
                            (
                                includeText,
                                verse
                                    .Descendants()
                                    .Where(node => node.FirstNode is XText)
                                    .First()
                                    ?.Attribute("morphId")?.Value.Substring(2, 3)
                                    ?? throw new InvalidDataException($@"Syntax tree {fileName} has a verse whose first leaf node 
                                                                            doesn't have a nodeId attribute. Cannot determine chapter"),
                                verse
                                    .Descendants()
                                    .Where(node => node.FirstNode is XText)
                                    .First()
                                    ?.Attribute("morphId")?.Value.Substring(5, 3)
                                    ?? throw new InvalidDataException($@"Syntax tree {fileName} has a verse whose first leaf node doesn't 
                                                                             have a nodeId attribute. Cannot determine chapter"),
                                string.Join(
                                    " ",
                                    verse
                                        .Descendants()
                                        .Where(node => node.FirstNode is XText)
                                        .Select(leaf => leaf?
                                            .Attribute("UnicodeLemma")?.Value.Replace(' ', '~') ?? ""))
                            )
                        )
                );
            */
         }
                
			/*
			 * note that CreateTextSegment calls this after tokenizing text:
				private class UnescapeSpacesTokenProcessor : ITokenProcessor
				{
					public IReadOnlyList<string> Process(IReadOnlyList<string> tokens)
					{
						return tokens.Select(t => t == "<space>" ? " " : t).ToArray();
					}
				}
			*/
	}
}