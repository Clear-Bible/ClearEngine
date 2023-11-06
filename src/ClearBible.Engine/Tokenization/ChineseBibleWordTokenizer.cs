using System.Reflection;

namespace ClearBible.Engine.Tokenization
{
    /// <summary>
    /// A tokenizer for bible translations (not general Chinese translations, see below).
    /// 
    /// Chinese words and combination corrections provided by Andi Wu (andi.wu@globalbibleinitiative.org)
    /// </summary>
    public class ChineseBibleWordTokenizer : MaximalMatchingTokenizer
    {
        private const string WORDS_FILE_NAME = "words.txt";
        private const string COMBINATION_CORRECTIONS_FILE_NAME = "combination_corrections.txt";

        public  ChineseBibleWordTokenizer() : this(null, MAX_GRAM_DEFAULT)
        {
        }
        public ChineseBibleWordTokenizer(string? chineseTokenizerDataDirectoryPath, int maxGram) : base(maxGram)
        {
            // set Words and overlaps.
            var dataDirectoryFilePath = chineseTokenizerDataDirectoryPath ??
                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                            + Path.DirectorySeparatorChar
                            + "Tokenization"
                            + Path.DirectorySeparatorChar
                            + "Data"
                            + Path.DirectorySeparatorChar
                            + "ChineseBibleWordTokenizer"
                            + Path.DirectorySeparatorChar;

            File.ReadAllLines(dataDirectoryFilePath + WORDS_FILE_NAME)
                        .Where(line => line != string.Empty)
                        .Select(Words.Add)
                        .ToList();

            foreach (var line in File.ReadAllLines(dataDirectoryFilePath + COMBINATION_CORRECTIONS_FILE_NAME))
            {
                string[] parts = line.Split("\t".ToCharArray());
                if (!CombinationCorrections.ContainsKey(parts[0]))
                    CombinationCorrections.Add(parts[0], parts[1]);
            }
        }
    }
}
