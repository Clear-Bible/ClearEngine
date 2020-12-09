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
    /// Information about a syntax tree node within the context of a zone.
    /// The nodes are numbered in postorder with stacks of one-child nodes
    /// collapsed.  Terminal nodes have a SourcePoint and no subnodes;
    /// Nonterminal nodes have no SourcePoint and have an array of the
    /// postorder numbers for the subnodes (and the subnode numbers are always
    /// less than the node's own postorder number because of the way that
    /// postorder numbers are assigned).
    /// </summary>
    /// 
    public record TreeNodeInfo(
        int PostOrderNumber,
        int[] SubNodes,
        SourcePoint SourcePoint)
    {
        public bool IsTerminal => SourcePoint is null;
    }


    /// <summary>
    /// Information about the syntax tree shape within the context of a zone.
    /// The nodes are numbered in postorder with stacks of one-child nodes
    /// collapsed.  The postorder numbers start at zero and continue to one
    /// less than the number of nodes.  The table is an array that maps the
    /// postorder number to information about the node.  You can visit the
    /// nodes in postorder by walking along the array from 0.
    /// </summary>
    /// 
    public record TreeInfoTable(
        TreeNodeInfo[] Array);



    public record CandidateTable(
        List<Candidate>[] Array);



    public record TerminalCandidate(
        TargetPoint TargetPoint,
        int Position,
        BitArray Range,
        int FirstPosition,
        int LastPosition,
        int NumberMotions,
        int NumberBackwardMotions,
        double LogJointProbability)
    {
        //public override bool Equals(object obj)
        //{
        //    if (obj is null)
        //        return false;
        //    else if (obj is TerminalCandidate tc)
        //        return Position == tc.Position;
        //    else if (obj is Candidate c && c.IsTerminal)
        //        return Position == c.GetTerminalCandidate().Position;
        //    else
        //        return false;
        //}
    }



    public record NonTerminalCandidate(
        int PostOrderNumber,
        int CandidateNumber,
        int[] SubCandidateNumbers,
        BitArray Range,
        int FirstPosition,
        int LastPosition,
        int NumberMotions,
        int NumberBackwardMotions,
        double LogJointProbability)
    {

    }


    /// <summary>
    /// The Candidate type is a discriminated union of the
    /// TerminalCandidate and NonTerminalCandidate types.  In
    /// other words, you should think of a Candidate object as
    /// being either a Candidate or a NonTerminalCandidate.
    /// </summary>
    /// 
    public class Candidate
    {
        /// <summary>
        /// The TerminalCandidate or NonTerminalCandidate object
        /// that is being represented.
        /// </summary>
        /// 
        private object _inner;

        /// <summary>
        /// Get the candidate with its type erased.
        /// </summary>
        /// 
        public object Get() => _inner;

        /// <summary>
        /// True if the object being represented is a TerminalCandidate.
        /// </summary>
        /// 
        public bool IsTerminal =>
            _inner.GetType() == typeof(TerminalCandidate);

        /// <summary>
        /// True if the object being represented is a NonTerminalCandidate.
        /// </summary>
        /// 
        public bool IsNonTerminal =>
            _inner.GetType() == typeof(NonTerminalCandidate);

        /// <summary>
        /// API to convert to TerminalCandidate; fails if it is not
        /// a TerminalCandidate.
        /// </summary>
        /// 
        public TerminalCandidate GetTerminalCandidate() =>
            (TerminalCandidate) _inner;

        /// <summary>
        /// API to convert to a NonTerminalCandidate; fails if it is not
        /// a NonTerminalCandidate.
        /// </summary>
        /// 
        public NonTerminalCandidate GetNonTerminalCandidate() =>
            (NonTerminalCandidate) _inner;

        /// <summary>
        /// Constructor from a TerminalCandidate.
        /// </summary>
        /// 
        public Candidate(TerminalCandidate c) { _inner = c; }

        /// <summary>
        /// Implicit type conversion from TerminalCandidate to Candidate.
        /// </summary>
        /// 
        public static implicit operator Candidate(TerminalCandidate c) =>
            new Candidate(c);

        /// <summary>
        /// Explicit type conversion from Candidate to TerminalCandidate.
        /// Fails if not a TerminalCandidate.
        /// </summary>
        /// 
        public static explicit operator TerminalCandidate(Candidate c) =>
            c.GetTerminalCandidate();

        /// <summary>
        /// Constructor from a NonTerminalCandidate.
        /// </summary>
        /// 
        public Candidate(NonTerminalCandidate c) { _inner = c; }

        /// <summary>
        /// Implicit type conversion from NonTerminalCandidate to Candidate.
        /// </summary>
        /// 
        public static implicit operator Candidate(NonTerminalCandidate c) =>
            new Candidate(c);

        /// <summary>
        /// Explicit type conversion from Candidate to NonTerminalCandidate.
        /// Fails if not a NonTerminalCandidate.
        /// </summary>
        /// 
        public static explicit operator NonTerminalCandidate(Candidate c) =>
            c.GetNonTerminalCandidate();

        /// <summary>
        /// Equality; delegated to the object inside.
        /// </summary>
        /// 
        public override bool Equals(object obj) => _inner.Equals(obj);

        /// <summary>
        /// Get hash code; delegated to the object inside.
        /// </summary>
        /// 
        public override int GetHashCode() => _inner.GetHashCode();
    }
}
