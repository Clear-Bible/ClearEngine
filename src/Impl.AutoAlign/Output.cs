﻿using System;
using System.Collections.Generic;
using System.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.TreeService;

    public class Output
    {
        public static Line WriteAlignment(
            List<MultiLink> multiLinks,
            List<SourcePoint> sourcePoints,
            List<TargetPoint> targetPoints,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, int> primaryPositions)
        {
            return new Line()
            {
                manuscript = new Manuscript()
                {
                    words =
                        sourcePoints
                        .Select(sp =>
                        {
                            string ID = sp.SourceID.AsCanonicalString;
                            Gloss gloss = glossTable[ID];

                            return new ManuscriptWord()
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

                translation = new Translation()
                {
                    words =
                        targetPoints
                        .Select(tp => new TranslationWord()
                        {
                            id = long.Parse(tp.TargetID.AsCanonicalString),
                            altId = tp.AltID,
                            text = tp.Text
                        })
                        .ToArray()
                },

                links =
                    multiLinks
                    .Select(multiLink => new Link()
                    {
                        source =
                            multiLink.Sources
                            .Select(sourcePoint => sourcePoint.Position)
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
                            : Math.Exp(multiLink.Targets.ElementAt(0).Score)
                    })
                    .ToList()
            };

            bool isNotOneToOne(MultiLink ml) =>
                ml.Sources.Count() > 1 ||
                ml.Targets.Count() > 1;
        }





        static IEnumerable<TargetBond> WithPrimaryWordFirst(
            IEnumerable<TargetBond> targets,
            Dictionary<string, int> primaryPositions)
        {
            if (targets.Count() <= 1) return targets;

            string groupKey =
                string.Join(
                    " ",
                    targets.Select(t => t.TargetPoint.Lower))
                .Trim();

            TargetBond primaryWord =
                targets.ElementAt(primaryPositions[groupKey]);

            return
                Enumerable.Empty<TargetBond>()
                .Append(primaryWord)
                .Concat(
                    targets
                    .Where(t => t != primaryWord))
                .ToList();
        }
    }

}
