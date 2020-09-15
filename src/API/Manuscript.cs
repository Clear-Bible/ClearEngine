using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface Manuscript
    {
        Uri Id { get; }

        IEnumerable<ManuscriptSegment> All();

        IEnumerable<ManuscriptSegment> Book(int book);

        IEnumerable<ManuscriptSegment> Chapter (int book, int chapter);

        IEnumerable<ManuscriptSegment> Verse(
            int book, int chapter, int verse);

        ManuscriptSegment Get(
            int book, int chapter, int verse, int word, int segment);

        ManuscriptSegment Get(string key);
    }

    public interface ManuscriptSegmentId
    {
        string Key { get; }

        int Book { get; }

        int Chapter { get; }

        int Verse { get; }

        int Word { get; }

        int Segment { get; }
    }

    public interface ManuscriptSegment
    {
        Manuscript Context { get; }

        ManuscriptSegmentId Id { get; }

        string SurfaceText { get; }

        string Morphology { get; }

        Lemma Lemma { get; }
    }

    public interface Lemma
    {
        Uri Context { get; }

        string Key { get; }

        string LemmaText { get; }

        Uri Language { get; }

        int Submeaning { get; }
    }
}
