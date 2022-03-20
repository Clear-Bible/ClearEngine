using System.Xml.Linq;

using ClearBible.Engine.Corpora;

using SIL.Machine.Corpora;
using SIL.Scripture;

using ClearBible.Engine.TreeAligner.Legacy;

namespace ClearBible.Engine.TreeAligner.Adapter
{

    internal class ZoneAlignmentAdapter
    {

        internal static IEnumerable<(SourcePoint, (TargetPoint, double))> AlignZone(ParallelTextSegment parallelTextSegment, IManuscriptTree manuscriptTree, IAutoAlignAssumptions config)
        {
            try
            {
                parallelTextSegment.SourceSegmentRefs.Cast<VerseRef>();
            }
            catch (InvalidCastException)
            {
                throw new InvalidDataException($"TreeAligner.Adapters.AlignZone received a ParallelTextSegment with a source segment that includes refs that are not VerseRefs");
            }

            var books = parallelTextSegment.SourceSegmentRefs
                .Select(r => ((VerseRef)r).Book)
                .Distinct();
            if (books.Count() > 1)
            {
                throw new InvalidDataException($"TreeAligner.Adapters.AlignZone received a ParallelTextSegment with a source segment that includes ref to more than one book");
            }
            if (books.Count() == 0)
            {
                throw new InvalidDataException($"TreeAligner.Adapters.AlignZone received a ParallelTextSegment with a source segment that includes ref without a book");
            }

            var chapterNumbers = parallelTextSegment.SourceSegmentRefs
                .Select(r => ((VerseRef)r).ChapterNum)
                .Distinct();
            if (chapterNumbers.Count() > 1)
            {
                throw new InvalidDataException($"TreeAligner.Adapters.AlignZone received a ParallelTextSegment with a source segment that includes ref to more than one chapterNum");
            }
            if (chapterNumbers.Count() == 0)
            {
                throw new InvalidDataException($"TreeAligner.Adapters.AlignZone received a ParallelTextSegment with a source segment that includes ref without a chapterNum");
            }

            var verseNumbers = parallelTextSegment.SourceSegmentRefs
                .Select(r => ((VerseRef)r).VerseNum)
                .Distinct();

            XElement? versesXElementCombined = manuscriptTree.GetVersesXElementsCombined(books.First(), chapterNumbers.FirstOrDefault(), verseNumbers);

            if (versesXElementCombined == null)
            {
                throw new InvalidDataException(@$"TreeAligner.Adapters.AlignZone got a versesXElementCombined that is null for 
                    book {books.First()} chapter {chapterNumbers.First()} verses {string.Join(" ", verseNumbers)}");
            }
            /*
            public record Target(
    TargetText TargetText,
    TargetLemma TargetLemma,
    TargetID TargetID);

                public readonly struct TargetID
        {
            public int Book => int.Parse(_tag.Substring(0, 2));
            public int Chapter => int.Parse(_tag.Substring(2, 3));
            public int Verse => int.Parse(_tag.Substring(5, 3));
            public int Word => int.Parse(_tag.Substring(8, 3));

            public ChapterID ChapterID => new ChapterID(_tag.Substring(0, 5));
            public VerseID VerseID => new VerseID(_tag.Substring(0, 8));

            private readonly string _tag;

            public TargetID(string tag) { _tag = tag; }

            public TargetID(
                int book, int chapter, int verse, int word)
            {
                _tag = $"{book:D2}{chapter:D3}{verse:D3}{word:D3}";
            }

            public TargetID(
                VerseID verseID,
                int word)
            {
                _tag = $"{verseID.AsCanonicalString}{word:D3}";
            }

            public string AsCanonicalString => _tag;
        }
        */

            if (parallelTextSegment.TargetSegment is not TokensTextSegment)
            {
                throw new InvalidDataException("parallelTextSegment supplied is not a TokensTextSegment, which is required for extracting target points.");
            }

            //FIXME: CHECK THIS!
            IEnumerable<Target> targets = ((TokensTextSegment)parallelTextSegment.TargetSegment).Tokens
                .Select(t => new Target(new TargetText(t.Text), new TargetLemma(t.Text), new TargetID(t.TokenId.ToString())));

            List<MonoLink> monoLinks = ZoneAlignment.GetMonoLinks(
                versesXElementCombined,
                ZoneAlignment.GetSourcePoints(versesXElementCombined),
                ZoneAlignment.GetTargetPoints(targets.ToList()),
                config);

            throw new NotImplementedException();
        }
    }
}
