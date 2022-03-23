using ClearBible.Engine.Tokenization;

using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    /// <summary>
    /// Used to obtain the USX Texts, with each Text corresponding to a book identified (Id) 
    /// by its three character book designation, e.g. "1JN'.
    /// 
    /// Segments, each of which is a verse id and text, can then be obtained by Text.GetSegments().
    /// 
    /// This override returns custom Engine texts through a new method GetEngineText() which doesn't attempt to 
    /// group segments by versification when Engine wants to replace Machine's versification with its own versification mapping.
    /// </summary>
    public class EngineUsxFileTextCorpus : UsxFileTextCorpus, IEngineCorpus
    {
        public EngineUsxFileTextCorpus(
            ITokenizer<string, int, string> wordTokenizer, 
            string projectPath, 
            ScrVers? versification = null,
            BaseTextSegmentProcessor? textSegmentProcessor = null) 
            : base(wordTokenizer, projectPath, versification)
        {
            TextSegmentProcessor = textSegmentProcessor;
            TextDictionary.Clear();
            foreach (string fileName in Directory.EnumerateFiles(projectPath, "*.usx"))
            {
                AddText(new EngineUsxFileText(wordTokenizer, fileName, Versification, this));
            }
        }

        public BaseTextSegmentProcessor? TextSegmentProcessor { get; set; }
        public bool DoMachineVersification { get; set; } = true;

        public void Train(ParallelTextCorpus parallelTextCorpus, ITextCorpus textCorpus)
        {
            TextSegmentProcessor?.Train(parallelTextCorpus, textCorpus);
        }
    }
}
