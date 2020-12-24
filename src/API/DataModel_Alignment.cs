using System;
using System.Collections.Generic;
using System.Xml.Linq;


namespace ClearBible.Clear3.API
{
    // Data Model for Alignment
    //-------------------------

    /// <summary>
    /// The specification of one zone as it occurs in a SimpleVersification,
    /// consisting of a set of source verses that should be associated with a
    /// set of target verses.
    /// </summary>
    /// 
    public record SimpleZoneSpec(     
        List<VerseID> SourceVerses,
        List<VerseID> TargetVerses);


    /// <summary>
    /// The versification concept from Clear2, consisting of a set of
    /// SimpleZoneSpec specifications.
    /// </summary>
    /// 
    public record SimpleVersification(
        List<SimpleZoneSpec> List);


    /// <summary>
    /// A string which is to be interpreted as target text.
    /// </summary>
    /// 
    public record TargetText(string Text);

    // FIXME: introduce a LowerText(string Text) record type, and
    // change name of TargetText to TargetSurfaceText; adjust usage in
    // the system at large to distinguish between target lower-cased text
    // and target surface text.


    /// <summary>
    /// A string which is to be interpreted as source surface text.
    /// </summary>
    /// 
    public record SourceText(string Text);


    /// <summary>
    /// A string which is to be interpreted as a source lemma.
    /// </summary>
    /// 
    public record Lemma(string Text);


    /// <summary>
    /// A score that expresses relative merit among a set of choices.
    /// </summary>
    /// 
    public record Score(double Double);


    /// <summary>
    /// The count of members in some collection.
    /// </summary>
    /// 
    public record Count(int Int);


    /// <summary>
    /// A threshold for judging whether a count is large enough
    /// for some purpose.
    /// </summary>
    /// 
    public record CountThreshold(int Int);


    /// <summary>
    /// A source segment instance as located in the source manuscript,
    /// with its surface text, lemma, and identification.
    /// </summary>
    /// 
    public record Source(
        SourceText SourceText,
        Lemma Lemma,
        SourceID SourceID);


    /// <summary>
    /// A source verse, consisting of exactly the sequence of the source
    /// segment instances in manuscript order for a particular verse.
    /// </summary>
    /// 
    public record SourceVerse(
        List<Source> List);


    /// <summary>
    /// A portion of the source manuscript, consisting of a sequence of source
    /// segment instances in order, perhaps collected from more than one verse,
    /// which has been identified for alignment with some portion of the
    /// target translation.
    /// </summary>
    /// 
    public record SourceZone(
        List<Source> List);


    /// <summary>
    /// A word instance as located in a target translation, with its surface
    /// text and identification.
    /// </summary>
    /// 
    public record Target(
        TargetText TargetText,
        TargetID TargetID);


    /// <summary>
    /// A verse from the target translation, consisting of exactly the sequence
    /// of target word instances in order for a particular verse.
    /// </summary>
    /// 
    public record TargetVerse(
        List<Target> List);


    /// <summary>
    /// A portion of the target translation, consisting of a sequence of
    /// translated word instances in order, perhaps collected from more than
    /// one verse, which has been identified for alignment with some portion
    /// of the source manuscript.
    /// </summary>
    /// 
    public record TargetZone(
        List<Target> List);


    /// <summary>
    /// The target translation, expressed as a collection of target verses.
    /// </summary>
    /// 
    public record TargetVerseCorpus(
        List<TargetVerse> List);


    /// <summary>
    /// A SourceZone and TargetZone that are to be aligned with one another.
    /// </summary>
    /// 
    public record ZonePair(
        SourceZone SourceZone,
        TargetZone TargetZone);


    /// <summary>
    /// A collection of ZonePair objects that are to be aligned.
    /// </summary>
    /// 
    public record ParallelCorpora(
        List<ZonePair> List);


    /// <summary>
    /// A version of ZonePair in the form used as input to the
    /// tree-based auto-alignment algorithm; the source zone must
    /// be a sequence of contiguous verses, and is specified by
    /// identifying the first and last source verses.
    /// </summary>
    /// 
    public record ZoneAlignmentProblem(
        TargetZone TargetZone,
        VerseID FirstSourceVerseID,
        VerseID LastSourceVerseID);


    /// <summary>
    /// A database of meanings for source lemmas; each meaning is
    /// the lowercased text of a target word with an associated score.
    /// Possible sources of a TranslationModel include (1) training a
    /// statistical translation model and (2) analyzing a database of
    /// manually checked alignments.
    /// </summary>
    /// 
    public record TranslationModel(
        Dictionary<Lemma, Dictionary<TargetText, Score>> Dictionary);


    /// <summary>
    /// Expresses an association between a particular source segment instance
    /// and target translated word instance.
    /// </summary>
    /// 
    public record BareLink(
        SourceID SourceID,
        TargetID TargetID);


    /// <summary>
    /// A database of possible alignments between source segment instances
    /// and target translated words, each with an associated score.  Possible
    /// sources of an AlignmentModel include training a statistical translation
    /// model.
    /// </summary>
    /// 
    public record AlignmentModel(
        Dictionary<BareLink, Score> Dictionary);



    /// <summary>
    /// Describes a source segment instance in the context of its membership
    /// in a source zone.
    /// </summary>
    /// 
    public record SourcePoint(
        string Lemma,
        XElement Terminal,  // terminal node in the syntax tree, to which node
                            // the source segment is associated
                            // FIXME: replace with something meaningful to
                            // clients of the API
        SourceID SourceID,
        string AltID,  // alternative identification in the form of, for
                       // example, "λόγος-2" to mean the second occurence of
                       // the surface form "λόγος" within this zone
        int TreePosition,  // zero-based position within the sequence of
                           // terminal nodes in syntax tree order for this
                           // zone
        double RelativeTreePosition,  // the TreePosition restated as a
                                      // fraction, at least 0, less than 1
        int SourcePosition  // zero-based position within the sequence of
                            // source segments in manuscript order for this
                            // zone
        );


    /// <summary>
    /// Describes a target translated word in the context of its membership
    /// within a target zone.
    /// </summary>
    /// 
    public record TargetPoint(
        string Text,   // surface text
        string Lower,  // Text as converted to lowercase
        TargetID TargetID,
        string AltID,  // alternative identification in the form of, for
                       // example, "word-2" to mean the second occurrence of
                       // the surface text "word" within this zone
        int Position,  // zero-based position within the sequence of target
                       // words in translation order for this zone,
        double RelativePosition  // the Position restated as a fraction,
                                 // at least 0, less than 1
        );


    /// <summary>
    /// Information about the context of a zone, expressed as a list of
    /// the source points in manuscript order and a list of the target points
    /// in translation order.
    /// </summary>
    /// 
    public record ZoneContext(
        List<SourcePoint> SourcePoints,
        List<TargetPoint> TargetPoints);


    /// <summary>
    /// A link to a target point, with an associated score.
    /// </summary>
    /// 
    public record TargetBond(
        TargetPoint TargetPoint,
        double Score);
    // FIXME: also need to track why a bond was made, might go here


    /// <summary>
    /// An association between one source point and one target point
    /// with an associated score.
    /// </summary>
    /// 
    public record MonoLink(
        SourcePoint SourcePoint,
        TargetBond TargetBond);


    /// <summary>
    /// An association between a set of source points and a set of
    /// target points, where each target point has an associated score.
    /// </summary>
    /// 
    public record MultiLink(
        List<SourcePoint> Sources,
        List<TargetBond> Targets);


    /// <summary>
    /// Alignment results for a zone, consisting of the zone context and
    /// a collection of one-to-one links between source points and target
    /// points.
    /// </summary>
    /// 
    public record ZoneMonoAlignment(
        ZoneContext ZoneContext,
        List<MonoLink> MonoLinks);


    /// <summary>
    /// Alignment results for a zone, consisting of the zone context and
    /// a collection of many-to-many links between source points and
    /// target points.
    /// </summary>
    /// 
    public record ZoneMultiAlignment(
        ZoneContext ZoneContext,
        List<MultiLink> MultiLinks);


    /// <summary>
    /// A collection of ZoneMultiAlignment objects that together express the
    /// alignment results for a set of zones.
    /// </summary>
    /// 
    public record ZoneMultiAlignments(
        List<ZoneMultiAlignment> List);    
}
