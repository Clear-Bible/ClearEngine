using System.Xml.Linq;

namespace ClearBible.Engine.SyntaxTree.Aligner.Legacy
{
    public readonly struct SourceID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));
        public int Verse => int.Parse(_tag.Substring(5, 3));
        public int Word => int.Parse(_tag.Substring(8, 3));
        public int Subsegment => int.Parse(_tag.Substring(11, 1));

        public string BookChapterVerse => _tag.Substring(0, 8);

        private readonly string _tag;

        public SourceID(string tag) { _tag = tag; }
        public string AsCanonicalString => _tag;
    }

    public readonly struct TargetID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));
        public int Verse => int.Parse(_tag.Substring(5, 3));
        public int Word => int.Parse(_tag.Substring(8, 3));
        public int Subsegment => int.Parse(_tag.Substring(11, 1));
        public string BookChapterVerse => _tag.Substring(0, 8);

        private readonly string _tag;
        public TargetID(string tag) { _tag = tag; }
        public string AsCanonicalString => _tag;
        public override bool Equals(object? obj) => obj is TargetID other && Equals(other);
        public bool Equals(TargetID t) => _tag == t._tag;
        public override int GetHashCode() => _tag.GetHashCode();
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

    public record MaybeTargetPoint(TargetPoint? TargetPoint)
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

        public double RelativePos =>
            TargetPoint?.RelativePosition ?? 0.0;
    }
    public record OpenTargetBond(MaybeTargetPoint MaybeTargetPoint, double Score)
    {
        public bool HasTargetPoint => MaybeTargetPoint.TargetPoint != null;
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
        public string? Gloss1;
        public string? Gloss2;
    }
}
