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
                        "point({0,3},{1,3}) ",
                        subject.SourcePoint.TreePosition,
                        subject.TargetPoint.Position);
                    break;

                case CandidateKind.EmptyPoint:
                    Description = string.Format(
                        "emptyPoint({0,3})",
                        subject.SourcePoint.TreePosition);
                    break;

                case CandidateKind.Union:
                    Description = string.Format(
                        "union({0,3},{1,3}) ",
                        getID(subject.Head),
                        getID(subject.Tail));
                    break;

                case CandidateKind.Adjusted:
                    Description = string.Format(
                        "adjusted({0,3})  ",
                        getID(subject.Underlying));
                    break;
            }
        }

        public override string ToString()
        {
            return $"[{ID,3} {Description} {Info}]";
        }
    }

    

    



    /// <summary>
    /// The CandidateKey stands for a candidate
    /// which applies to some subset of the source points, and links
    /// each of them to a target point or to nothing.
    /// </summary>
    /// 
    public class CandidateKey
    {
        private CandidateDb _candidateDb;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="candidateDb">
        /// The candidate database that gives this candidate its meaning.
        /// </param>
        /// 
        public CandidateKey(CandidateDb candidateDb)
        {
            _candidateDb = candidateDb;
        }

        public double LogScore => AuxInfo.LogScore;

        public CandidateAuxInfoRecord AuxInfo =>
            _candidateDb.AuxInfo[this];

        public bool TryGetTerminal(out TerminalCandidateRecord record) =>
            _candidateDb.Terminals.TryGetValue(this, out record);

        public TerminalCandidateRecord TerminalCandidateRecord =>
            _candidateDb.Terminals[this];

        public bool TryGetNonTerminal(out NonTerminalCandidateRecord record) =>
            _candidateDb.NonTerminals.TryGetValue(this, out record);

        public NonTerminalCandidateRecord NonTerminalCandidateRecord =>
            _candidateDb.NonTerminals[this];


        /// <summary>
        /// Get list of target points for this candidate, including
        /// nulls for those source points that do not have target points.
        /// </summary>
        /// 
        public List<TargetPoint> GetTargetPoints()
        {
            // Helper function to recursively enumerate the sub-candidate
            // tree structure.
            //
            IEnumerable<TargetPoint> f(CandidateKey key)
            {
                if (_candidateDb.Terminals.TryGetValue(key,
                    out TerminalCandidateRecord terminal))
                    yield return terminal.TargetPoint;
                else
                {
                    NonTerminalCandidateRecord nt =
                        _candidateDb.NonTerminals[key];
                    foreach (TargetPoint tp in f(nt.Head)) yield return tp;
                    foreach (TargetPoint tp in f(nt.Tail)) yield return tp;
                }
            }

            return f(this).ToList();
        }


        public CandidateKey Cons(CandidateKey tail)
        {
            CandidateKey cons = new CandidateKey(_candidateDb);

            _candidateDb.NonTerminals[cons] = new NonTerminalCandidateRecord(
                Head: this,
                Tail: tail);

            _candidateDb.AuxInfo[cons] = _candidateDb.CombineAuxInfo(
                _candidateDb.AuxInfo.GetValueOrDefault(this),
                _candidateDb.AuxInfo.GetValueOrDefault(tail));

            return cons;
        }
    }


    /// <summary>
    /// A terminal candidate links a particular SourcePoint
    /// to a particular TargetPoint, or to no TargetPoint (in which
    /// case the TargetPoint property is null.
    /// </summary>
    /// 
    public record TerminalCandidateRecord(
        SourcePoint SourcePoint,
        TargetPoint TargetPoint);


    /// <summary>
    /// A non-terminal candidate is a sequence of sub-candidates.
    /// </summary>
    /// 
    public record NonTerminalCandidateRecord(
        CandidateKey Head,
        CandidateKey Tail);


    /// <summary>
    /// Auxiliary information about a candidate.
    /// </summary>
    /// 
    public class CandidateAuxInfoRecord
    {
        // The set of target point positions that are linked to,
        // expressed as a bit array.
        public BitArray Range { get; set; }

        public int? FirstTargetPosition { get; set; }
        public int? LastTargetPosition { get; set; }

        // The sum of absolute values of deltas in position, but
        // considering only the source points that are linked to target
        // points instead of nothing.
        public int TotalMotion { get; set; }

        // The number of deltas that are non-zero.
        public int NumberMotions { get; set; }

        // The number of deltas that are negative.
        public int NumberBackwardMotions { get; set; }

        // The logarithm of a probability-like score for
        // this candidate.
        public double LogScore { get; set; }
    }

    public class CandidateInfo
    {
        public int? FirstTargetPosition;
        public int? LastTargetPosition;
        public int TotalMotion;
        public int NumberMotions;
        public int NumberBackwardMotions;
        public double LogScore;
        public List<int> TargetPositions;

        public CandidateInfo(Candidate cand)
        {
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
                "[{0:3} {1:3} {2:3} {3:3} {4:3} {5:6} ({6})]",
                FirstTargetPosition,
                LastTargetPosition,
                TotalMotion,
                NumberMotions,
                NumberBackwardMotions,
                LogScore,
                string.Join(" ", TargetPositions));
        }
    }


    /// <summary>
    /// The CandidateDb expresses the meaning of a CandidateKey.
    /// Each candidate key appears in either the Terminals or the
    /// NonTerminals table, and all candidate keys appear in the
    /// AuxInfo table.
    /// NumberTargetPoints is used for creating the Range
    /// bit array in a candidate's auxiliary info record.
    /// </summary>
    /// 
    public record CandidateDb(
        int NumberTargetPoints,
        Dictionary<CandidateKey, TerminalCandidateRecord> Terminals,
        Dictionary<CandidateKey, NonTerminalCandidateRecord> NonTerminals,
        Dictionary<CandidateKey, CandidateAuxInfoRecord> AuxInfo)
    {
        public static CandidateDb MakeEmpty(int numberTargetPoints) =>
            new CandidateDb(
                numberTargetPoints,
                new(),
                new(),
                new());

        /// <summary>
        /// Create a new terminal candidate that maps a source point
        /// to a non-null target point.
        /// </summary>
        /// 
        public CandidateKey NewTerminal(
            SourcePoint sourcePoint,
            TargetPoint targetPoint,
            double logScore)
        {
            CandidateKey key = new CandidateKey(this);

            int position = targetPoint.Position;

            // Prepare the range as a bit array with just the bit
            // set for the position of the target point.
            BitArray range = new BitArray(NumberTargetPoints);
            range.Set(position, true);

            // Add a terminal candidate record to the database.
            Terminals[key] = new TerminalCandidateRecord(
                sourcePoint, targetPoint);

            // Add an aux record to the database.
            // The candidate has no motions, because there is just
            // one target point.
            AuxInfo[key] = new CandidateAuxInfoRecord()
            {
                Range = range,
                FirstTargetPosition = position,
                LastTargetPosition = position,
                TotalMotion = 0,
                NumberMotions = 0,
                NumberBackwardMotions = 0,
                LogScore = logScore
            };

            return key;
        }


        /// <summary>
        /// Create a new terminal candidate that represents the
        /// certainty of not linking a source point at all.
        /// </summary>
        /// 
        public CandidateKey NewEmptyTerminal(
            SourcePoint sourcePoint)
        {
            CandidateKey key = new CandidateKey(this);

            // Add a terminal candidate record to the database.
            Terminals[key] = new TerminalCandidateRecord(
                sourcePoint, null);

            // Add an aux record to the database.
            // The candidate has no start and end positions, and no
            // motions, because there is no target point.
            // The log score is zero, because this candidate represents
            // a probability of 1 for no link at all for this source point.
            AuxInfo[key] = new CandidateAuxInfoRecord()
            {
                Range = new BitArray(NumberTargetPoints),
                FirstTargetPosition = null,
                LastTargetPosition = null,
                TotalMotion = 0,
                NumberMotions = 0,
                NumberBackwardMotions = 0,
                LogScore = 0.0
            };

            return key;
        }


        public CandidateAuxInfoRecord CombineAuxInfo(
            CandidateAuxInfoRecord head,
            CandidateAuxInfoRecord tail)
        {
            int delta =
                (tail.FirstTargetPosition - head.LastTargetPosition)
                ?? 0;

            int deltaTotalMotion = Math.Abs(delta);
            int deltaNumberMotions = delta != 0 ? 1 : 0;
            int deltaNumberBackwardMotions = delta < 0 ? 1 : 0;

            return new CandidateAuxInfoRecord()
            {
                Range = (new BitArray(head.Range)).Or(tail.Range),
                FirstTargetPosition =
                    head.FirstTargetPosition ??
                    tail.FirstTargetPosition,
                LastTargetPosition =
                    tail.LastTargetPosition ??
                    head.LastTargetPosition,
                TotalMotion =
                    head.TotalMotion +
                    tail.TotalMotion +
                    deltaTotalMotion,
                NumberMotions =
                    head.NumberMotions +
                    tail.NumberMotions +
                    deltaNumberMotions,
                NumberBackwardMotions =
                    head.NumberBackwardMotions +
                    tail.NumberBackwardMotions +
                    deltaNumberBackwardMotions,
                LogScore =
                    head.LogScore +
                    tail.LogScore
            };
        }
    }


    /// <summary>
    /// Represents a set of target point positions, and provides some
    /// limited operations for working with them.
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


    public enum CandidateKind
    {
        Point,
        EmptyPoint,
        Union,
        Adjusted
    };


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
    }


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

            (_range, _conflicted) = head.TargetRange.Combine(tail.TargetRange);
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
}
