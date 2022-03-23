using SIL.Machine.Corpora;
using SIL.Scripture;

//using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Corpora
{
    public class EngineManuscriptFileText : ManuscriptFileText, IEngineText
    {
        private readonly IEngineCorpus _engineCorpus;
        public bool _leaveTextRaw = false;

        /// <summary>
        /// Creates the Text for a manuscript book.
        /// </summary>
        /// <param name="manuscriptText"></param>
        /// <param name="book"></param>
        /// <param name="versification">Defaults to Original</param>
        public EngineManuscriptFileText(
            IManuscriptText manuscriptText,
            string book,
            ScrVers versification,
            IEngineCorpus engineCorpus)
            : base(manuscriptText, book, versification)
        {
            _engineCorpus = engineCorpus;
        }


        /// <summary>
        /// An Engine override which uses GetSegmentsInDocOrder if GetSegmentsRetrunsDocSegments is set to true
        /// to bypass Machine's versification.
        /// </summary>
        /// <param name="includeText"></param>
        /// <param name="basedOn">A machine versification setting set by ParallelTextCorpus and its derivatives.</param>
        /// <returns>Segments, verse and optionally text, in the book identified by property Id, e.g. '1JN'.
        /// Verses are document verses adjusted by SIL's versification if GetSetmentsReturnsDocSegments is true, 
        /// otherwise verses are as they are in the document.</returns>
        public override IEnumerable<TextSegment> GetSegments(bool includeText = true, IText? basedOn = null)
        {
            // SEE NOTE IN EngineUsfmFileText.GetSegments() as to why this override is necessary and its limitations.

            //apply machine versification if configured.
            try
            {
                IEnumerable<TextSegment> segments;
                if (!_engineCorpus.DoMachineVersification)
                {
                    segments = GetSegmentsInDocOrder(includeText: includeText);
                }
                else
                {
                    //textSegments = base.GetSegments(includeText, basedOn);
                    segments = _getSegments(includeText, basedOn);
                }

                segments = segments
                    .Select(ts => _engineCorpus.TextSegmentProcessor?.Process(((TokensTextSegment) ts)) ?? (TokensTextSegment) ts);

                if (_leaveTextRaw)
                {
                    segments.ToList(); //make sure to evaluate enumeration before setting ToggleTextRaw from true to false;
                }
                return segments;
            }
            finally
            {
                _leaveTextRaw = false;
            }
        }

        /*
        protected override IEnumerable<TextSegment> GetSegmentsInDocOrder(bool includeText = true)
        {
            return _manuscriptCorpus.GetManuscriptTokens(Id)
                .Where(token => token.Use)
                .SelectMany(token => CreateTextSegments(
                    includeText,
                    token.TokenId.ChapterNum,
                    token.TokenId.VerseNum,
                    token.Text));

        */

        protected override TextSegment CreateTextSegment(bool includeText, string text, object segRef,
            bool isSentenceStart = true, bool isInRange = false, bool isRangeStart = false)
        {
            text = text.Trim();
            if (!includeText)
            {
                return new TextSegment(Id, segRef, Array.Empty<string>(), isSentenceStart, isInRange, isRangeStart,
                    isEmpty: text.Length == 0);
            }

            IReadOnlyList<string> segment;
            if (_leaveTextRaw)
            {
                segment = new List<string>() { text };
            }
            else
            {
                segment = WordTokenizer.Tokenize(text).ToArray();
            }

            segment = TokenProcessors.UnescapeSpaces.Process(segment);

            (string bookAbbreviation, int chapterNum, int verseNum) = TokensTextSegment.GetBookChapterVerse(segRef);
            IReadOnlyList<ManuscriptToken> tokens = _manuscriptText.GetManuscriptTokensForSegment(bookAbbreviation, chapterNum, verseNum).ToList();

            return new TokensTextSegment(
                Id,
                segRef,
                segment,
                isSentenceStart,
                isInRange,
                isRangeStart,
                segment.Count == 0,
                tokens);
        }

        /// <summary>
        /// Needed as a replacement for SIL.Machine.Corpora.ScriptureText::GetSegments (base) because its implementation generates TextSegments internally
        /// and we need MachineTokenTextSegments.
        /// </summary>
        /// <param name="includeText"></param>
        /// <param name="basedOn"></param>
        /// <returns></returns>
        private IEnumerable<TextSegment> _getSegments(bool includeText = true, IText? basedOn = null)
        {
            ScrVers? basedOnVers = null;
            if (basedOn is ScriptureText scriptureText && Versification != scriptureText.Versification)
                basedOnVers = scriptureText.Versification;
            var segList = new List<(VerseRef Ref, TextSegment Segment)>();
            bool outOfOrder = false;
            var prevVerseRef = new VerseRef();
            int rangeStartOffset = -1;
            foreach (TextSegment s in GetSegmentsInDocOrder(includeText))
            {
                TextSegment seg = s;
                var verseRef = (VerseRef)seg.SegmentRef;
                if (basedOnVers != null)
                {
                    verseRef.ChangeVersification(basedOnVers);
                    // convert on-to-many versification mapping to a verse range
                    if (verseRef.Equals(prevVerseRef))
                    {
                        var (rangeStartVerseRef, rangeStartSeg) = segList[segList.Count + rangeStartOffset];
                        bool isRangeStart = false;
                        if (rangeStartOffset == -1)
                            isRangeStart = rangeStartSeg.IsInRange ? rangeStartSeg.IsRangeStart : true;
                        segList[segList.Count + rangeStartOffset] = (rangeStartVerseRef,
                            TextSegmentFactory(rangeStartSeg, seg, isRangeStart));
                            //new TextSegment(rangeStartSeg.TextId, rangeStartSeg.SegmentRef,
                            //    rangeStartSeg.Segment.Concat(seg.Segment).ToArray(), rangeStartSeg.IsSentenceStart,
                            //    isInRange: true, isRangeStart: isRangeStart,
                            //    isEmpty: rangeStartSeg.IsEmpty && seg.IsEmpty));

                        seg = CreateEmptyTextSegment(seg.SegmentRef, isInRange: true);
                        rangeStartOffset--;
                    }
                    else
                    {
                        rangeStartOffset = -1;
                    }
                }
                segList.Add((verseRef, seg));
                if (!outOfOrder && verseRef.CompareTo(prevVerseRef) < 0)
                    outOfOrder = true;
                prevVerseRef = verseRef;
            }

            if (outOfOrder)
                segList.Sort((x, y) => x.Ref.CompareTo(y.Ref));

            return segList.Select(t => t.Segment);
        }

        protected virtual TextSegment TextSegmentFactory(TextSegment textSegmentStart, TextSegment textSegment, bool isRangeStart)
        {
            if (textSegmentStart is not TokensTextSegment || textSegment is not TokensTextSegment)
            {
                throw new ApplicationException("BUG: EngineManuscriptFileText._textSegmentFactory received a TextSegment that isn't a TokensTextSegment");
            }
            return new TokensTextSegment(
                textSegmentStart.TextId,
                textSegmentStart.SegmentRef,
                textSegmentStart.Segment.Concat(textSegment.Segment).ToArray(),
                textSegmentStart.IsSentenceStart,
                isInRange: true,
                isRangeStart: isRangeStart,
                isEmpty: textSegmentStart.IsEmpty && textSegment.IsEmpty,
                ((TokensTextSegment)textSegmentStart).Tokens.Concat(((TokensTextSegment)textSegment).Tokens).ToList().AsReadOnly());
        }
        public bool LeaveTextRaw
        {
            get
            {
                return _leaveTextRaw;
            }
        }

        /// <summary>
        /// Should never be called while iterating GetSegments()
        /// </summary>
        public bool ToggleLeaveTextRawOn()
        {
            bool prev = _leaveTextRaw;
            _leaveTextRaw = true;
            return prev;
        }
    }
}