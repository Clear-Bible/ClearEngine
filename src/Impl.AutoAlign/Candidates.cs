using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.AutoAlign
{
    public class TempCandidateDebug
    {
        private static Dictionary<CandidateKey, Candidate_Old> keyToOld = new();

        private static Dictionary<Candidate_Old, CandidateKey> oldToKey = new();

        public static void Put(CandidateKey key, Candidate_Old old)
        {
            keyToOld[key] = old;
            oldToKey[old] = key;
        }

        public static Candidate_Old OldForKey(CandidateKey key) => keyToOld[key];

        public static CandidateKey KeyForOld(Candidate_Old old) => oldToKey[old];

        public static bool CandidateTablesMatch(
            AlternativesForTerminals oldTable,
            Dictionary<SourceID, List<CandidateKey>> newTable)
        {
            return
                oldTable
                .SelectMany(kvp => kvp.Value.Select(val => (kvp.Key, val)))
                .ToHashSet()
                .SetEquals(
                    newTable
                    .SelectMany(kvp => kvp.Value.Select(val =>
                        (kvp.Key.AsCanonicalString, TempCandidateDebug.OldForKey(val))))
                    .ToHashSet());
        }

        public static List<CandidateReport1Line> Report1(CandidateKey key)
        {
            Dictionary<CandidateKey, int> ids = new();
            int nextID = 1;
            int idFor(CandidateKey key)
            {
                if (ids.TryGetValue(key, out int id)) return id;
                id = nextID++;
                ids[key] = id;
                return id;
            }

            IEnumerable<CandidateReport1Line> f(CandidateKey key)
            {
                if (key.TryGetTerminal(out TerminalCandidateRecord rec))
                    yield return new CandidateReport1Line(
                        idFor(key), null, null,
                        rec.TargetPoint?.Position, key.AuxInfo);
                else
                {
                    NonTerminalCandidateRecord recnt =
                        key.NonTerminalCandidateRecord;
                    yield return new CandidateReport1Line(
                        idFor(key), idFor(recnt.Head), idFor(recnt.Tail),
                        null, key.AuxInfo);
                    foreach (var line in f(recnt.Head)) yield return line;
                    foreach (var line in f(recnt.Tail)) yield return line;
                }
            }

            return f(key).ToList();
        }
    }

    public record CandidateReport1Line(
        int ID,
        int? head,
        int? tail,
        int? targetPosition,
        CandidateAuxInfoRecord aux);

    



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
}
