using System.Xml.Linq;

namespace ClearBible.Engine.TreeAligner.Legacy
{
    public readonly struct ChapterID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));

        private readonly string _tag;
        public ChapterID(string tag) { _tag = tag; }
        public ChapterID(
            int book, int chapter)
        {
            _tag = $"{book:D2}{chapter:D3}";
        }
        public string AsCanonicalString => _tag;

        /// <summary>
        /// A special value of ChapterID that has book 0 and chapter 0,
        /// and that means "no chapter".
        /// </summary>
        /// 
        public static ChapterID None => new ChapterID("00000");
    }

    public readonly struct VerseID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));
        public int Verse => int.Parse(_tag.Substring(5, 3));
        public ChapterID ChapterID => new ChapterID(_tag.Substring(0, 5));

        private readonly string _tag;
        public VerseID(string tag) { _tag = tag; }
        public VerseID(
            int book, int chapter, int verse)
        {
            _tag = $"{book:D2}{chapter:D3}{verse:D3}";
        }
        public string AsCanonicalString => _tag;
    }

    public readonly struct SourceID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));
        public int Verse => int.Parse(_tag.Substring(5, 3));
        public int Word => int.Parse(_tag.Substring(8, 3));
        public int Subsegment => int.Parse(_tag.Substring(11, 1));

        public ChapterID ChapterID => new ChapterID(_tag.Substring(0, 5));
        public VerseID VerseID => new VerseID(_tag.Substring(0, 8));

        private readonly string _tag;

        public SourceID(string tag) { _tag = tag; }

        public SourceID(
            int book, int chapter, int verse, int word, int subsegment)
        {
            _tag = $"{book:D2}{chapter:D3}{verse:D3}{word:D3}{subsegment:D1}";
        }

        public string AsCanonicalString => _tag;
    }

    public record SourcePoint(
        string Lemma,
        string Category,
        XElement Terminal,  // terminal node in the syntax tree, to which node
                            // the source segment is associated
                            // FIXME: replace with something meaningful to
                            // clients of the API
        SourceID SourceID,
        string AltID,  // alternative identification in the form of, for
                       // example, "λόγος-2" to mean the second occurence of
                       // the surface form "λόγος" within this zone FIXME: should use an AltID record
        int TreePosition,  // zero-based position within the sequence of
                           // terminal nodes in syntax tree order for this
                           // zone
        double RelativeTreePosition,  // the TreePosition restated as a
                                      // fraction, at least 0, less than 1
        int SourcePosition  // zero-based position within the sequence of
                            // source segments in manuscript order for this
                            // zone
    );
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

    public record TargetText(string Text);
    public record TargetLemma(string Text);
    public record Target(TargetText TargetText, TargetLemma TargetLemma, TargetID TargetID);


    public record TargetPoint(
    string Text,
    string Lemma,  // lemma text, usually text as converted to lowercase
    TargetID TargetID,
    string AltID,  // alternative identification in the form of, for
                   // example, "word-2" to mean the second occurrence of
                   // the surface text "word" within this zone FIXME: should use an AltID record
    int Position,  // zero-based position within the sequence of target
                   // words in translation order for this zone,
    double RelativePosition  // the Position restated as a fraction,
                             // at least 0, less than 1
    );


    public record TargetBond(TargetPoint TargetPoint, double Score);
    public record MonoLink(SourcePoint SourcePoint, TargetBond TargetBond);

    public record MaybeTargetPoint(TargetPoint TargetPoint)
    {
        public string ID =>
            TargetPoint?.TargetID.AsCanonicalString ?? "0";

        public string AltID =>
            TargetPoint?.AltID ?? "";

        public string Lemma =>
            TargetPoint?.Lemma ?? "";

        public string Text =>
            TargetPoint?.Text ?? "";

        public int Position =>
            TargetPoint?.Position ?? -1;

        public bool IsNothing =>
            TargetPoint == null;

        public double RelativePos =>
            TargetPoint?.RelativePosition ?? 0.0;
    }
    public record OpenTargetBond(MaybeTargetPoint MaybeTargetPoint, double Score)
    {
        public bool HasTargetPoint => !MaybeTargetPoint.IsNothing;
    }
    public class OpenMonoLink
    {
        public SourcePoint SourcePoint { get; }
        public OpenTargetBond OpenTargetBond { get; private set; }

        public bool HasTargetPoint =>
            OpenTargetBond.HasTargetPoint;

        public OpenMonoLink(
            SourcePoint sourcePoint,
            OpenTargetBond openTargetBond)
        {
            SourcePoint = sourcePoint;
            OpenTargetBond = openTargetBond;
        }

        public void ResetOpenTargetBond(OpenTargetBond bond)
        {
            OpenTargetBond = bond;
        }
    }

    public struct TreeNodeID
    {
        /// <summary>
        /// The tag is a string of decimal digits of the form
        /// BBCCCVVVPPPSSSL, where BBCCCVVV identifies the verse (as in
        /// a canonical VerseID string), PPP is the position, SSS is the
        /// span, and L is the level.
        /// </summary>
        /// 
        private string _tag;

        public string AsCanonicalString => _tag;

        public TreeNodeID(string canonicalString)
        {
            _tag = canonicalString;
        }

        public TreeNodeID(
            int book,
            int chapter,
            int verse,
            int position,
            int span,
            int level)
        {
            _tag = $"{book:D2}{chapter:D3}{verse:D3}{position:D3}{span:D3}{level:D1}";
        }

        public TreeNodeID(
            VerseID verseID,
            int position,
            int span,
            int level)
        {
            _tag = $"{verseID.Book:D2}{verseID.Chapter:D3}{verseID.Verse:D3}{position:D3}{span:D3}{level:D1}";
        }

        /// <summary>
        /// The verse containing the first terminal under the node.
        /// </summary>
        /// 
        public VerseID VerseID =>
            new VerseID(_tag.Substring(0, 8));

        /// <summary>
        /// The 1-based position of the first terminal under the node
        /// within the verse.
        /// </summary>
        /// 
        public int Position => int.Parse(_tag.Substring(8, 3));

        /// <summary>
        /// The number of terminals under the node.
        /// </summary>
        /// 
        public int Span => int.Parse(_tag.Substring(11, 3));

        /// <summary>
        /// The 0-based position of this node in a stack of nodes
        /// that all have the same verse, start, and span.  Each node
        /// except the bottom one has just one child, which is the node
        /// below it in the stack.  The position is counted from the
        /// bottom of the stack, nearest the leaves of the tree.
        /// </summary>
        /// 
        public int Level => int.Parse(_tag.Substring(14, 1));

        public TreeNodeStackID TreeNodeStackID => new TreeNodeStackID(_tag.Substring(0, 14));
    }

    public struct TreeNodeStackID
    {
        /// <summary>
        /// The tag is a string of decimal digits of the form
        /// BBCCCVVVPPPSSS, where BBCCCVVV identifies the verse (as in
        /// a canonical VerseID string), PPP is the position, and SSS is
        /// the span.
        /// </summary>
        /// 
        private string _tag;

        public string AsCanonicalString => _tag;

        public TreeNodeStackID(string canonicalString)
        {
            _tag = canonicalString;
        }

        public TreeNodeStackID(
            int book,
            int chapter,
            int verse,
            int position,
            int span)
        {
            _tag = $"{book:D2}{chapter:D3}{verse:D3}{position:D3}{span:D3}";
        }

        public TreeNodeStackID(
            VerseID verseID,
            int position,
            int span)
        {
            _tag = $"{verseID.Book:D2}{verseID.Chapter:D3}{verseID.Verse:D3}{position:D3}{span:D3}";
        }

        /// <summary>
        /// The verse containing the first terminal under the node.
        /// </summary>
        /// 
        public VerseID VerseID =>
            new VerseID(_tag.Substring(0, 8));

        /// <summary>
        /// The 1-based position of the first terminal within
        /// the verse.
        /// </summary>
        /// 
        public int Position => int.Parse(_tag.Substring(8, 3));

        /// <summary>
        /// The number of terminals under the node.
        /// </summary>
        /// 
        public int Span => int.Parse(_tag.Substring(11, 3));
    }

    public record SourceLemma(string Text);
    public record Score(double Double);
    public record TranslationModel(Dictionary<SourceLemma, Dictionary<TargetLemma, Score>> Dictionary);
    public record BareLink(SourceID SourceID, TargetID TargetID);
    public record AlignmentModel(Dictionary<BareLink, Score> Dictionary);

    public delegate bool TryGet<TKey, TValue>(TKey key, out TValue value);

    public record SourceLemmasAsText(string Text);
    public record TargetGroupAsText(string Text);
    public record PrimaryPosition(int Int);
    public record TargetGroup(TargetGroupAsText TargetGroupAsText, PrimaryPosition PrimaryPosition);
    public record GroupTranslationsTable(Dictionary<SourceLemmasAsText,  HashSet<TargetGroup>>  Dictionary);

    public class Gloss
    {
        public string Gloss1;
        public string Gloss2;
    }
}
