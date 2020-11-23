using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using System.Net.Http.Headers;
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.TreeService;


    
    
    


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


    public record OpenTargetBond(
        MaybeTargetPoint MaybeTargetPoint,
        double Score)
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



    public class Candidate
    {
        public CandidateChain Chain;
        public double Prob;

        public Candidate()
        {
            Chain = new CandidateChain();
        }

        public Candidate(MaybeTargetPoint tw, double probability)
        {
            Chain = new CandidateChain(Enumerable.Repeat(tw, 1));
            Prob = probability;
        }

        public Candidate(CandidateChain chain, double probability)
        {
            Chain = chain;
            Prob = probability;
        }
    }

    /// <summary>
    /// A CandidateChain is a sequence of TargetWord objects
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
    /// An AlternativesForTerminals object is a mapping:
    /// SourceWord.ID => AlternativeCandidates.
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
