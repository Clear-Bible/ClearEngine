using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Corpora
{
    /// <summary>
    /// An Engine USX text override that returns segments, each of which is a verse id and text, from its GetSegments() override which aren't
    /// grouped my Machine versification so Engine can apply its own versification mapping.
    /// </summary>
    public class EngineUsxFileText : UsxFileText
    {
        public EngineUsxFileText(ITokenizer<string, int, string> wordTokenizer, string fileName, ScrVers? versification = null) 
            : base(wordTokenizer, fileName, versification)
        {
        }

        /// <summary>
        /// An Engine override which doesn't group segments based on Machine's versification.
        /// </summary>
        /// <param name="includeText"></param>
        /// <param name="basedOn"></param>
        /// <returns>Segments, which are verse and text, as the are in the USX document.</returns>
        public override IEnumerable<TextSegment> GetSegments(bool includeText = true, IText basedOn = null)
        {

            /*
            var segments = GetSegmentsInDocOrder(includeText: true)
                .ToList();
            segments.Sort((x, y) => ((VerseRef)x.SegmentRef).CompareTo((VerseRef)y.SegmentRef));
            return segments;
            */

            //Do not sort since sequential TextSegments define ranges.

            // SEE NOTE IN EngineUsfmFileText.GetSegments() as to why this override is necessary and its limitations.
            return GetSegmentsInDocOrder(includeText: includeText);
        }
    }
}
