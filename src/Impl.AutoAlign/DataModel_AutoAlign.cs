using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ClearBible.Clear3.API;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    // Internal Data Model for Tree-based Auto Alignment Algorithm
    //------------------------------------------------------------


    /// <summary>
    /// Something that might be a TargetPoint or might
    /// be nothing.
    /// </summary>
    /// 
    public record MaybeTargetPoint(
        TargetPoint TargetPoint)
    {
        public string ID =>
            TargetPoint?.TargetID.AsCanonicalString ?? "0";

        public string AltID =>
            TargetPoint?.AltID ?? "";

        public string Lower =>
            TargetPoint?.Lower ?? "";

        public string Text =>
            TargetPoint?.Text ?? "";

        public int Position =>
            TargetPoint?.Position ?? -1;

        public bool IsNothing =>
            TargetPoint == null;

        public double RelativePos =>
            TargetPoint?.RelativePosition ?? 0.0;
    }


    /// <summary>
    /// A TargetBond that might not have a TargetPoint.
    /// </summary>
    /// 
    public record OpenTargetBond(
        MaybeTargetPoint MaybeTargetPoint,
        double Score)
    {
        public bool HasTargetPoint => !MaybeTargetPoint.IsNothing;
    }


    /// <summary>
    /// A MonoLink with an OpenTargetBond instead of a
    /// TargetBond, and whose OpenTargetBond can be reset.
    /// </summary>
    /// 
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


    /// <summary>
    /// A Candidate represents a sequence of zero or more choices
    /// of MaybeTargetPoint objects, with also a probability associated with
    /// this sequence.
    /// </summary>
    /// 
    public class Candidate
    {
        /// <summary>
        /// The sequence of choices represented by this Candidate.
        /// </summary>
        /// 
        public CandidateChain Chain;

        /// <summary>
        /// The overall probability of the sequence of choices
        /// represented by this Candidate.
        /// </summary>
        /// 
        public double Prob;

        /// <summary>
        /// Constructs a Candidate with a sequence of zero choices.
        /// </summary>
        /// 
        public Candidate()
        {
            Chain = new CandidateChain();
        }

        /// <summary>
        /// Constructs a Candidate with a sequence of exactly
        /// one MaybeTargetPoint, which is given.
        /// </summary>
        /// 
        public Candidate(MaybeTargetPoint tw, double probability)
        {
            Chain = new CandidateChain(Enumerable.Repeat(tw, 1));
            Prob = probability;
        }

        /// <summary>
        /// Constructs a Candidate with a sequence of choices
        /// that is given.
        /// </summary>
        ///
        public Candidate(CandidateChain chain, double probability)
        {
            Chain = chain;
            Prob = probability;
        }
    }

    /// <summary>
    /// A CandidateChain is a sequence of MaybeTargetPoint objects
    /// or a sequence of Candidate objects.
    /// </summary>
    /// 
    public class CandidateChain : ArrayList
    {
        public CandidateChain()
            : base()
        {
        }

        public CandidateChain(IEnumerable<Candidate> candidates)
            : base(candidates.ToList())
        {
        }

        public CandidateChain(IEnumerable<MaybeTargetPoint> targetWords)
            : base(targetWords.ToList())
        {
        }
    }


    /// <summary>
    /// An AlternativeCandidates object is a list of Candidate
    /// objects that are alternatives to one another.
    /// </summary>
    /// 
    public class AlternativeCandidates : List<Candidate>
    {
        public AlternativeCandidates()
            : base()
        {
        }

        public AlternativeCandidates(IEnumerable<Candidate> candidates)
            : base(candidates)
        {
        }
    }


    /// <summary>
    /// An AlternativesForTerminals object maps a SourceID (as a canonical
    /// string) to a list of Candidates for that are alternatives for
    /// that source word.
    /// </summary>
    /// 
    public class AlternativesForTerminals : Dictionary<string, List<Candidate>>
    {
        public AlternativesForTerminals()
            : base()
        {
        }
    }
}
