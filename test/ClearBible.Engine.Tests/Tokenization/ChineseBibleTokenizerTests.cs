﻿using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tests.Corpora;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;



namespace ClearBible.Engine.Tests.Tokenization
{
    public class ChineseBibleTokenizerTests
    {
        protected readonly ITestOutputHelper output_;
        public ChineseBibleTokenizerTests(ITestOutputHelper output)
        {
            output_ = output;
        }

        [Fact]
        public void Tokenization__ChineseBible()
        {
            List<string> verseTexts = new()
            {
                "他在世界，世界也是藉著他造的，世界卻不認識他。", //no extra spaces
                "耶和華　神用泥土造了野地的各樣野獸，和空中的各樣飛鳥，把牠們都帶到那人面前，看他給牠們叫甚麼名字；那人怎樣叫各樣有生命的活物，那就是牠的名字。", //space in between first and second word.
                " 因此人要離開父母，和妻子連合，二人成為一體。 那時，夫妻二人赤身露體，彼此都不覺得羞恥。", // space at beginning and after first period.
                "他在世界，世界也是藉著他造的，世界卻不認識他。 ", //extra space at end
            };

            var corpus = new DictionaryTextCorpus(
                new MemoryText("text1", verseTexts
                    .Select((vt, i)  => TestHelpers.CreateTextRow(new VerseRef(1, 1, i + 1), vt, isSentenceStart: true))))
                .Tokenize<ChineseBibleWordTokenizer>()
                .Transform<IntoTokensTextRowProcessor>()
                .Transform<SetTrainingBySurfaceLowercase>()
                .ToList(); //so it only tokenizes and transforms once.

            output_.WriteLine("Texts:");
            corpus.Cast<TokensTextRow>()
                .Select((ttr, i) =>
                {
                    output_.WriteLine($"Original: {verseTexts[i]}");
                    TestHelpers.WriteTokensTextRow(output_, ttr, new EngineStringDetokenizer(new ChineseBibleWordDetokenizer()));
                    return ttr;
                })
                .ToList();
        }
    }
}