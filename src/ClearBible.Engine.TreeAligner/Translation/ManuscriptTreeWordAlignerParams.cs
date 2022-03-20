using ClearBible.Engine.TreeAligner.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.TreeAligner.Translation
{
    public class ManuscriptTreeWordAlignerParams
    {
        public ManuscriptTreeWordAlignerParams(
            Dictionary<string, Dictionary<string, int>> strongs,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            Dictionary<string, int> goodLinks,
            Dictionary<string, int> badLinks,
            List<string> sourceFunctionWords,
            List<string> targetFunctionWords,
            List<string> stopWords,
            List<string> puncs,
            TranslationModel manTransModel,
            GroupTranslationsTable groups)
        {
            this.strongs = strongs;
            this.glossTable = glossTable;
            this.oldLinks = oldLinks;
            this.goodLinks = goodLinks;
            this.badLinks = badLinks;
            this.sourceFunctionWords = sourceFunctionWords;
            this.targetFunctionWords = targetFunctionWords;
            this.stopWords = stopWords;
            this.puncs = puncs;
            this.manTransModel = manTransModel;
            this.groups = groups;
        }

        public ManuscriptTreeWordAlignerParams(
            Dictionary<string, Dictionary<string, int>> strongs, 
            Dictionary<string, Gloss> glossTable, 
            Dictionary<string, Dictionary<string, string>> oldLinks, 
            Dictionary<string, int> goodLinks, 
            Dictionary<string, int> badLinks, 
            List<string> sourceFunctionWords, 
            List<string> targetFunctionWords, 
            List<string> stopWords, 
            List<string> puncs, 
            TranslationModel manTransModel, 
            GroupTranslationsTable groups,
            int maxPaths,
            int goodLinkMinCount,
            int badLinkMinCount,
            bool useAlignModel,
            bool contentWordsOnly,
            bool useLemmaCatModel) 
        {
            this.maxPaths = maxPaths;
            this.goodLinkMinCount = goodLinkMinCount;
            this.badLinkMinCount = badLinkMinCount;
            this.useAlignModel = useAlignModel;
            this.contentWordsOnly = contentWordsOnly;
            this.useLemmaCatModel = useLemmaCatModel;
            this.strongs = strongs;
            this.glossTable = glossTable;
            this.oldLinks = oldLinks;
            this.goodLinks = goodLinks;
            this.badLinks = badLinks;
            this.sourceFunctionWords = sourceFunctionWords;
            this.targetFunctionWords = targetFunctionWords;
            this.stopWords = stopWords;
            this.puncs = puncs;
            this.manTransModel = manTransModel;
            this.groups = groups;
        }


        public Dictionary<string, Dictionary<string, int>> strongs { get; }
        public Dictionary<string, Gloss> glossTable { get; }
        public Dictionary<string, Dictionary<string, string>> oldLinks { get; }
        public Dictionary<string, int> goodLinks { get; }
        public Dictionary<string, int> badLinks { get; }
        public List<string> sourceFunctionWords { get; }
        public List<string> targetFunctionWords { get; }
        public List<string> stopWords { get; }
        public List<string> puncs { get; }
        public TranslationModel manTransModel { get; }
        public GroupTranslationsTable groups { get; }
        public int maxPaths { get; set; }
        public int goodLinkMinCount { get; set; }
        public int badLinkMinCount { get; set; }
        public bool useAlignModel { get; set; }
        public bool contentWordsOnly { get; set; }
        public bool useLemmaCatModel { get; set; }
    }
}
