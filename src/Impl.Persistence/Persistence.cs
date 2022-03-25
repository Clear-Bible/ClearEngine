using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace ClearBible.Clear3.Impl.Persistence
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.TreeService;

    /// <summary>
    /// (Implementation of IPersistence.)
    /// </summary>
    public class Persistence : IPersistence
    {
        /// <summary>
        /// (Implementation of IPersistence.GetLpaLine)
        /// 
        public LpaLine GetLpaLine(
            ZoneMultiAlignment zoneMultiAlignment,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, int> primaryPositions)
        {
            // Extract the component parts of the ZoneMultiAlignment.
            ((List<SourcePoint> sourcePoints, List<TargetPoint> targetPoints),
             List<MultiLink> multiLinks)
                = zoneMultiAlignment;

            return new LpaLine()
            {
                manuscript = new LpaManuscript()
                {
                    words =
                        sourcePoints
                        .Select(sp =>
                        {
                            string ID = sp.SourceID.AsCanonicalString;
                            Gloss gloss = glossTable[ID];

                            return new LpaManuscriptWord()
                            {
                                id = long.Parse(ID),
                                altId = sp.AltID,
                                text = sp.Terminal.Surface(),
                                lemma = sp.Terminal.Lemma(),
                                strong = sp.Terminal.Strong(),
                                pos = sp.Terminal.Category(),
                                morph = sp.Terminal.Analysis(),
                                gloss = gloss.Gloss1,
                                gloss2 = gloss.Gloss2
                            };
                        })
                        .ToArray()
                },

                translation = new LpaTranslation()
                {
                    words =
                        targetPoints
                        .Select(tp => new LpaTranslationWord()
                        {
                            id = long.Parse(tp.TargetID.AsCanonicalString),
                            altId = tp.AltID,
                            text = tp.Text

                        })
                        .ToArray()
                },

                links =
                    multiLinks
                    .Select(multiLink => new LpaLink()
                    {
                        source =
                            multiLink.Sources
                            .Select(sourcePoint => sourcePoint.SourcePosition)
                            .ToArray(),

                        target =
                            WithPrimaryWordFirst(
                                multiLink.Targets,
                                primaryPositions)
                            .Select(t => t.TargetPoint.Position)
                            .ToArray(),

                        cscore =
                            isNotOneToOne(multiLink)
                            ? 0.9
                            : Math.Exp(multiLink.Targets[0].Score)
                    })
                    .ToList()
            };

            bool isNotOneToOne(MultiLink ml) =>
                ml.Sources.Count > 1 ||
                ml.Targets.Count > 1;
        }

        /// <summary>
        /// (Implementation of IPersistence.GetLpaLemmaLine)
        ///
        /// 2022.03.24 CL: Added this method so we can have the lemma in the target word.
        /// Could modify this along with GetLpaLine() so that the common code is refactored.
        public LpaLemmaLine GetLpaLemmaLine(
            ZoneMultiAlignment zoneMultiAlignment,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, int> primaryPositions)
        {
            // Extract the component parts of the ZoneMultiAlignment.
            ((List<SourcePoint> sourcePoints, List<TargetPoint> targetPoints),
             List<MultiLink> multiLinks)
                = zoneMultiAlignment;

            return new LpaLemmaLine()
            {
                manuscript = new LpaManuscript()
                {
                    words =
                        sourcePoints
                        .Select(sp =>
                        {
                            string ID = sp.SourceID.AsCanonicalString;
                            Gloss gloss = glossTable[ID];

                            return new LpaManuscriptWord()
                            {
                                id = long.Parse(ID),
                                altId = sp.AltID,
                                text = sp.Terminal.Surface(),
                                lemma = sp.Terminal.Lemma(),
                                strong = sp.Terminal.Strong(),
                                pos = sp.Terminal.Category(),
                                morph = sp.Terminal.Analysis(),
                                gloss = gloss.Gloss1,
                                gloss2 = gloss.Gloss2
                            };
                        })
                        .ToArray()
                },

                translation = new LpaLemmaTranslation()
                {
                    words =
                        targetPoints
                        .Select(tp => new LpaLemmaTranslationWord()
                        {
                            id = long.Parse(tp.TargetID.AsCanonicalString),
                            altId = tp.AltID,
                            text = tp.Text,
                            lemma = tp.Lemma

                        })
                        .ToArray()
                },

                links =
                    multiLinks
                    .Select(multiLink => new LpaLink()
                    {
                        source =
                            multiLink.Sources
                            .Select(sourcePoint => sourcePoint.SourcePosition)
                            .ToArray(),

                        target =
                            WithPrimaryWordFirst(
                                multiLink.Targets,
                                primaryPositions)
                            .Select(t => t.TargetPoint.Position)
                            .ToArray(),

                        cscore =
                            isNotOneToOne(multiLink)
                            ? 0.9
                            : Math.Exp(multiLink.Targets[0].Score)
                    })
                    .ToList()
            };

            bool isNotOneToOne(MultiLink ml) =>
                ml.Sources.Count > 1 ||
                ml.Targets.Count > 1;
        }


        /// <summary>
        /// Reorder the list of TargetBond so that the primary word
        /// in a group occurs at the head of the list.
        /// </summary>
        /// 
        IEnumerable<TargetBond> WithPrimaryWordFirst(
                IReadOnlyList<TargetBond> targets,
                Dictionary<string, int> primaryPositions)
        {
            // If there are not multiple targets, then there is nothing
            // to do.
            if (targets.Count <= 1) return targets;

            // Construct the group key as a string of space separated
            // target words.
            string groupKey =
                string.Join(
                    " ",
                    targets.Select(t => t.TargetPoint.Lemma))
                .Trim();

            // If the group key occurs in the primary positions table:
            if (primaryPositions.TryGetValue(groupKey, out int n))
            {
                // Get the TargetBond associated with the primary position
                // for the group.
                TargetBond primaryWord = targets[n];

                // Reorder the TargetBond list to put the primary word at
                // the front.
                return
                    Enumerable.Empty<TargetBond>()
                    .Append(primaryWord)
                    .Concat(
                        targets
                        .Where(t => t != primaryWord))
                    .ToList();
            }

            // The group key does not occur in the primary positions
            // table.
            return targets;          
        }
    }
}
