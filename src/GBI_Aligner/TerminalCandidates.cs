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
            ref Hashtable candidateTable,  // the output goes here
                                           // HashTable(SourceWord.Id => ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
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
            Hashtable existingLinks, // Hashtable(mWord.altId => tWord.altId)
            Hashtable idMap, // HashTable(SourceWord.ID => SourceWord.AltID)
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

                ArrayList topCandidates = null;
                topCandidates = Align.GetTopCandidates(sWord, tWords, model, manModel, alignProbs, useAlignModel, n, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, existingLinks, sourceFuncWords, contentWordsOnly, strongs);
                    // ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double })

          /*      foreach (Candidate c in topCandidates)
                {
                    string linkedWords = Align.GetWords(c);
                } */

                candidateTable.Add(sWord.ID, topCandidates);

                ResolveConflicts(ref candidateTable);
            }

            FillGaps(ref candidateTable);
        }

        static void FillGaps(ref Hashtable candidateTable)
        {
            ArrayList gaps = FindGaps(candidateTable);

            foreach(string morphID in gaps)
            {
                ArrayList emptyCandidate = Align.CreateEmptyCandidate();
                candidateTable[morphID] = emptyCandidate;
            }
        }

        static ArrayList FindGaps(Hashtable candidateTable)
        {
            ArrayList gaps = new ArrayList();

            IDictionaryEnumerator tableEnum = candidateTable.GetEnumerator();

            while (tableEnum.MoveNext())
            {
                string morphID = (string)tableEnum.Key;
                ArrayList candidates = (ArrayList)tableEnum.Value;

                if (candidates.Count == 0)
                {
                    gaps.Add(morphID);
                }
            }

            return gaps;
        }

        static void ResolveConflicts(ref Hashtable candidateTable)
        {
            Hashtable conflicts = FindConflicts(candidateTable);

            if (conflicts.Count > 0)
            {
                IDictionaryEnumerator conflictEnum = conflicts.GetEnumerator();
                while (conflictEnum.MoveNext())
                {
                    string target = (string)conflictEnum.Key;
                    ArrayList positions = (ArrayList)conflictEnum.Value;                    
                    ArrayList conflictingCandidates = GetConflictingCandidates(target, positions, candidateTable);
                    Candidate winningCandidate = Align.GetWinningCandidate(conflictingCandidates);
                    if (winningCandidate != null)
                    {
                        RemoveLosingCandidates(target, positions, winningCandidate, ref candidateTable);
                    }
                }
            }
        }

        static void RemoveLosingCandidates(string target, ArrayList positions, Candidate winningCandidate, ref Hashtable candidateTable)
        {
            foreach(string morphID in positions)
            {
                ArrayList candidates = (ArrayList)candidateTable[morphID];
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
            if (c.Sequence.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                TargetWord tWord = (TargetWord)c.Sequence[0];
                return tWord.ID;
            }
        }

        static ArrayList GetConflictingCandidates(string target, ArrayList positions, Hashtable candidateTable)
        {
            ArrayList conflictingCandidates = new ArrayList();

            foreach(string morphID in positions)
            {
                Candidate c = GetConflictingCandidate(morphID, target, candidateTable);
                conflictingCandidates.Add(c);
            }

            return conflictingCandidates;
        }

        static Candidate GetConflictingCandidate(string morphID, string target, Hashtable candidateTable)
        {
            Candidate conflictingCandidate = null;

            ArrayList candidates = (ArrayList)candidateTable[morphID];
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
        static Hashtable FindConflicts(Hashtable candidateTable)
        {
            Hashtable targets = new Hashtable(); // Hashtable(linkedWords => ArrayList(wordId))

            IDictionaryEnumerator tableEnum = candidateTable.GetEnumerator();

            while (tableEnum.MoveNext())
            {
                string morphID = (string)tableEnum.Key;  // source word ID
                ArrayList candidates = (ArrayList)tableEnum.Value;
                    // candidates :: ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double })

                for (int i = 1; i < candidates.Count; i++) // excluding the top candidate
                {
                    Candidate c = (Candidate)candidates[i];
                    // c :: Candidate{ Sequence ArrayList(TargetWord), Prob double }

                    string linkedWords = Align.GetWords(c);
                    if (targets.ContainsKey(linkedWords))
                    {
                        ArrayList positions = (ArrayList)targets[linkedWords];
                        positions.Add(morphID);
                    }
                    else
                    {
                        ArrayList positions = new ArrayList();
                        positions.Add(morphID);
                        targets.Add(linkedWords, positions);
                    }
                }
            }

            Hashtable conflicts = new Hashtable();

            IDictionaryEnumerator targetEnum = targets.GetEnumerator();

            while (targetEnum.MoveNext())
            {
                string target = (string)targetEnum.Key;
                ArrayList positions = (ArrayList)targetEnum.Value;
                if (positions.Count > 1)
                {
                    conflicts.Add(target, positions);
                }
            }

            return conflicts;
        }
    }
}
