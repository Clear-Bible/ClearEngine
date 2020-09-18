using System;

namespace ClearBible.Clear3.API
{
    public interface Lemma
    {
        Uri Context { get; }

        string Key { get; }  // Could be a Strong's Number

        string LemmaText { get; }

        Uri Language { get; }

        int Submeaning { get; }
    }

    public interface LemmaService
    {
        Status LoadLemmaResource(Uri context);

        bool Find(Uri context, string key, out Lemma lemma);

        Status Create(
            Uri context,
            string key,
            string lemmaText,
            Uri language,
            int submeaning,
            out Lemma lemma);
    }    
}
