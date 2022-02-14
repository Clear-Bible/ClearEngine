using SIL.Machine.Corpora;
using SIL.Scripture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Corpora
{
    public class ManuscriptFileTextCorpus : ScriptureTextCorpus, IEngineCorpus
    {
        public ManuscriptFileTextCorpus(string manusciptTreesPath) : base(null)
        {
            //var foo = Directory.EnumerateFiles(manusciptTreesPath, "*.xml")
            //    .Select(fileName => fileName.Trim().Substring(manusciptTreesPath.Length + 1, fileName.Length - manusciptTreesPath.Length - 1 - 13))
            //    .Distinct();

            Directory.EnumerateFiles(manusciptTreesPath, "*.xml")
                .Select(fileName => fileName.Trim().Substring(manusciptTreesPath.Length + 1, fileName.Length - manusciptTreesPath.Length - 1 - 13))
                //names are  in b[bb]ccc.trees.xml format, and we want the b[bb] part, therefore subtracting
                // 13 characters and 1 more for the directory separator.
                .Distinct()
                .Select(fileNameBookPrefix =>
                {
                    AddText(new ManuscriptFileText(manusciptTreesPath, fileNameBookPrefix, Versification));
                    return fileNameBookPrefix;
                }).ToList();
        }
        public override ScrVers Versification => ScrVers.Original;

        public IText GetEngineText(string id)
        {
            if (TextDictionary.TryGetValue(id, out IText? text))
                return text;
            return CreateNullText(id);
        }
    }
}
