﻿using System.Text;

using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    /// <summary>
    /// An Engine USFM text override that returns segments, each of which is a verse id and text, from its GetSegments() override. which aren't
	/// grouped my Machine versification so Engine can apply its own versification mapping.
    /// </summary>
    public class EngineUsfmFileText : UsfmFileText
    {
        private readonly IEngineCorpus _engineCorpus;

        public EngineUsfmFileText(
            ITokenizer<string, int, string> wordTokenizer,
            UsfmStylesheet stylesheet,
            Encoding encoding,
            string fileName,
            ScrVers? versification,
            bool includeMarkers,
            IEngineCorpus engineCorpus)
            : base(wordTokenizer, stylesheet, encoding, fileName, versification, includeMarkers)
        {
            _engineCorpus = engineCorpus;
        }

        /// <summary>
        /// An Engine override which doesn't group segments based on Machine's versification.
        /// </summary>
        /// <param name="includeText"></param>
        /// <param name="basedOn"></param>
        /// <returns>Segments, which are verse and text, as the are in the USFM document.</returns>
        public override IEnumerable<TextSegment> GetSegments(bool includeText = true, IText basedOn = null)
        {
            /*
                * NOTE:
                *  TextSegment
                *    .IsSentenceStart    purpose unknown.
                *    .IsInRange          determines if verse is a part of a range, e.g. 4-7
                *    .IsRangeStart       determines if verse is the first verse in a range.
                *    
                *    For "Fee fi fo" corresponds to 1JN 1:1-4 in source text:
                *    
                *    VerseRef.AllVerses()
                *      (versenum 1, verses 1)   Fee fi fo
                *      (versenum 2, verses -)     -
                *      (versenum 3, verses -)     -
                *      (versenum 4, verses 4)     -    (Notice that VerseRef.AllVerses sets Verse on the last one, but that VerseRef.Verses setter parses this field
                *                                      and puts the number into verseNum)
                *                                      
                *    Similarly, ScriptureText.CreateTextSegments() creates TextSegment with
                *      (versenum 1, verses 1) IsRangeStart: true, IsInRange: true, Segment: Fee fi fo
                *      (versenum 2, verses -) IsRangeStart: false, IsInRange: true, Segment: <empty>
                *      (versenum 3, verses -) IsRangeStart: false, IsInRange: true, Segment: <empty>     -
                *      (versenum 4, verses 4) IsRangeStart: false, IsInRange: true, Segment: <empty>
                *
                */


            /* RM 2/13/22
                * 
                * It appears USFM and USX support file verse ranges, where "Fee fi fo" corresponds to 1JN 1:1-4.
                * 
                * VerseRef has the notion of a versenum and verse, where verse can be a set of ranges speficied by dash, each range separated by commas,
                * e.g. 1-4,6-9.
                * 
                * For a USFM or USX range, it appears SIL.Machine.Corpora.ScriptureText.CreateTextSegments() adds the following (with the help of
                * SIL.Scripture.VerseRef.AllVerses()):
                * 
                *  (versenum 1, verses 1)   Fee fi fo
                *  (versenum 2, verses -)     -
                *  (versenum 3, verses -)     -
                *  (versenum 4, verses 4)     -    (Notice that VerseRef.AllVerses sets Verse on the last one, but that VerseRef.Verses setter parses this field
                *                                      and puts the number into verseNum)
                *  
                *  Separately, this.GetSegments() tries to take care of many to one groupings in versifications. For example, if USFM has
                *  1 bee
                *  2 bi
                *  3 bo
                *  
                *  and there is something like the following (?) in a versification file
                *  
                * 1-3 = 1
                * 
                * it will change the above into
                * 1 be bi bo   (startofrange t)
                * 2 -          (is in range t)
                * 3 -          (is in range t)
                * 
                * Then, ParallelText possibly uses this into to create its parallel segments.
                * 
                * Engine therefore takes the approach of building its map using ScriptureText.GetSegments(), letting it group things in case ParallelText
                * needs these groupings to formulate ParallelTextSegments correctly. 
                * 
                * However, once Engine's map is built and available, Engine uses EngineXXXText, e.g. EngineUsfmFileText
                * overrides of ScriptureText.GetSegments() to get the segments as the document produces them so that 
                * many to one verses that Machine concatenated together are left apart, e.g.
                *  1 bee
                *  2 bi
                *  3 bo                 * 
                * 
                * so that engine maps can put them together in other ways.
                * 
                * The problem is that file verse ranges probably look the same and obviously cannot be 'left apart' because they weren't translated apart, 
                * e.g. the words Fee, fi, etc. in the segment "Fee fi fo", which corresponds to 1JN 1:1-4 in the file, cannot be broken down into 
                * individual verses because there is not enough information in the translation file(s) to do that.
                *
                * FIXME:
                * Therefore, either Engine mapping editors will need to make sure and not break apart file verse ranges, or Engine
                * will need to provide a check to ensure an Engine mapping didn't break apart file verse ranges.
                * 
                */

            //Do not sort since sequential TextSegments define ranges.

            //apply machine versification if configured.
            IEnumerable<TextSegment> textSegments;
            if (!_engineCorpus.DoMachineVersification)
            {
                textSegments = GetSegmentsInDocOrder(includeText: includeText);
            }
            else
            {
                textSegments = base.GetSegments(includeText, basedOn);
            }

            // return if no processors configured.
            if (!includeText || _engineCorpus.TextSegmentProcessor == null)
            {
                //used yield here so the following foreach with yield, which creates an iterator, is possible
                foreach (var textSegment in textSegments
                        .Select(textSegment => new TokenIdsTextSegment(textSegment)))
                {
                    yield return textSegment;
                }
            }
            else
            {
                // otherwise process the TextSegments.
                foreach (var textSegment in textSegments)
                {
                    yield return _engineCorpus.TextSegmentProcessor.Process(new TokenIdsTextSegment(textSegment));
                }
            }
        }
    }
}
