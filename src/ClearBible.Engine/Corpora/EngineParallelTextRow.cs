﻿
using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Scripture;
using System;

namespace ClearBible.Engine.Corpora
{
    public class EngineParallelTextRow : ParallelTextRow, IEquatable<EngineParallelTextRow>
    {
        private static IReadOnlyList<object> GetSourceSegmentRefs(IEnumerable<TextRow> sourceTextRows, IEnumerable<TextRow> targetTextRows, IEnumerable<AlignmentRow> alignmentRows)
        {
            var sourceSegment = sourceTextRows
                .SelectMany(textRow => textRow.Segment).ToList();
            var targetSegment = targetTextRows
                .SelectMany(textRow => textRow.Segment).ToList();

            var sourceRefs = sourceTextRows
                .Select(textRow => textRow.Ref).ToList();
            var targetRefs = targetTextRows
                .Select(textRow => textRow.Ref).ToList();

            //FIXME: is this right?
            IReadOnlyCollection<AlignedWordPair>? alignedWordPairs = null;
            if ((sourceRefs.Count() == 1) && (alignmentRows.Count() > 0))
            {
                alignedWordPairs = alignmentRows
                    .Where(alignmentRow => alignmentRow.Ref.Equals(sourceRefs.First()))
                    .Select(alignmentRow => alignmentRow.AlignedWordPairs)
                    .FirstOrDefault();
            }

            //since C# doesn't support tuple splatting yet.
            //return (sourceSegmentRefs.AsReadOnly(), targetSegmentRefs.AsReadOnly(), sourceTextSegments.AsReadOnly(), targetTextSegments.AsReadOnly(), alignedWordPairs);

            _TargetSegmentRefs = targetRefs.AsReadOnly();
            _SourceTextSegment = sourceSegment.AsReadOnly();
            _TargetTextSegment = targetSegment.AsReadOnly();
            _AlignedWordPairs = alignedWordPairs;

            return sourceRefs.AsReadOnly();
        }
        private static IReadOnlyList<object> _TargetSegmentRefs = new List<object>();
        private static IReadOnlyList<string> _SourceTextSegment = new List<string>();
        private static IReadOnlyList<string> _TargetTextSegment = new List<string>();
        private static IReadOnlyCollection<AlignedWordPair>? _AlignedWordPairs = new List<AlignedWordPair>();

        public EngineParallelTextRow(
            //string textId,
            IEnumerable<TextRow> sourceTextRows,
            IEnumerable<CompositeToken> sourceVersesCompositeTokens,
            IEnumerable<TextRow> targetTextRows,
            IEnumerable<CompositeToken> targetVersesCompositeTokens,
            IAlignmentCorpus alignmentRows
            )
            : base(
                  GetSourceSegmentRefs(sourceTextRows, targetTextRows, alignmentRows),
                  _TargetSegmentRefs)
        {
            SourceSegment = _SourceTextSegment;
            TargetSegment = _TargetTextSegment;
            AlignedWordPairs = _AlignedWordPairs;

            try
            {
                //validate
                if (sourceVersesCompositeTokens
                    .Where(ct => ct.Tokens.Count() == 0 || ct.Tokens.Any(t => !SourceRefs.Cast<VerseRef>().Contains(new VerseRef(t.TokenId.BookNumber, t.TokenId.ChapterNumber, t.TokenId.VerseNumber))))
                    .Count() > 0)
                    throw new InvalidParameterEngineException(name: "sourceVersesCompositeTokens", value: "", message: "One or more have no Tokens, or Tokens that are not in SourceRefs, or both");


                //Only set SourceTokens if all the members of sourceSegments can be cast to a TokensTextSegment
                var tokens = sourceTextRows
                    .Cast<TokensTextRow>() //throws an invalidCastException if any of the members can't be cast to type
                    .SelectMany(tokensTextRow => tokensTextRow.Tokens
                        .Where(token => ! sourceVersesCompositeTokens
                            .SelectMany(ct => ct.Tokens)
                            .Select(t => t.TokenId)
                            .Contains(token.TokenId)))
                    .ToList();
                tokens.AddRange(sourceVersesCompositeTokens
                    .Where(ct => ct.Tokens.Any()));

                SourceTokens = tokens;
            }
            catch (InvalidCastException)
            {
            }

            try
            {
                //validate
                if (targetVersesCompositeTokens
                    .Where(ct => ct.Tokens.Count() == 0 || ct.Tokens.Any(t => !TargetRefs.Cast<VerseRef>().Contains(new VerseRef(t.TokenId.BookNumber, t.TokenId.ChapterNumber, t.TokenId.VerseNumber))))
                    .Count() > 0)
                    throw new InvalidParameterEngineException(name: "targetVersesCompositeTokens", value: "", message: "One or more have no Tokens, or Tokens that are not in TargetRefs, or both");

                //Only set TargetTokens if all the members of targetSegments can be cast to a TokensTextSegment
                var tokens = targetTextRows
                    .Cast<TokensTextRow>() //throws an invalidCastException if any of the members can't be cast to type
                    .SelectMany(tokensTextRow => tokensTextRow.Tokens
                        .Where(token => !targetVersesCompositeTokens
                            .SelectMany(ct => ct.Tokens)
                            .Select(t => t.TokenId)
                            .Contains(token.TokenId)))
                    .ToList();
                tokens.AddRange(targetVersesCompositeTokens
                    .Where(ct => ct.Tokens.Any()));

                TargetTokens = tokens;
            }
            catch (InvalidCastException)
            {
            }
        }

        public EngineParallelTextRow(ParallelTextRow parallelTextRow, 
            IReadOnlyList<Token> sourceTokens,
            IReadOnlyList<Token> targetTokens)
            : base(parallelTextRow.SourceRefs, parallelTextRow.TargetRefs)
        {
            SourceSegment = parallelTextRow.SourceSegment;
            TargetSegment = parallelTextRow.TargetSegment;
            AlignedWordPairs = parallelTextRow.AlignedWordPairs;
            IsSourceSentenceStart = parallelTextRow.IsSourceSentenceStart;
            IsSourceInRange = parallelTextRow.IsSourceInRange;
            IsSourceRangeStart = parallelTextRow.IsSourceRangeStart;
            IsTargetSentenceStart = parallelTextRow.IsTargetSentenceStart;
            IsTargetInRange = parallelTextRow.IsTargetInRange;
            IsTargetRangeStart = parallelTextRow.IsTargetRangeStart;
            IsEmpty = parallelTextRow.IsEmpty;

            SourceTokens = sourceTokens;
            TargetTokens = targetTokens;
        }

        public IReadOnlyList<Token>? SourceTokens { get; }

        public IReadOnlyList<Token>? TargetTokens { get; }

        public override bool Equals(object? obj)
        {
            bool equals = obj is EngineParallelTextRow engineParallelTextRow
                && engineParallelTextRow.SourceRefs.Count() > 0
                && engineParallelTextRow.SourceRefs.Count() == SourceRefs.Count();

            if (!equals)
                return false;

            foreach (var sourceVerseRef in SourceRefs.Cast<VerseRef>())
            {
                if (!((EngineParallelTextRow) obj!).SourceRefs
                        .Cast<VerseRef>()
                        .Contains(sourceVerseRef))
                {
                    equals = false;
                    break;
                }
            }
            return equals;
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            foreach (var sourceVerseRef in SourceRefs.Cast<VerseRef>())
                hashCode += sourceVerseRef.GetHashCode();
            return hashCode;
        }

        public bool Equals(EngineParallelTextRow? other)
        {
            return Equals((object?)other);
        }
    }
}
