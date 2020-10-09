﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Collections;

using Utilities;
using Trees;

namespace GBI_Aligner
{
    class TerminalCandidates
    {
        public static void GetTerminalCandidates(
            AlternativesForTerminals candidateTable,  // the output goes here
            XmlNode treeNode, // syntax tree for current verse
            List<TargetWord> tWords, // ArrayList(TargetWord)
            Dictionary<string, Dictionary<string, double>> model,
            Dictionary<string, Dictionary<string, Stats>> manModel, // manually checked alignments
                                // Hashtable(source => Hashtable(target => Stats{ count, probability})
            Hashtable alignProbs, // Hashtable("bbcccvvvwwwn-bbcccvvvwww" => probability)
            bool useAlignModel,
            int n,  // number of target tokens
            string verseID, // from the syntax tree
            ArrayList puncs, 
            ArrayList stopWords, 
            Hashtable goodLinks,  // Hashtable(link => count)
            int goodLinkMinCount,
            Hashtable badLinks,  // Hashtable(link => count)
            int badLinkMinCount,
            Hashtable existingLinks, // Hashtable(mWord.altId => tWord.altId)
            Dictionary<string, string> idMap, 
            ArrayList sourceFuncWords,
            bool contentWordsOnly,  // not actually used
            Hashtable strongs
            )
        {
            ArrayList terminalNodes = Terminals.GetTerminalXmlNodes(treeNode);
                // ArrayList(XmlNode)

            foreach(XmlNode terminalNode in terminalNodes)
            {
                SourceWord sWord = new SourceWord();
                sWord.ID = Utils.GetAttribValue(terminalNode, "morphId");
                if (sWord.ID == "41002004013")
                {
                    ;
                }
                sWord.Category = Utils.GetAttribValue(terminalNode, "Cat");
                if (sWord.ID.Length == 11)
                {
                    sWord.ID += "1";
                }
                sWord.AltID = (string)idMap[sWord.ID];
                //               sWord.Lemma = (string)lemmaTable[sWord.ID];
                sWord.Text = Utils.GetAttribValue(terminalNode, "Unicode");
                sWord.Lemma = Utils.GetAttribValue(terminalNode, "UnicodeLemma");
                sWord.Strong = Utils.GetAttribValue(terminalNode, "Language") + Utils.GetAttribValue(terminalNode, "StrongNumberX");
                sWord.Morph = Utils.GetAttribValue(terminalNode, "Analysis");
                if (sWord.Lemma == null) continue;
 //               if (contentWordsOnly && sourceFuncWords.Contains(sWord.Lemma)) continue;

                AlternativeCandidates topCandidates =
                    Align.GetTopCandidates(sWord, new ArrayList(tWords), model, manModel,
                        alignProbs, useAlignModel, n, puncs, stopWords,
                        goodLinks, goodLinkMinCount, badLinks, badLinkMinCount,
                        existingLinks, sourceFuncWords, contentWordsOnly,
                        strongs);

                candidateTable.Add(sWord.ID, topCandidates);

                ResolveConflicts(candidateTable);
            }

            FillGaps(candidateTable);
        }

        static void FillGaps(AlternativesForTerminals candidateTable)
        {
            ArrayList gaps = FindGaps(candidateTable);

            foreach(string morphID in gaps)
            {
                List<Candidate> emptyCandidate = Align.CreateEmptyCandidate();
                candidateTable[morphID] = emptyCandidate;
            }
        }

        static ArrayList FindGaps(AlternativesForTerminals candidateTable)
        {
            ArrayList gaps = new ArrayList();

            foreach (var tableEnum in candidateTable)
            {
                string morphID = tableEnum.Key;
                List<Candidate> candidates = tableEnum.Value;

                if (candidates.Count == 0)
                {
                    gaps.Add(morphID);
                }
            }

            return gaps;
        }

        static void ResolveConflicts(AlternativesForTerminals candidateTable)
        {
            Dictionary<string, List<string>> conflicts = FindConflicts(candidateTable);

            if (conflicts.Count > 0)
            {
                foreach (var conflictEnum in conflicts)
                {
                    string target = conflictEnum.Key;
                    List<string> positions = conflictEnum.Value;                    
                    ArrayList conflictingCandidates = GetConflictingCandidates(target, positions, candidateTable);
                    Candidate winningCandidate = Align.GetWinningCandidate(conflictingCandidates);
                    if (winningCandidate != null)
                    {
                        RemoveLosingCandidates(target, positions, winningCandidate, candidateTable);
                    }
                }
            }
        }

        static void RemoveLosingCandidates(string target, List<string> positions, Candidate winningCandidate, AlternativesForTerminals candidateTable)
        {
            foreach(string morphID in positions)
            {
                List<Candidate> candidates = candidateTable[morphID];
                for (int i = 0; i < candidates.Count; i++)
                {
                    Candidate c = (Candidate)candidates[i];
                    string targetID = GetTargetID(c);
                    if (targetID == string.Empty) continue;
                    string linkedWords = Align.GetWords(c);
                    if (linkedWords == target && c != winningCandidate && c.Prob < 0.0)
                    {
                        candidates.Remove(c);
                    }
                }
            }
        }

        static string GetTargetID(Candidate c)
        {
            if (c.Chain.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                TargetWord tWord = (TargetWord)c.Chain[0];
                return tWord.ID;
            }
        }

        static ArrayList GetConflictingCandidates(string target, List<string> positions, AlternativesForTerminals candidateTable)
        {
            ArrayList conflictingCandidates = new ArrayList();

            foreach(string morphID in positions)
            {
                Candidate c = GetConflictingCandidate(morphID, target, candidateTable);
                conflictingCandidates.Add(c);
            }

            return conflictingCandidates;
        }

        static Candidate GetConflictingCandidate(string morphID, string target, AlternativesForTerminals candidateTable)
        {
            Candidate conflictingCandidate = null;

            List<Candidate> candidates = candidateTable[morphID];
            foreach(Candidate candidate in candidates)
            {
                string linkedWords = Align.GetWords(candidate);
                if (linkedWords == target)
                {
                    conflictingCandidate = candidate;
                    break;
                }
            }

            return conflictingCandidate;
        }

 
        static Dictionary<string, List<string>> FindConflicts(AlternativesForTerminals candidateTable)
        {
            Dictionary<string, List<string>> targets =
                new Dictionary<string, List<string>>();

            foreach (var tableEnum in candidateTable)
            {
                string morphID = tableEnum.Key;
                List<Candidate> candidates = tableEnum.Value;

                for (int i = 1; i < candidates.Count; i++) // excluding the top candidate
                {
                    Candidate c = candidates[i];

                    string linkedWords = Align.GetWords(c);
                    if (targets.ContainsKey(linkedWords))
                    {
                        List<string> positions = targets[linkedWords];
                        positions.Add(morphID);
                    }
                    else
                    {
                        List<string> positions = new List<string>();
                        positions.Add(morphID);
                        targets.Add(linkedWords, positions);
                    }
                }
            }

            Dictionary<string, List<string>> conflicts =
                new Dictionary<string, List<string>>();

            foreach (var targetEnum in targets)
            {
                string target = targetEnum.Key;
                List<string> positions = targetEnum.Value;
                if (positions.Count > 1)
                {
                    conflicts.Add(target, positions);
                }
            }

            return conflicts;
        }
    }
}
