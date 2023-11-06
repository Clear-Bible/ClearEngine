using SIL.Machine.Annotations;
using SIL.Machine.Tokenization;

/*
Copyright © 2023 Cherith Analytics, LLC
Permission is hereby granted, free of charge, to any person obtaining a copy of portions of this file, specifically WhitespaceDelimitLongestWordMatches() and GetNgram(), (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/


/* Notes from Andi:
The maximal matching algorithm has two problems: 

(1) it can be wrong when there are combinational ambiguity 
where a sequence of characters can be single word/token in some contexts but separate words in other contexts.  
Since MM always go for the bigger one, these sequences will always be segmented as a single word regardless of contexts.  
Cases of combinational ambiguity do not occur very frequently, though.

(2) it can be wrong when there are overlapping ambiguities where a sequence of characters such as ABC can be 
segmented as AB C or A BC depending on the contexts.  The overlap.txt file is used to correct such mistakes, 
favoring the more likely segmentation in the Bible.

Another way to resolve overlapping ambiguities is to do it from both directions.  The code I gave you only does 
it in one direction: left to right.  But you can do both left to right and right to left.  The tokens you can get in 
both left and right directions are guaranteed to be correct, but you need a separate routine to handle the ones that 
do not agree in both directions.  You may lose words if you only keep the tokens that you can get from both directions.  
In other words, bi-directional maximal matching can improve precision but lose in recall.

In spite of these problems, the accuracy of MM is pretty high and meets the needs of most applications.

To improve the coverage of the current tokenizer (i.e. to be able to handle texts that contain words that are not in the word list), 
we just need to add words to the list.  However, the more words are in the list, the more likely we will have combinational and overlapping 
ambiguities.
*/
namespace ClearBible.Engine.Tokenization
{
    /// <summary>
    ///
    /// Based on Andi Wu's BibleTokenizer (andi.wu@globalbibleinitiative.org)
    /// 
    /// 1. looks at a verse's characters as if each is a linear (sequential) graph of 
    /// vertices (characters) and edges (their pairwise adjacency).
    /// 2. tries to match them to the pairs of characters in the vocabulary file (words.txt), considering each pair 
    /// in this file as as two vertices connected by a pair-wise-edge, using a simple greedy algorithm,
    /// 3. to find the maximal matching set for the verse consistent with the pairs in the vocabulary file,
    /// 4. which are considered characters joined together as a token.
    /// 
    /// Words contains all the words to match.
    /// 
    /// CombinationCorrections map combination errors to corrections. 
    /// 
    /// </summary>
    public class MaximalMatchingTokenizer : WhitespaceTokenizer
    {
        public const int MAX_GRAM_DEFAULT = 10;

        private readonly int _maxGram;
        protected HashSet<string> Words { get; } = new();
        protected Dictionary<string, string> CombinationCorrections = new();

        /// <summary>
        /// Add words to maximal match on to Words property.
        /// </summary>
        /// <param name="maxGram"></param>
        public MaximalMatchingTokenizer(int maxGram)
        {
            _maxGram = maxGram;
        }
        public override IEnumerable<Range<int>> TokenizeAsRanges(string data, Range<int> range)
        {
            //segment words
            var whitespaceSegmentedText = WhitespaceDelimitLongestWordMatches(data.Substring(range.Start, range.Length), Words, _maxGram);

            //correct overlaps
            CorrectCombinations(whitespaceSegmentedText);

            var rangesForWhitespaceSegmentedText = base.TokenizeAsRanges(whitespaceSegmentedText, Range<int>.Create(0, whitespaceSegmentedText.Length));

            return rangesForWhitespaceSegmentedText
                .Select((r, i) => Range<int>.Create(r.Start - i, r.End - i));
        }

        /// <summary>
        /// Use to correct the following combination errors by replacing a sequence of characters with another:
        /// 1. maximal match algorithm favors largest combination of characters, but this can be incorrect when in context with other characters
        /// that follow. For example, if algorithm tokenizes as AB, in cases where AB is followed by C the characters A and B should instead be tokenized as A B.
        /// 2. When there is an overlapping ambiguity, for example, if algorithm tokenizes as AB C but it could have also tokenized as A BC.
        /// 
        /// These corrections should favor the majority cases.
        /// </summary>
        /// <param name="whitespaceSegmentedText"></param>
        protected virtual void CorrectCombinations(string whitespaceSegmentedText)
        {
            CombinationCorrections
                .Select(kvp => whitespaceSegmentedText.Replace(kvp.Key, kvp.Value))
                .ToList();
        }

        /// <summary>
        /// Note: whitespace delimits spaces that aren't a part of a match.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="words"></param>
        /// <param name="maxGram"></param>
        /// <returns></returns>
        protected virtual string WhitespaceDelimitLongestWordMatches(string text, HashSet<string> words, int maxGram)
        {
            string segments = string.Empty;

            char[] chars = text.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                for (int n = maxGram; n > 0; n--)
                {
                    string ngram = GetNgram(chars, i, n);
                    if (ngram != string.Empty && (n == 1 || words.Contains(ngram)))
                    {
                        segments += ngram + (ngram != " " ? " " : ""); // only delimit if the single character is not a space, otherwise just add space.
                        i = i + n - 1; // need to subtract 1 because the iterator increments after the body.
                        break;
                    }
                }
            }
            return segments;
        }
        protected static string GetNgram(char[] chars, int index, int length)
        {
            string ngram = string.Empty;

            for (int i = index; (i + length) <= chars.Length && length > 0; i++, length--)
            {
                ngram += chars[i].ToString();
            }

            return ngram;
        }
    }
}
