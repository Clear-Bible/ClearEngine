using SIL.Machine.Corpora;
using SIL.Scripture;
using System.Text;

namespace ClearBible.Engine.Corpora
{
    public static class MachinePatchExtensions
    {
        public static IEnumerable<(string Text, VerseRef RefCorpusVerseRef, VerseRef? CorpusVerseRef)> ExtractScripture(
            this ITextCorpus corpus,
            ITextCorpus? refCorpus = null
        )
        {
            if (refCorpus == null)
                refCorpus = ScriptureTextCorpus.CreateVersificationRefCorpus();

            var parallelCorpus = refCorpus.AlignRows(corpus, allSourceRows: true);
            VerseRef? curRef = null;
            VerseRef? curTrgRef = null;
            var curTrgLine = new StringBuilder();
            bool curTrgLineRange = true;
            foreach (ParallelTextRow row in parallelCorpus)
            {
                var vref = (VerseRef)row.Ref;
                if (
                    curRef.HasValue
                    && vref.CompareTo(curRef.Value, null, compareAllVerses: true, compareSegments: false) != 0
                )
                {
                    yield return (curTrgLineRange ? "<range>" : curTrgLine.ToString(), curRef.Value, curTrgRef);
                    curTrgLine = new StringBuilder();
                    curTrgLineRange = true;
                    curTrgRef = null;
                }

                curRef = vref;
                if (!curTrgRef.HasValue && row.TargetRefs.Count > 0)
                {
                    curTrgRef = (VerseRef)row.TargetRefs[0];
                }
                else if (curTrgRef.HasValue && row.TargetRefs.Count > 0 && !curTrgRef.Value.Equals(row.TargetRefs[0]))
                {
                    curTrgRef.Value.Simplify();
                    var trgRef = (VerseRef)row.TargetRefs[0];
                    VerseRef startRef,
                        endRef;
                    if (curTrgRef.Value < trgRef)
                    {
                        startRef = curTrgRef.Value;
                        endRef = trgRef;
                    }
                    else
                    {
                        startRef = trgRef;
                        endRef = curTrgRef.Value;
                    }
                    if (startRef.Chapter == endRef.Chapter)
                    {
                        curTrgRef = new VerseRef(
                            startRef.Book,
                            startRef.Chapter,
                            $"{startRef.VerseNum}-{endRef.VerseNum}",
                            startRef.Versification
                        );
                    }
                    else
                    {
                        curTrgRef = endRef;
                    }
                }

                if (!row.IsTargetInRange || row.IsTargetRangeStart || row.TargetText.Length > 0)
                {
                    if (row.TargetText.Length > 0)
                    {
                        if (curTrgLine.Length > 0)
                            curTrgLine.Append(" ");
                        curTrgLine.Append(row.TargetText);
                    }
                    curTrgLineRange = false;
                }
            }

            if (curRef.HasValue)
                yield return (curTrgLineRange ? "<range>" : curTrgLine.ToString(), curRef.Value, curTrgRef);
        }
    }
}
