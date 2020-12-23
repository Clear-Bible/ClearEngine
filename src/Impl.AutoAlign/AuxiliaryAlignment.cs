using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


using ClearBible.Clear3.API;
using ClearBible.Clear3.Impl.Miscellaneous;
using ClearBible.Clear3.Impl.TreeService;

namespace ClearBible.Clear3.Impl.AutoAlign
{
    public class AuxiliaryAlignment
    {
        /// <summary>
        /// Attempt to improve an alignment by adding links for source
        /// points that are not yet linked to anything.
        /// </summary>
        /// <param name="links">
        /// The alignment to be improved.
        /// </param>
        /// <param name="targetPoints">
        /// The target points of the zone.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain auto-alignment.
        /// </param>
        /// 
        public static void ImproveAlignment(
            List<OpenMonoLink> links,
            List<TargetPoint> targetPoints,
            IAutoAlignAssumptions assumptions)
        {
            // Get a list of target IDs (as canonical strings) for
            // the target points that are already linked.
            List<string> linkedTargets =
                links
                .Where(mw => mw.HasTargetPoint)
                .Select(mw => mw.OpenTargetBond.MaybeTargetPoint.ID)
                .ToList();

            // Make a table mapping source ID (as canonical string) to
            // its associated OpenMonoLink for those source points that
            // are already linked.
            Dictionary<string, OpenMonoLink> linksTable =
                links
                .Where(mw => mw.HasTargetPoint)
                .ToDictionary(
                    mw => mw.SourcePoint.SourceID.AsCanonicalString,
                    mw => mw);

            // For each link in the alignment where the source point
            // is unlinked:
            foreach (OpenMonoLink link in
                links.Where(link => link.OpenTargetBond.MaybeTargetPoint.IsNothing))
            {
                // Try to align the unlinked source point.
                OpenTargetBond linkedWord =
                    AlignUnlinkedSourcePoint(
                        link.SourcePoint,
                        targetPoints,
                        linksTable,
                        linkedTargets,
                        assumptions);

                // If the attempt was successful:
                if (linkedWord != null)
                {
                    // Reset the link to contain the OpenTargetBond that
                    // was found.
                    link.ResetOpenTargetBond(linkedWord);
                }
            }
        }


        /// <summary>
        /// Attempt to find a target point for a source point that is not
        /// yet linked.
        /// </summary>
        /// <param name="sourceNode">
        /// The source point for which a link is sought.
        /// </param>
        /// <param name="targetPoints">
        /// The target points in the zone.
        /// </param>
        /// <param name="linksTable">
        /// Table of source points that are already linked, consisting of
        /// a mapping from source ID (as a canonical string) to its
        /// OpenMonoLink.
        /// </param>
        /// <param name="linkedTargets">
        /// List of target ID (as a canonical string) for those target
        /// points that are already linked.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain the auto alignment.
        /// </param>
        /// <returns>
        /// A new OpenTargetBond containing the target point to be
        /// linked to the unlinked source point, or null if no suitable target
        /// point can be found.
        /// </returns>
        /// 
        public static OpenTargetBond AlignUnlinkedSourcePoint(
            SourcePoint sourceNode,
            List<TargetPoint> targetPoints,
            Dictionary<string, OpenMonoLink> linksTable,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions)
        {
            // If the source point is a stop word, then give up.
            if (assumptions.IsStopWord(sourceNode.Lemma)) return null;

            // If assuming content words only and the source point
            // is a function word, then give up.
            if (assumptions.ContentWordsOnly &&
                assumptions.IsSourceFunctionWord(sourceNode.Lemma))
            {
                return null;
            }

            // If assuming use of the estimated alignment model and
            // some alignment for the unlinked source point can be found:
            if (assumptions.UseAlignModel &&
                assumptions.TryGetPreAlignment(
                    sourceNode.SourceID.AsCanonicalString,
                    out string targetID))
            {
                // If the proposed target is already linked, then give up.
                if (linkedTargets.Contains(targetID)) return null;

                // Get the target point associated with the target ID
                // of the proposal.
                TargetPoint targetPoint =
                    targetPoints.First(
                        tp => tp.TargetID.AsCanonicalString == targetID);

                // If the source point is a stop word and the proposal
                // has not been declared as a good link, then give up.
                if (assumptions.IsStopWord(sourceNode.Lemma) &&
                    !assumptions.IsGoodLink(
                        sourceNode.Lemma,
                        targetPoint.Lower))
                {
                    return null;
                }

                // If the proposal has not been declared as a bad link
                // and the target point is neither punctuation nor stop word:
                if (!assumptions.IsBadLink(
                        sourceNode.Lemma,
                        targetPoint.Lower) &&
                    !assumptions.IsPunctuation(targetPoint.Lower) &&
                    !assumptions.IsStopWord(targetPoint.Lower))
                {
                    // Find it appropriate to link to this target word
                    // with a (log) probability of 0.
                    return new OpenTargetBond(
                        MaybeTargetPoint: new MaybeTargetPoint(targetPoint),
                        Score: 0);
                }
            }

            // Attempt to find existing links in the immediate context
            // surrounding the unlinked source point by using the syntax
            // tree.
            List<OpenMonoLink> linkedSiblings =
                GetLinkedSiblings(
                    sourceNode.Terminal,
                    linksTable);

            // If any such links were found:
            if (linkedSiblings.Count > 0)
            {
                // Get the nearest such link before the unlinked source point.
                OpenMonoLink preNeighbor =
                    GetPreNeighbor(sourceNode, linkedSiblings);

                // Get the nearest such link after the unlinked source point.
                OpenMonoLink postNeighbor =
                    GetPostNeighbor(sourceNode, linkedSiblings);

                // Prepare to collect candidate target points.
                List<MaybeTargetPoint> targetCandidates = new List<MaybeTargetPoint>();

                // If there are neighboring links on both sides:
                if (preNeighbor != null && postNeighbor != null)
                {
                    // Find suitable candidate target points that lie
                    // between the neighboring links.
                    targetCandidates =
                        GetTargetCandidates(
                            preNeighbor,
                            postNeighbor,
                            targetPoints,
                            linkedTargets,
                            assumptions);
                }
                // Otherwise if there is (only) a neighboring link to
                // the left:
                else if (preNeighbor != null)
                {
                    // Find suitable candidate target points that lie
                    // in the region surrounding the left neighbor.
                    targetCandidates =
                        GetTargetCandidates(
                            preNeighbor,
                            targetPoints,
                            linkedTargets,
                            assumptions);
                }
                // Otherwise if there is (only) a neighboring link to
                // the right:
                else if (postNeighbor != null)
                {
                    // Find suitable candidate target points that lie
                    // in the region surrounding the right neighbor.
                    targetCandidates =
                        GetTargetCandidates(
                            postNeighbor,
                            targetPoints,
                            linkedTargets,
                            assumptions);
                }

                // If any target candidates were found:
                if (targetCandidates.Count > 0)
                {
                    // Perform further analysis to determine the best
                    // candidate from among the target candidates.
                    OpenTargetBond newTarget = GetTopCandidate(
                        sourceNode,
                        targetCandidates,
                        linkedTargets,
                        assumptions);

                    // If a best candidate was found:
                    if (newTarget != null)
                    {
                        return newTarget;
                    }
                }
            }

            // No suitable candidate was found by any of the strategies
            // above.
            return null;
        }


        /// <summary>
        /// Get candidate target points for linking to an unlinked source
        /// point the lie in the region surrounding an anchor link.
        /// </summary>
        /// <param name="anchorLink">
        /// The anchoring link that determines the region to be searched.
        /// </param>
        /// <param name="targetPoints">
        /// The target points in the zone.
        /// </param>
        /// <param name="linkedTargets">
        /// The source IDs (as canonical strings) for those target points
        /// that are already linked.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain the auto alignment.
        /// </param>
        /// <returns>
        /// Candidate target points for linking to the unlinked source
        /// point.
        /// </returns>
        /// 
        public static List<MaybeTargetPoint> GetTargetCandidates(
            OpenMonoLink anchorLink,
            List<TargetPoint> targetPoints,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions)
        {
            // Get the anchor position as an index into the list of
            // target points.
            int anchor = anchorLink.OpenTargetBond.MaybeTargetPoint.Position;

            // Helper function to generate an enumeration that starts to
            // the left of the anchor position and goes down for three steps.
            IEnumerable<int> down()
            {
                for (int i = anchor - 1; i >= anchor - 3; i--)
                    yield return i;
            }

            // Helper function to generate an enumeration that goes to
            // the right of the anchor position and goes up for three steps.
            IEnumerable<int> up()
            {
                for (int i = anchor + 1; i <= anchor + 3; i++)
                    yield return i;
            }

            // Search for suitable target words to the left and to the
            // right of the anchor position.
            return
                getWords(down())
                .Concat(getWords(up()))
                .ToList();

            // Helper function that looks for suitable target words within
            // an enumeration of index positions.
            IEnumerable<MaybeTargetPoint> getWords(
                IEnumerable<int> positions) =>
                    PositionsToTargetCandidates(
                        positions,
                        targetPoints,
                        linkedTargets,
                        assumptions);
        }


        /// <summary>
        /// Get candidate target points for linking to an unlinked source
        /// point between a link on the left and a link on the right.
        /// </summary>
        /// <param name="leftAnchor">
        /// The neighboring link on the left.
        /// </param>
        /// <param name="rightAnchor">
        /// The neighboring link on the right.
        /// </param>
        /// <param name="targetPoints">
        /// The target points in the zone.
        /// </param>
        /// <param name="linkedTargets">
        /// The source IDs (as canonical strings) for those target points
        /// that are already linked.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain the auto alignment.
        /// </param>
        /// <returns>
        /// Candidate target points for linking to the unlinked source
        /// point.
        /// </returns>
        /// 
        public static List<MaybeTargetPoint> GetTargetCandidates(
            OpenMonoLink leftAnchor,
            OpenMonoLink rightAnchor,
            List<TargetPoint> targetPoints,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions)
        {
            // Helper function that enumerates the indices of target points
            // between the left and right neighbors.
            IEnumerable<int> span()
            {
                for (int i = leftAnchor.OpenTargetBond.MaybeTargetPoint.Position;
                    i < rightAnchor.OpenTargetBond.MaybeTargetPoint.Position;
                    i++)
                {
                    yield return i;
                }
            }

            // Convert the enumeration to a list of target candidates.
            return PositionsToTargetCandidates(
                span(),
                targetPoints,
                linkedTargets,
                assumptions).ToList();
        }


        /// <summary>
        /// Find target points in a specified enumeration of target point
        /// positions that are suitable for linking to an unlinked source
        /// point.
        /// </summary>
        /// <param name="positions">
        /// Enumeration of the target point positions to be considered.
        /// </param>
        /// <param name="targetPoints">
        /// Target points for the zone.
        /// </param>
        /// <param name="linkedTargets">
        /// Target IDs (as canonical strings) of target points that are
        /// already linked.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain the auto-alignment.
        /// </param>
        /// <returns>
        /// Suitable target points, as a list of MaybeTargetPoint objects.
        /// </returns>
        /// 
        public static IEnumerable<MaybeTargetPoint> PositionsToTargetCandidates(
            IEnumerable<int> positions,
            List<TargetPoint> targetPoints,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions)
        {
            // Starting from the enumerated positions and keeping only those
            // that are valid indices, look up the target point for each index,
            // get the lower-cased text of the target point, if assuming
            // content words only then remove those that are function words,
            // keep only those not already linked, take the resulting
            // sequence only until punctuation is found, and convert to
            // MaybeTargetPoint objects.
            var ansr =
                positions
                .Where(n => n >= 0 && n < targetPoints.Count)
                .Select(n => targetPoints[n])
                .Select(targetPoint => new
                {
                    targetPoint.Lower,
                    targetPoint
                })
                .Where(x =>
                    !assumptions.ContentWordsOnly || isContentWord(x.Lower))
                .Where(x => isNotLinkedAlready(x.Lower))
                .TakeWhile(x => isNotPunctuation(x.Lower))
                .Select(x => new MaybeTargetPoint(x.targetPoint))
                .ToList();

            return ansr;

            // Helper function to check for a content word.
            bool isContentWord(string text) =>
                !assumptions.IsTargetFunctionWord(text);

            // Helper function to check that a target point is not
            // linked already.
            bool isNotLinkedAlready(string text) =>
                !linkedTargets.Contains(text);

            // Helper function to check for punctuation.
            bool isNotPunctuation(string text) =>
                !assumptions.IsPunctuation(text);
        }


        /// <summary>
        /// Examine the candidate target points that have been identified
        /// for possible linking to an unlinked source point during the
        /// ImproveAlignment() phase, and find the best one.
        /// </summary>
        /// <param name="sWord">
        /// The unlinked source point.
        /// </param>
        /// <param name="tWords">
        /// The candidate target points, as a list of MaybeTargetPoint.
        /// </param>
        /// <param name="linkedTargets">
        /// List of target ID (as a canonical string) for those target
        /// points that are already linked.
        /// </param>
        /// <param name="assumptions">
        /// Assumptions that constrain the auto-alignment.
        /// </param>
        /// <returns>
        /// An OpenTargetBond for the best candidate, or null if there
        /// is no suitable candidate.
        /// </returns>
        /// 
        public static OpenTargetBond GetTopCandidate(
            SourcePoint sWord,
            List<MaybeTargetPoint> tWords,
            List<string> linkedTargets,
            IAutoAlignAssumptions assumptions
            )
        {
            // Make a table of probabilities for suitable
            // target points.
            // Starting with the candidate target points,
            // filter out those that are unsuitable, get
            // the score from the estimated translation model,
            // keep only those with a score of at least 0.17,
            // and make a table for the results.
            // The table maps the candidate to the log of
            // its score.
            // A candidate is unsuitable if:
            // the target word is punctuation;
            // the target point is a stopword;
            // it has been identified as a bad link;
            // the source point is a stop word and the
            // candidate has not been identified as a
            // good link;
            // or the target word is already linked.
            Dictionary<MaybeTargetPoint, double> probs =
                tWords
                .Where(notPunctuation)
                .Where(notTargetStopWord)
                .Where(notBadLink)
                .Where(sourceStopWordImpliesIsGoodLink)
                .Where(notAlreadyLinked)
                .Select(tWord => new
                {
                    tWord,
                    score = getTranslationModelScore(tWord)
                })
                .Where(x => x.score >= 0.17)
                .ToDictionary(
                    x => x.tWord,
                    x => Math.Log(x.score));

            // Helper function to test that target point is not punctuation.
            bool notPunctuation(MaybeTargetPoint tw) =>
                !assumptions.IsPunctuation(tw.Lower);

            // Helper function to test that target point is not a stopword.
            bool notTargetStopWord(MaybeTargetPoint tw) =>
                !assumptions.IsStopWord(tw.Lower);

            // Helper function to test that target point is not already linked.
            bool notAlreadyLinked(MaybeTargetPoint tw) =>
                !linkedTargets.Contains(tw.ID);

            // Helper function to test that candidate is not a bad link.
            bool notBadLink(MaybeTargetPoint tw) =>
                !assumptions.IsBadLink(sWord.Lemma, tw.Lower);

            // Helper function to test that if source point is a stopword
            // then candidate is a good link.
            bool sourceStopWordImpliesIsGoodLink(MaybeTargetPoint tw) =>
                !assumptions.IsStopWord(sWord.Lemma) ||
                assumptions.IsGoodLink(sWord.Lemma, tw.Lower);

            // Helper function to obtain the score for a candidate
            // from the estimated translation model.
            double getTranslationModelScore(MaybeTargetPoint tw) =>
                assumptions.GetTranslationModelScore(sWord.Lemma, tw.Lower);

            // If the probabilities table has any entries:
            if (probs.Count > 0)
            {
                // Sort the candidates by their probabilities in
                // descending order, with a special auxiliary hashing
                // function to break ties:
                List<MaybeTargetPoint> candidates =
                    SortWordCandidates(probs);

                // Get the first candidate in the result.
                MaybeTargetPoint topCandidate = candidates[0];

                // Express the candidate as an OpenTargetBond.
                OpenTargetBond linkedWord = new OpenTargetBond(
                    MaybeTargetPoint: topCandidate,
                    Score: probs[topCandidate]);

                return linkedWord;
            }

            // No suitable candidates were found; give up.
            return null;
        }


        /// <summary>
        /// Sort target word candidates by their probabilities, using
        /// a special hashing function to break ties.
        /// </summary>
        /// <param name="pathProbs">
        /// Table mapping the candidates to their probabilities.
        /// </param>
        /// <returns>
        /// The sorted candidates.
        /// </returns>
        /// 
        public static List<MaybeTargetPoint> SortWordCandidates(
            Dictionary<MaybeTargetPoint, double> pathProbs)
        {
            // Helper function to compute auxiliary hash code:
            // construct a string mentioning the lower-cased
            // text and position of the candidate, and call the
            // standard hashing function on this string.
            int hashCodeOfWordAndPosition(MaybeTargetPoint tw) =>
                $"{tw.Lower}-{tw.Position}".GetHashCode();

            // Starting from the candidates table, order the
            // entries by probability in descending order and
            // then by the auxiliary hash code, and return the
            // candidates for the resulting sorted entries.
            return
                pathProbs
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenByDescending(kvp =>
                        hashCodeOfWordAndPosition(kvp.Key))
                    .Select(kvp => kvp.Key)
                    .ToList();
        }



        /// <summary>
        /// Find the closest link that occurs before the specified
        /// source point, with respect to the syntax tree ordering of
        /// source points.
        /// </summary>
        /// <param name="sourceNode">
        /// The source point for which a nearest left neighboring link is
        /// sought.
        /// </param>
        /// <param name="linkedSiblings">
        /// The links to be examined to find a nearest left neighboring link.
        /// </param>
        /// <returns>
        /// The nearest left neighboring link, or null if none is found.
        /// </returns>
        /// 
        public static OpenMonoLink GetPreNeighbor(
            SourcePoint sourceNode,
            List<OpenMonoLink> linkedSiblings)
        {
            // The limiting position is the start position of the
            // terminal node associated with the source point.
            int limit = sourceNode.Terminal.AttrAsInt("Start");

            // Helper function to compute the ending position of
            // a link with respect to the syntax tree.
            int end(OpenMonoLink mw) =>
                mw.SourcePoint.Terminal.AttrAsInt("End");

            // Starting with the links to be examined,
            // let the associated distance be the limit minus the
            // end position of the link, keep only those links
            // with positive distances, sort by distance in
            // increasing order, and return the first link that
            // remains, or null if nothing remains.
            return
                linkedSiblings
                .Select(mw => new { mw, distance = limit - end(mw) })
                .Where(x => x.distance > 0)
                .OrderBy(x => x.distance)
                .Select(x => x.mw)
                .FirstOrDefault();
        }


        /// <summary>
        /// Find the closest link that occurs after the specified source
        /// point, with respect to the syntax tree ordering of source
        /// points.
        /// </summary>
        /// <param name="sourceNode">
        /// The source point for which a nearest right neighboring link is
        /// sought.
        /// </param>
        /// <param name="linkedSiblings">
        /// The links to be examined to find a nearest right neighboring
        /// link, assumed to be already sorted in tree order.
        /// </param>
        /// <returns>
        /// 
        /// </returns>
        /// 
        public static OpenMonoLink GetPostNeighbor(
            SourcePoint sourceNode,
            List<OpenMonoLink> linkedSiblings)
        {
            // The limiting position is the end position of the
            // terminal node associated with the source point.
            int limit = sourceNode.Terminal.AttrAsInt("End");

            // Helper function to compute the ending position of
            // a link with respect to the syntax tree.
            int end(OpenMonoLink mw) =>
                mw.SourcePoint.Terminal.AttrAsInt("End");

            // Starting with the links to be examined,
            // keep only those with ending position greater
            // than the limit, and return the first such
            // link, or null if there are no such links.
            return
                linkedSiblings
                .Where(mw => end(mw) > limit)
                .FirstOrDefault();
        }


        /// <summary>
        /// Attempt to find links associated with the immediate context
        /// surrounding a syntax tree node but not with any source points below
        /// that node.
        /// </summary>
        /// <param name="treeNode">
        /// Syntax tree node to be examined.
        /// </param>
        /// <param name="linksTable">
        /// Table of source points that are already linked, consisting of
        /// a mapping from source ID (as a canonical string) to its
        /// OpenMonoLink.
        /// </param>
        /// <returns>
        /// List of OpenMonoLink for the links found, and which might be
        /// empty.
        /// </returns>
        /// 
        public static List<OpenMonoLink> GetLinkedSiblings(
            XElement treeNode,
            Dictionary<string, OpenMonoLink> linksTable)
        {
            // If this tree node has a parent that is not the container
            // for the syntax tree:
            if (treeNode.Parent != null &&
                treeNode.Parent.Name.LocalName != "Tree")
            {
                // Starting with the children of the parent
                // but excluding the start node itself,
                // get all of the terminal nodes beneath these
                // nodes, convert them to Source ID canonical strings,
                // look up these IDs in the table of source points
                // already linked, and keep only the non-null results.
                List<OpenMonoLink> linkedSiblings =
                    treeNode.Parent.Elements()
                    .Where(child => child != treeNode)
                    .SelectMany(child => child.GetTerminalNodes())
                    .Select(term => term.SourceId())
                    .Select(sourceId => linksTable.GetValueOrDefault(sourceId))
                    .Where(x => !(x is null))
                    .ToList();

                // If no links were found:
                if (linkedSiblings.Count == 0)
                {
                    // Try again starting with the parent node
                    // by calling this function recursively.
                    return GetLinkedSiblings(treeNode.Parent, linksTable);
                }
                else
                {
                    // Some links were found, return them.
                    return linkedSiblings;
                }
            }
            else
            {
                // We have reached the top of the tree, so there are
                // no sibling links.
                return new List<OpenMonoLink>();
            }
        }

    }
}
