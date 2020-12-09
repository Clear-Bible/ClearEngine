using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.AutoAlign
{
    public static class CandidateDbExtensions
    {
        /// <summary>
        /// Create a new terminal candidate.
        /// </summary>
        /// 
        public static CandidateKey NewTerminal(
            this CandidateDb candidateDb,
            SourcePoint sourcePoint,
            TargetPoint targetPoint,
            double logScore)
        {
            CandidateKey key = new CandidateKey();
            
            int position = targetPoint.Position;

            // Prepare the range as a bit array with just the bit
            // set for the position of the target point.
            BitArray range = new BitArray(candidateDb.NumberTargetPoints);
            range.Set(position, true);

            // Add a terminal candidate record to the database.
            candidateDb.Terminals[key] = new TerminalCandidateRecord(
                sourcePoint, targetPoint);

            // Add an aux record to the database.
            // The candidate has no motions, because there is just
            // one target point.
            CandidateAuxInfoRecord aux = new CandidateAuxInfoRecord()
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
        /// Create a new terminal candidate that is empty, because it
        /// links a source point to no target point.
        /// </summary>
        /// 
        public static CandidateKey NewEmptyTerminal(
            this CandidateDb candidateDb,
            SourcePoint sourcePoint)
        {
            CandidateKey key = new CandidateKey();

            // Add a terminal candidate record to the database.
            candidateDb.Terminals[key] = new TerminalCandidateRecord(
                sourcePoint, null);

            // Do not add an aux record, because this candidate
            // does not really have a target point.

            return key;
        }


        public static List<TargetPoint> GetTargetPoints(
            this CandidateDb candidateDb,
            CandidateKey candidateKey)
        {
            IEnumerable<TargetPoint> f(CandidateKey key)
            {
                if (candidateDb.Terminals.TryGetValue(key,
                    out TerminalCandidateRecord terminal))
                    yield return terminal.TargetPoint;
                else
                {
                    NonTerminalCandidateRecord nonterminal =
                        candidateDb.NonTerminals[key];
                    foreach (CandidateKey subkey in nonterminal.SubCandidates)
                        foreach (TargetPoint tp in f(subkey))
                            yield return tp;
                }
            }

            return f(candidateKey).ToList();
        }
    }
}
