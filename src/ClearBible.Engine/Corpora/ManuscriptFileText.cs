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
        private readonly string _manusciptTreesPath;
        private readonly string _fileNameBookPrefix;

        private class ManuscriptTokenizer : WhitespaceTokenizer
        {
            protected override bool IsWhitespace(char c)
            {
                return c == ' ';
            }
        }
        private static string ConvertToSILBookAbbreviation(string fileNameBookPrefix)
		{
			var hasMapping = Mappings.ManuscriptFileBookToSILBookPrefixes.TryGetValue(fileNameBookPrefix, out var result);
			if (hasMapping)
			{
				return result != null ? result.code : throw new NullReferenceException($"Mapping.ManuscriptFileBookToSILBookPrefixes[{fileNameBookPrefix}] = null");
			}
			else
			{
				throw new KeyNotFoundException($"Mapping.ManuscriptFileBookToSILBookPrefixes[{fileNameBookPrefix}] doesn't exist.");
			}
		}
		
		public ManuscriptFileText(string manusciptTreesPath, string fileNameBookPrefix, ScrVers versification)
			: base(new ManuscriptTokenizer(), ConvertToSILBookAbbreviation(fileNameBookPrefix), versification ?? ScrVers.Original)
        {
			_manusciptTreesPath = manusciptTreesPath;
			_fileNameBookPrefix = fileNameBookPrefix;
		}



        /// <summary>
        /// Set to true to get the segments in the document rather than Machine's behavior of trying to partially group them as is required by Engine verse mapping.
        /// Defaults to false.
        /// </summary>
        public bool GetSegmentsReturnsDocSegments { get; set; }

        /// <summary>
        /// An Engine override which doesn't group segments based on Machine's versification.
        /// </summary>
        /// <param name="includeText"></param>
        /// <param name="basedOn"></param>
        /// <returns>Segments, which are verse and text, as the are in the USFM document, sorted by verse.</returns>
        public override IEnumerable<TextSegment> GetSegments(bool includeText = true, IText basedOn = null)
        {
            if (GetSegmentsReturnsDocSegments)
            {
                return GetSegmentsInDocOrder(includeText: includeText);
            }
            else
            {
                return base.GetSegments(includeText, basedOn);
            }
        }

        protected override IEnumerable<TextSegment> GetSegmentsInDocOrder(bool includeText = true)
        {
            return Directory.EnumerateFiles(_manusciptTreesPath, $"{_fileNameBookPrefix}*.xml")
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
                                    ?.Attribute("nodeId")?.Value.Substring(2, 3)
                                    ?? throw new InvalidDataException($@"Syntax tree {fileName} has a verse whose first leaf node 
                                                                            doesn't have a nodeId attribute. Cannot determine chapter"),
                                verse
                                    .Descendants()
                                    .Where(node => node.FirstNode is XText)
                                    .First()
                                    ?.Attribute("nodeId")?.Value.Substring(5, 3)
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