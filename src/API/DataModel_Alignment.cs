using System;
using System.Collections.Generic;
using System.Xml.Linq;


namespace ClearBible.Clear3.API
{
    // Data Model for Alignment
    //-------------------------


    public record SimpleZoneSpec(
        List<VerseID> SourceVerses,
        List<VerseID> TargetVerses);


    public record SimpleVersification(
        List<SimpleZoneSpec> List);


    public record TargetText(string Text);


    public record SourceText(string Text);


    public record Lemma(string Text);


    public record Score(double Double);


    public record Count(int Int);


    public record CountThreshold(int Int);


    public record Source(
        SourceText SourceText,
        Lemma Lemma,
        SourceID SourceID);


    public record SourceVerse(
        List<Source> List);


    public record SourceZone(
        List<Source> List);


    public record Target(
        TargetText TargetText,
        TargetID TargetID);


    public record TargetVerse(
        List<Target> List);


    public record TargetZone(
        List<Target> List);


    public record TargetVerseCorpus(
        List<TargetVerse> List);



    public record ZonePair(
        SourceZone SourceZone,
        TargetZone TargetZone);


    public record ParallelCorpora(
        List<ZonePair> List);



    public record ZoneAlignmentProblem(
        TargetZone TargetZone,
        VerseID FirstSourceVerseID,
        VerseID LastSourceVerseID);



    public record TranslationModel(
        Dictionary<Lemma, Dictionary<TargetText, Score>> Dictionary);


    public record BareLink(
        SourceID SourceID,
        TargetID TargetID);


    public record AlignmentModel(
        Dictionary<BareLink, Score> Dictionary);

    


    public record SourcePoint(
        string Lemma,
        XElement Terminal,
        SourceID SourceID,
        string AltID,
        int TreePosition,
        double RelativeTreePosition,
        int SourcePosition);


    public record TargetPoint(
        string Text,
        string Lower,
        TargetID TargetID,
        string AltID,
        int Position,
        double RelativePosition);


    public record ZoneContext(
        List<SourcePoint> SourcePoints,
        List<TargetPoint> TargetPoints);


    public record TargetBond(
        TargetPoint TargetPoint,
        double Score);
    // FIXME: Someday may also need to track why a bond was made.


    public record MonoLink(
        SourcePoint SourcePoint,
        TargetBond TargetBond);


    public record MultiLink(
        List<SourcePoint> Sources,
        List<TargetBond> Targets);


    public record ZoneMonoAlignment(
        ZoneContext ZoneContext,
        List<MonoLink> MonoLinks);


    public record ZoneMultiAlignment(
        ZoneContext ZoneContext,
        List<MultiLink> MultiLinks);


    public record ZoneMultiAlignments(
        List<ZoneMultiAlignment> List);



    


    
}
