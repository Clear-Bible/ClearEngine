using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.AutoAlign
{
    /// <summary>
    /// Represents a set of target point positions, and provides some
    /// limited operations for working with them.  Intended to represent
    /// the set of target point positions for a Candidate.
    /// </summary>
    /// 
    public class TargetRange
    {
        private List<uint> _bitVectors;

        /// <summary>
        /// Creates a new empty set of positions.
        /// </summary>
        /// 
        public TargetRange()
        {
            _bitVectors = new();
        }

        /// <summary>
        /// Creates a singleton set containing only the specified
        /// position.
        /// </summary>
        /// 
        public TargetRange(int targetPosition)
        {
            int div = targetPosition / 32;
            int mod = targetPosition % 32;
            _bitVectors =
                Enumerable.Repeat((uint)0, div)
                .Concat(Enumerable.Repeat((uint)1 << mod, 1))
                .ToList();          
        }

        private TargetRange(List<uint> bitVectors)
        {
            _bitVectors = bitVectors;
        }

        /// <summary>
        /// Combines the receiver with another Range to produce a new
        /// Range that represents the union of the two sets, with also
        /// a flag that is true if there is any position in common
        /// between the two sets.
        /// </summary>
        /// 
        public (TargetRange, bool) Combine(TargetRange other)
        {
            bool conflicted =
                _bitVectors.Zip(other._bitVectors, (a, b) => a & b)
                .Any(x => x != 0);

            int cthis = _bitVectors.Count;
            int cother = other._bitVectors.Count;
            IEnumerable<uint> tail =
                cthis > cother
                ? _bitVectors.Skip(cother)
                : other._bitVectors.Skip(cthis);

            List<uint> union =
                _bitVectors.Zip(other._bitVectors, (a, b) => a | b)
                .Concat(tail)
                .ToList();

            return (new TargetRange(union), conflicted);
        }

        /// <summary>
        /// Get the list of positions that is represented by
        /// the receiver.
        /// </summary>
        /// 
        public List<int> Positions()
        {
            return
                _bitVectors
                .SelectMany((bv, n) =>
                    Enumerable.Range(0, 32)
                    .Select(k => new
                    {
                        position = 32 * n + k,
                        flag = ((1 << k) & bv) != 0
                    }))
                .Where(x => x.flag)
                .Select(x => x.position)
                .ToList();
        }
    }


    /// <summary>
    /// Different kinds of candidates, as explained further in
    /// the comments for the Candidate abstract class and its
    /// subclasses.
    /// </summary>
    /// 
    public enum CandidateKind
    {
        Point,       // links one source point to one target point
        EmptyPoint,  // links one source point to nothing
        Union,       // combines two sub-candidates
        Adjusted     // an underlying candidate with an adjusted score
    };


    /// <summary>
    /// Abstract class that represents a candidate alignment.  There
    /// are different kinds of candidates, as explained further in the
    /// comments for this class and its subclasses.
    /// </summary>
    /// 
    public abstract class Candidate
    {
        /// <summary>
        /// Make a new point Candidate that represents the link between
        /// one sourcePoint and one targetPoint with a specified
        /// score.
        /// </summary>
        /// <param name="numberTerminals">
        /// The number of terminals in the current zone (needed by
        /// the implementation).
        /// </param>
        /// 
        public static Candidate NewPoint(
            SourcePoint sourcePoint,
            TargetPoint targetPoint,
            double logScore)
            =>
            new PointCandidate(
                sourcePoint,
                targetPoint,
                logScore);

        /// <summary>
        /// Make a point Candidate that represents the certainty of linking
        /// a sourcePoint to nothing.
        /// </summary>
        /// <param name="numberTerminals">
        /// The number of terminals in the current zone (needed by
        /// the implementation).
        /// </param>
        /// 
        public static Candidate NewEmptyPoint(
            SourcePoint sourcePoint)
            =>
            new EmptyPointCandidate(sourcePoint);

        /// <summary>
        /// Make a new Candidate that represents all of the links in
        /// the receiver and all of the links in tail.
        /// </summary>
        /// 
        public Candidate Union(Candidate tail) =>
            new UnionCandidate(this, tail);

        /// <summary>
        /// Make a new Candidate that refers to the receiver as
        /// its underlying candidate and has an adjusted score.
        /// </summary>
        /// 
        public Candidate WithAdjustedScore(double logScore) =>
            new AdjustedScoreCandidate(this, logScore);


        public abstract CandidateKind Kind { get; }

        /// <summary>
        /// True if this Candidate was create by NewPoint() or
        /// NewEmptyPoint().
        /// </summary>
        /// 
        public abstract bool IsPoint { get; }

        /// <summary>
        /// True if this Candidate was created by Union().
        /// </summary>
        /// 
        public abstract bool IsUnion { get; }

        /// <summary>
        /// True if this Candidate was created by WithAdjustedScore().
        /// </summary>
        /// 
        public abstract bool IsAdjusted { get; }

        /// <summary>
        /// The source point if IsPoint is true, and null
        /// for other kinds of Candidate.
        /// </summary>
        /// 
        public abstract SourcePoint SourcePoint { get; }

        /// <summary>
        /// The target point if this Candidate was created
        /// by NewPoint(), and null otherwise.  In particular,
        /// is null for a point candidate that represents the
        /// certainty of linking a source point to nothing.
        /// </summary>
        /// 
        public abstract TargetPoint TargetPoint { get; }

        /// <summary>
        /// The right child of a union candidate.
        /// Is null for other kinds of candidate.
        /// </summary>
        /// 
        public abstract Candidate Head { get; }

        /// <summary>
        /// The left child of a union candidate.
        /// Is null for other kinds of candidates.
        /// </summary>
        /// 
        public abstract Candidate Tail { get; }

        /// <summary>
        /// The underlying Candidate if this is an adjusted-score
        /// Candidate.  Is null for other kinds of candidates.
        /// </summary>
        /// 
        public abstract Candidate Underlying { get; }

        /// <summary>
        /// The logarithm of a probability-like score for
        /// this candidate.
        /// If the Candidate was created by NewPoint, then
        /// equals the score that was specified.
        /// If the Candidate was created by NewEmptyPoint,
        /// then equals 0.
        /// If the Candidate was created by Union(), then
        /// equals the sum of the scores of the head and tail.
        /// If the Candidate was created by WithAdjustedScore(),
        /// then equals the score that was specified.
        /// </summary>
        /// 
        public abstract double LogScore { get; }

        /// <summary>
        /// True if this candidate links two different source points
        /// to the same target point.
        /// </summary>
        /// 
        public abstract bool IsConflicted { get; }

        /// <summary>
        /// The target position of the left-most source point
        /// for this candidate,
        /// or null if no targets are linked by this candidate.
        /// </summary>
        /// 
        public abstract int? FirstTargetPosition { get; }

        /// <summary>
        /// The target position of the right-most source point
        /// for this candidate,
        /// or null if no targets are linked by this candidate.
        /// </summary>
        /// 
        public abstract int? LastTargetPosition { get; }

        /// <summary>
        /// The sum of the absolute values of the deltas in target
        /// point positions for this candidate, when considered in
        /// source-point tree order and omitting source points that
        /// are linked to nothing.
        /// </summary>
        /// 
        public abstract int TotalMotion { get; }

        /// <summary>
        /// The number of the non-zero deltas in target
        /// point positions for this candidate, when considered in
        /// source-point tree order and omitting source points that
        /// are linked to nothing.
        /// </summary>
        /// 
        public abstract int NumberMotions { get; }

        /// <summary>
        /// The number of the negative deltas in target
        /// point positions for this candidate, when considered in
        /// source-point tree order and omitting source points that
        /// are linked to nothing.
        /// </summary>
        /// 
        public abstract int NumberBackwardMotions { get; }

        /// <summary>
        /// Representation of the set of target points that are
        /// linked to by this candidate.
        /// </summary>
        /// 
        public abstract TargetRange TargetRange { get; }


        /// <summary>
        /// Get the list of target points associated with this candidate,
        /// including nulls as implied by empty-point sub-candidates, in
        /// source-point syntax-tree order.
        /// </summary>
        /// 
        public List<TargetPoint> GetTargetPoints()
        {
            IEnumerable<TargetPoint> f(Candidate cand)
            {
                if (cand.IsPoint) yield return cand.TargetPoint;
                else if (cand.IsAdjusted)
                    foreach (var p in f(cand.Underlying)) yield return p;
                else if (cand.IsUnion)
                {
                    foreach (var p in f(cand.Head)) yield return p;
                    foreach (var p in f(cand.Tail)) yield return p;
                }               
            }

            return f(this).ToList();
        }


        /// <summary>
        /// Get the correspondence between source points and target points
        /// represented by this candidate, in the form of an enumeration of
        /// source-target pairs in source-point syntax-tree order, and with
        /// null target points for those source points that are not linked
        /// to anything.  Each link is accompanied by its log score.
        /// </summary>
        /// 
        public IEnumerable<(SourcePoint, TargetPoint, double)> GetCorrespondence()
        {
            if (IsPoint) yield return (SourcePoint, TargetPoint, LogScore);
            else if (IsAdjusted)
                foreach (var x in Underlying.GetCorrespondence())
                    yield return x;
            else if (IsUnion)
            {
                foreach (var x in Head.GetCorrespondence()) yield return x;
                foreach (var x in Tail.GetCorrespondence()) yield return x;
            }
        }
    }


    /// <summary>
    /// Implements Candidate for a point candidate that links
    /// one source point to one target point.
    /// </summary>
    /// 
    public class PointCandidate : Candidate
    {
        public PointCandidate(
            SourcePoint sourcePoint,
            TargetPoint targetPoint,
            double logScore)
        {
            _sourcePoint = sourcePoint;
            _targetPoint = targetPoint;
            _logScore = logScore;
        }

        private SourcePoint _sourcePoint;
        private TargetPoint _targetPoint;
        private double _logScore;

        public override CandidateKind Kind => CandidateKind.Point;

        public override bool IsPoint => true;

        public override bool IsUnion => false;

        public override bool IsAdjusted => false;

        public override SourcePoint SourcePoint => _sourcePoint;

        public override TargetPoint TargetPoint => _targetPoint;

        public override Candidate Head => null;

        public override Candidate Tail => null;

        public override Candidate Underlying => null;

        public override double LogScore => _logScore;

        public override bool IsConflicted => false;

        public override int? FirstTargetPosition => _targetPoint.Position;

        public override int? LastTargetPosition => _targetPoint.Position;

        public override int TotalMotion => 0;

        public override int NumberMotions => 0;

        public override int NumberBackwardMotions => 0;

        public override TargetRange TargetRange => new TargetRange(_targetPoint.Position);
    }


    /// <summary>
    /// Implements Candidate for an empty point candidate that
    /// links one source point to nothing.
    /// </summary>
    /// 
    public class EmptyPointCandidate : Candidate
    {
        public EmptyPointCandidate(
            SourcePoint sourcePoint)
        {
            _sourcePoint = sourcePoint;
        }

        public override CandidateKind Kind => CandidateKind.EmptyPoint;

        private SourcePoint _sourcePoint;

        public override bool IsPoint => true;

        public override bool IsUnion => false;

        public override bool IsAdjusted => false;

        public override SourcePoint SourcePoint => _sourcePoint;

        public override TargetPoint TargetPoint => null;

        public override Candidate Head => null;

        public override Candidate Tail => null;

        public override Candidate Underlying => null;

        public override double LogScore => 0.0;

        public override bool IsConflicted => false;

        public override int? FirstTargetPosition => null;

        public override int? LastTargetPosition => null;

        public override int TotalMotion => 0;

        public override int NumberMotions => 0;

        public override int NumberBackwardMotions => 0;

        public override TargetRange TargetRange => new TargetRange();
    }


    /// <summary>
    /// Implements Candidate for a union candidate that
    /// combines two sub-candidates.
    /// </summary>
    /// 
    public class UnionCandidate : Candidate
    {
        public UnionCandidate(Candidate head, Candidate tail)
        {
            _head = head;
            _tail = tail;
            _logScore = head.LogScore + tail.LogScore;

            int delta =
                (tail.FirstTargetPosition - head.LastTargetPosition)
                ?? 0;

            int deltaTotalMotion = Math.Abs(delta);
            int deltaNumberMotions = delta != 0 ? 1 : 0;
            int deltaNumberBackwardMotions = delta < 0 ? 1 : 0;

            _firstTargetPosition =
                head.FirstTargetPosition ??
                tail.FirstTargetPosition;
            _lastTargetPosition =
                tail.LastTargetPosition ??
                head.LastTargetPosition;
            _totalMotion =
                head.TotalMotion +
                tail.TotalMotion +
                deltaTotalMotion;
            _numberMotions =
                head.NumberMotions +
                tail.NumberMotions +
                deltaNumberMotions;
            _numberBackwardMotions =
                head.NumberBackwardMotions +
                tail.NumberBackwardMotions +
                deltaNumberBackwardMotions;

            (TargetRange range, bool subRangesInConflict) =
                head.TargetRange.Combine(tail.TargetRange);

            _range = range;
            _conflicted =
                subRangesInConflict ||
                head.IsConflicted ||
                tail.IsConflicted;
        }

        private Candidate _head;
        private Candidate _tail;
        private double _logScore;
        private int? _firstTargetPosition;
        private int? _lastTargetPosition;
        private int _totalMotion;
        private int _numberMotions;
        private int _numberBackwardMotions;
        private TargetRange _range;
        private bool _conflicted;


        public override CandidateKind Kind => CandidateKind.Union;

        public override bool IsPoint => false;

        public override bool IsUnion => true;

        public override bool IsAdjusted => false;

        public override SourcePoint SourcePoint => null;

        public override TargetPoint TargetPoint => null;

        public override Candidate Head => _head;

        public override Candidate Tail => _tail;

        public override Candidate Underlying => null;

        public override double LogScore => _logScore;

        public override bool IsConflicted => _conflicted;

        public override int? FirstTargetPosition => _firstTargetPosition;

        public override int? LastTargetPosition => _lastTargetPosition;

        public override int TotalMotion => _totalMotion;

        public override int NumberMotions => _numberMotions;

        public override int NumberBackwardMotions => _numberBackwardMotions;

        public override TargetRange TargetRange => _range;
    }


    /// <summary>
    /// Implements Candidate for an adjusted-score candidate that
    /// is an underlying candidate with an adjusted score.
    /// </summary>
    /// 
    public class AdjustedScoreCandidate : Candidate
    {
        public AdjustedScoreCandidate(Candidate basis, double logScore)
        {
            _basis = basis;
            _logScore = logScore;
        }

        Candidate _basis;
        double _logScore;


        public override CandidateKind Kind => CandidateKind.Adjusted;

        public override bool IsPoint => false;

        public override bool IsUnion => false;

        public override bool IsAdjusted => true;

        public override SourcePoint SourcePoint => null;

        public override TargetPoint TargetPoint => null;

        public override Candidate Head => null;

        public override Candidate Tail => null;

        public override Candidate Underlying => _basis;

        public override double LogScore => _logScore;

        public override bool IsConflicted => _basis.IsConflicted;

        public override int? FirstTargetPosition => _basis.FirstTargetPosition;

        public override int? LastTargetPosition => _basis.LastTargetPosition;

        public override int TotalMotion => _basis.TotalMotion;

        public override int NumberMotions => _basis.NumberMotions;

        public override int NumberBackwardMotions => _basis.NumberBackwardMotions;

        public override TargetRange TargetRange => _basis.TargetRange;
    }





    // Debugging
    //----------


    /// <summary>
    /// Some facilities to help in debugging candidates and the algorithms
    /// that use them.
    /// </summary>
    /// 
    public class TempCandidateDebug
    {
        private static Dictionary<Candidate, Candidate_Old> candToOld = new();

        private static Dictionary<Candidate_Old, Candidate> oldToCand = new();

        public static void Put(Candidate cand, Candidate_Old old)
        {
            candToOld[cand] = old;
            oldToCand[old] = cand;
        }

        public static Candidate_Old OldForCand(Candidate cand) => candToOld[cand];

        public static Candidate CandForOld(Candidate_Old old) => oldToCand[old];

        public static bool CandidateTablesMatch(
            AlternativesForTerminals oldTable,
            Dictionary<SourceID, List<Candidate>> newTable)
        {
            return
                oldTable
                .SelectMany(kvp => kvp.Value.Select(val => (kvp.Key, val)))
                .ToHashSet()
                .SetEquals(
                    newTable
                    .SelectMany(kvp => kvp.Value.Select(val =>
                        (kvp.Key.AsCanonicalString, TempCandidateDebug.OldForCand(val))))
                    .ToHashSet());
        }

        public static List<CandidateReport1Line> Report1(Candidate cand)
        {
            Dictionary<Candidate, int> ids = new();
            int nextID = 1;
            int idFor(Candidate cand)
            {
                if (ids.TryGetValue(cand, out int id)) return id;
                id = nextID++;
                ids[cand] = id;
                return id;
            }

            IEnumerable<CandidateReport1Line> f(Candidate cand)
            {
                yield return new CandidateReport1Line(cand, idFor);
                if (cand.IsUnion)
                {
                    foreach (var line in f(cand.Head)) yield return line;
                    foreach (var line in f(cand.Tail)) yield return line;
                }
                else if (cand.IsAdjusted)
                {
                    foreach (var line in f(cand.Underlying)) yield return line;
                }
            }

            return f(cand).ToList();
        }
    }


    /// <summary>
    /// Part of a report about candidates that can be generated to
    /// assist in debugging.
    /// </summary>
    /// 
    public class CandidateReport1Line
    {
        public int ID;
        public string Description;
        public CandidateInfo Info;

        public CandidateReport1Line(
            Candidate subject,
            Func<Candidate, int> getID)
        {
            ID = getID(subject);
            Info = new CandidateInfo(subject);
            switch (subject.Kind)
            {
                case CandidateKind.Point:
                    Description = string.Format(
                        "point({0:D3},{1:D3}) ",
                        subject.SourcePoint.TreePosition,
                        subject.TargetPoint.Position);
                    break;

                case CandidateKind.EmptyPoint:
                    Description = string.Format(
                        "emptyPoint({0:D3})",
                        subject.SourcePoint.TreePosition);
                    break;

                case CandidateKind.Union:
                    Description = string.Format(
                        "union({0:D3},{1:D3}) ",
                        getID(subject.Head),
                        getID(subject.Tail));
                    break;

                case CandidateKind.Adjusted:
                    Description = string.Format(
                        "adjusted({0:D3})  ",
                        getID(subject.Underlying));
                    break;
            }
        }

        public override string ToString()
        {
            return $"[{ID:D3} {Description} {Info}]";
        }
    }


    /// <summary>
    /// Information collected about a candidate, perhaps in the
    /// course of creating a report for debugging.
    /// </summary>
    /// 
    public class CandidateInfo
    {
        public bool Conflicted;
        public int? FirstTargetPosition;
        public int? LastTargetPosition;
        public int TotalMotion;
        public int NumberMotions;
        public int NumberBackwardMotions;
        public double LogScore;
        public List<int> TargetPositions;

        public CandidateInfo(Candidate cand)
        {
            Conflicted = cand.IsConflicted;
            FirstTargetPosition = cand.FirstTargetPosition;
            LastTargetPosition = cand.LastTargetPosition;
            TotalMotion = cand.TotalMotion;
            NumberMotions = cand.NumberMotions;
            NumberBackwardMotions = cand.NumberBackwardMotions;
            LogScore = cand.LogScore;
            TargetPositions = cand.TargetRange.Positions();
        }

        public override string ToString()
        {
            return string.Format(
                "[{0:D3} {1:D3} {2:D3} {3:D3} {4:D3} {5:E6} {6} ({7})]",
                FirstTargetPosition,
                LastTargetPosition,
                TotalMotion,
                NumberMotions,
                NumberBackwardMotions,
                LogScore,
                Conflicted ? "X" : " ",
                string.Join(" ", TargetPositions));
        }
    }
}
