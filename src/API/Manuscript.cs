using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    //public interface Manuscript
    //{
    //    Uri Id { get; }

    //    IEnumerable<ManuscriptSegment> All();

    //    IEnumerable<ManuscriptSegment> Book(int book);

    //    IEnumerable<ManuscriptSegment> Chapter (int book, int chapter);

    //    IEnumerable<ManuscriptSegment> Verse(
    //        int book, int chapter, int verse);

    //    bool Segment(
    //        int book, int chapter, int verse, int word, int segNum,
    //        out ManuscriptSegment segment);

    //    bool Find(string key, out ManuscriptSegment segment);
    //}

    //public interface ManuscriptSegment
    //{
    //    Status QueryId(out string key, out Manuscript context);

    //    Status QueryPosition(
    //        out int chapter,
    //        out int book,
    //        out int word,
    //        out int segNum);

    //    Status Query(
    //        out string surfaceText,
    //        out string optionalMorphology,
    //        out Lemma optionalLemma);
    //}

    

    //public interface ManuscriptFactory
    //{
    //    Status CreateManuscript(
    //        Uri id,
    //        out Manuscript manuscript);

    //    Status CreateSegment(
    //        Manuscript manuscript,
    //        int book,
    //        int chapter,
    //        int verse,
    //        int word,
    //        int segNum,
    //        string surfaceText,
    //        out ManuscriptSegment segment);

    //    Status DeleteSegment(
    //        Manuscript manuscript,
    //        string manuscriptSegmentKey);

    //    Status SetMorphology(
    //        ManuscriptSegment segment,
    //        string morphology);

    //    Status SetLemma(
    //        ManuscriptSegment segment,
    //        Uri lemmaContext,
    //        string lemmaKey,
    //        string lemmaText,
    //        Uri language,
    //        int submeaning);

    //    Status ClearLemma(ManuscriptSegment segment);
    //}
}
