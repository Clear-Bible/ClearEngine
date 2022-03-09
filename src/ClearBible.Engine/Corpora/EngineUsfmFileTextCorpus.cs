using System.Text;

using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;

using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    /// <summary>
    /// Used to obtain the USFM Texts, with each Text corresponding to a book identified (Id) 
    /// by its three character book designation, e.g. "1JN'.
    /// 
    /// Segments, each of which is a verse id and text, can then be obtained by Text.GetSegments().
    /// 
    /// This override returns custom Engine texts through a new method GetEngineText() which doesn't attempt to 
    /// group segments by versification when Engine wants to replace Machine's versification with its own versification mapping.
    /// </summary>
    public class EngineUsfmFileTextCorpus : UsfmFileTextCorpus, IEngineCorpus
    {
        public EngineUsfmFileTextCorpus(
            ITokenizer<string, int, string> wordTokenizer, 
            string stylesheetFileName, 
            Encoding encoding, 
            string projectPath, 
            ScrVers? versification = null,
            ITextSegmentProcessor? textSegmentProcessor = null,
            bool includeMarkers = false, 
            string filePattern = "*.SFM")
            : base(wordTokenizer, stylesheetFileName, encoding, projectPath, versification, includeMarkers, filePattern)
        {
            TextSegmentProcessor = textSegmentProcessor;

            var stylesheet = new UsfmStylesheet(stylesheetFileName);
            TextDictionary.Clear();
            foreach (string sfmFileName in Directory.EnumerateFiles(projectPath, filePattern))
            {
                AddText(new EngineUsfmFileText(wordTokenizer, stylesheet, encoding, sfmFileName, Versification,
                    includeMarkers, this));
            }
        }

        public ITextSegmentProcessor? TextSegmentProcessor { get; set; }
        public bool DoMachineVersification { get; set; } = true;
    }
}
