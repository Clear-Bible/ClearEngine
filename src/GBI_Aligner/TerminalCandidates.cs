using System;
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
            Dictionary<string, List<Candidate>> candidateTable,
            XmlNode treeNode, // syntax tree for current verse
            List<TargetWord> tWords,
            Hashtable model, // translation model, Hashtable(source => Hashtable(target => probability))
            Hashtable manModel, // manually checked alignments
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
            Dictionary<string, string> existingLinks, // mWord.altId => tWord.altId
            Dictionary<string, string> idMap, // (SourceWord.ID => SourceWord.AltID)
            ArrayList sourceFuncWords,
            bool contentWordsOnly,  // not actually used
            Hashtable strongs
            )
        {
            List<XmlNode> terminalNodes = Terminals.GetTerminalXmlNodes(treeNode);

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

                List<Candidate> topCandidates =
                    Align.GetTopCandidates(sWord, tWords, model, manModel, alignProbs, useAlignModel, n, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, existingLinks, sourceFuncWords, contentWordsOnly, strongs);

                candidateTable.Add(sWord.ID, topCandidates);

                ResolveConflicts(candidateTable);
            }

            FillGaps(candidateTable);
        }

        static void FillGaps(Dictionary<string, List<Candidate>> candidateTable)
        {
            List<string> gaps = FindGaps(candidateTable);

            foreach(string morphID in gaps)
            {
                List<Candidate> emptyCandidate = Align.CreateEmptyCandidate();
                candidateTable[morphID] = emptyCandidate;
            }
        }

        static List<string> FindGaps(Dictionary<string, List<Candidate>> candidateTable)
        {
            List<string> gaps = new List<string>();

            IDictionaryEnumerator tableEnum = candidateTable.GetEnumerator();

            while (tableEnum.MoveNext())

            foreach (var keyValuePair in candidateTable)
            {
                string morphID = keyValuePair.Key;
                List<Candidate> candidates = keyValuePair.Value;

                if (candidates.Count == 0)
                {
                    gaps.Add(morphID);
                }
            }

            return gaps;
        }

        static void ResolveConflicts(Dictionary<string, List<Candidate>> candidateTable)
        {
            Dictionary<string, List<string>> conflicts = FindConflicts(candidateTable);

            if (conflicts.Count > 0)
            {
                IDictionaryEnumerator conflictEnum = conflicts.GetEnumerator();
                while (conflictEnum.MoveNext())

                foreach (var keyValuePair in conflicts)
                {
                    string target = keyValuePair.Key;
                    List<string> positions = keyValuePair.Value;                    
                    List<Candidate> conflictingCandidates = GetConflictingCandidates(target, positions, candidateTable);
                    Candidate winningCandidate = Align.GetWinningCandidate(conflictingCandidates);
                    if (winningCandidate != null)
                    {
                        RemoveLosingCandidates(target, positions, winningCandidate, candidateTable);
                    }
                }
            }
        }

        static void RemoveLosingCandidates(
            string target,
            List<string> positions,
            Candidate winningCandidate,
            Dictionary<string, List<Candidate>> candidateTable)
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
            string result = string.Empty;

            c.Sequence.Match(
                targetWords =>
                {
                    if (targetWords.Count > 0)
                    { 
                        TargetWord tWord = targetWords[0];
                        result = tWord.ID;
                    }
                },
                candidates =>
                {
                    throw new InvalidOperationException("DEFECT");
                });

            return result;            
        }

        static List<Candidate> GetConflictingCandidates(
            string target,
            List<string> positions,
            Dictionary<string, List<Candidate>> candidateTable)
        {
            List<Candidate> conflictingCandidates = new List<Candidate>();

            foreach(string morphID in positions)
            {
                Candidate c = GetConflictingCandidate(morphID, target, candidateTable);
                conflictingCandidates.Add(c);
            }

            return conflictingCandidates;
        }

        static Candidate GetConflictingCandidate(
            string morphID,
            string target,
            Dictionary<string, List<Candidate>> candidateTable)
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

        // candidateTable = HashTable(SourceWord.Id => ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
        //
        // returns Hashtable(linkedWords => ArrayList(wordId))
        // where the ArrayLists all have more than one member
        //
        static Dictionary<string, List<string>> FindConflicts(Dictionary<string, List<Candidate>> candidateTable)
        {
            Dictionary<string, List<string>> targets =
                new Dictionary<string, List<string>>();

            foreach (var keyValuePair in candidateTable)
            {
                string morphID = keyValuePair.Key;
                List<Candidate> candidates = keyValuePair.Value;

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

            foreach (var keyValuePair in targets)
            {
                string target = keyValuePair.Key;
                List<string> positions = keyValuePair.Value;
                if (positions.Count > 1)
                {
                    conflicts.Add(target, positions);
                }
            }

            return conflicts;
        }
    }
}
