using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public EngineUsfmFileTextCorpus(ITokenizer<string, int, string> wordTokenizer, string stylesheetFileName, 
            Encoding encoding, string projectPath, ScrVers? versification = null, bool includeMarkers = false, string filePattern = "*.SFM")
            : base(wordTokenizer, stylesheetFileName, encoding, projectPath, versification, includeMarkers, filePattern)
        {
            var stylesheet = new UsfmStylesheet(stylesheetFileName);
            foreach (string sfmFileName in Directory.EnumerateFiles(projectPath, filePattern))
            {
                var engineText = new EngineUsfmFileText(wordTokenizer, stylesheet, encoding, sfmFileName, Versification,
                    includeMarkers);
                EngineTextDictionary[engineText.Id] = engineText;
            }
        }

        protected Dictionary<string, IText> EngineTextDictionary { get;} = new Dictionary<string, IText>();

        /// <summary>
        /// Used to obtain Engine USFM texts which don't attempt to group segments based on Machine versification
        /// when its GetSegments() is called, which is necessary when Engine applies its own versification mapping.
        /// </summary>
        /// <param name="id">The book id in three-character notation, e.g. "1JN"</param>
        /// <returns>The Engine Text from which segments, each of which is a verse id and text, can then be obtained by Text.GetSegments(). 
        /// Segments are not grouped with Machine versification so Engine can apply its own mapping.
        /// </returns>
        public IText GetEngineText(string id)
        {
            if (EngineTextDictionary.TryGetValue(id, out IText? text))
                    return text;
                return CreateNullText(id);
         }
    }
}
