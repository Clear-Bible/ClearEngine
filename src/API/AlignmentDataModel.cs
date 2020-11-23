using System;
using System.Collections.Generic;
using System.Xml.Linq;


namespace ClearBible.Clear3.API
{
    // Data Model for Alignment


    public record TargetText(string Text);

    public record SourceText(string Text);

    public record Lemma(string Text);

    public record Score(double Double);

    public record Count(int Int);

    public record CountThreshold(int Int);


    public record Target(
        TargetText TargetText,
        TargetID TargetID);

    public record TranslationPair(
        List<Target> Targets,
        VerseID FirstSourceVerseID,
        VerseID LastSourceVerseID);


    public record BareLink(
        SourceID SourceID,
        TargetID TargetID);

    public record AlignmentModel(
        Dictionary<BareLink, Score> Inner);

    


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



    // Move this somewhere else ...
    // FIXME -- exactly two glosses?
    public class Gloss
    {
        public string Gloss1;
        public string Gloss2;
    }
}
