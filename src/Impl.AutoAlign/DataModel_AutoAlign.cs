using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ClearBible.Clear3.API;
using ClearBible.Clear3.Impl.TreeService;
using System.Linq.Expressions;

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
    /// A version of TargetBond that might not have a TargetPoint.
    /// </summary>
    /// 
    public record OpenTargetBond(
        MaybeTargetPoint MaybeTargetPoint,
        double Score)
    {
        public bool HasTargetPoint => !MaybeTargetPoint.IsNothing;
    }


    /// <summary>
    /// A version of MonoLink with an OpenTargetBond instead of a
    /// TargetBond, and whose OpenTargetBond property can be reset.
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
    /// A Candidate represents a sequence of zero or more MaybeTargetPoint
    /// objects, and with a probability that is associated with the sequence
    /// as a whole.  (In order to interpret what the Candidate means, one has
    /// to have a matching sequence of SourcePoint objects in mind; the candidate
    /// represents the assignment of the MaybeTargetPoint to the corresponding
    /// SourcePoint.)
    /// </summary>
    ///
    /// FIXME: Would like to consider ways to rework Candidate,
    /// CandidateChain, and associated data structures in the tree-based
    /// alignment algorithm to simplify and make understanding easier.
    /// Would it help somehow to use maps instead of lists in some places?
    /// Would like to get rid of the untyped ArrayList that shows up as
    /// the base class of CandidateChain.
    /// Would it help to use OpenTargetBond earlier in the process?
    /// Is there a way to make the meaning inherent in Candidate and
    /// Candidate chain without a matching sequence of SourcePoint
    /// objects that is implicit?
    /// 
    public class Candidate_Old
    {
        /// <summary>
        /// The sequence of assignments represented by this Candidate.
        /// </summary>
        /// 
        public CandidateChain Chain;

        /// <summary>
        /// The overall probability of the sequence of assignments
        /// represented by this Candidate.
        /// </summary>
        /// 
        public double Prob;

        /// <summary>
        /// Constructs a Candidate with a sequence of zero assignments.
        /// </summary>
        /// 
        public Candidate_Old()
        {
            Chain = new CandidateChain();
        }

        /// <summary>
        /// Constructs a Candidate with a sequence of exactly
        /// one MaybeTargetPoint, which is given.
        /// </summary>
        /// 
        public Candidate_Old(MaybeTargetPoint tw, double probability)
        {
            Chain = new CandidateChain(Enumerable.Repeat(tw, 1));
            Prob = probability;
        }

        /// <summary>
        /// Constructs a Candidate with the sequence of assignments
        /// that is given by a CandidateChain.
        /// </summary>
        ///
        public Candidate_Old(CandidateChain chain, double probability)
        {
            Chain = chain;
            Prob = probability;
        }
    }

    /// <summary>
    /// A CandidateChain is the sequence of assignments that occurs
    /// inside of a Candidate.  Each CandidateChain is either a list of
    /// Candidate objects or a list of MaybeTargetPoint objects.
    /// (In order to interpret what the CandidateChain means, one has
    /// to have a matching sequence of SourcePoint objects in mind; the
    /// candidate represents the assignment of the MaybeTargetPoint to
    /// the corresponding SourcePoint.)
    /// </summary>
    ///
    /// FIXME: See FIXME notes for Candidate.
    /// 
    public class CandidateChain : ArrayList
    {
        public CandidateChain()
            : base()
        {
        }

        public CandidateChain(IEnumerable<Candidate_Old> candidates)
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
    public class AlternativeCandidates : List<Candidate_Old>
    {
        public AlternativeCandidates()
            : base()
        {
        }

        public AlternativeCandidates(IEnumerable<Candidate_Old> candidates)
            : base(candidates)
        {
        }
    }


    /// <summary>
    /// An AlternativesForTerminals object maps a SourceID (as a canonical
    /// string) to a list of Candidates that represent alternative
    /// assignments of a TargetPoint for the associated SourcePoint.
    /// </summary>
    /// 
    public class AlternativesForTerminals : Dictionary<string, List<Candidate_Old>>
    {
        public AlternativesForTerminals()
            : base()
        {
        }
    }
    

    /// <summary>
    /// Alternative candidates for source points.
    /// </summary>
    /// 
    public record AltCandsForSourcePoint(
        Dictionary<SourceID, List<Candidate>> Dictionary);


    /// <summary>
    /// Alternative candidates for nodes of the syntax tree.
    /// All of the nodes in the same stack of one-child nodes
    /// share the same alternatives.
    /// </summary>
    /// 
    public record AltCandsForSyntaxNode(
        Dictionary<TreeNodeStackID, List<Candidate>> Dictionary);


    
}
