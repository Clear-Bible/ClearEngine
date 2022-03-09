using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Tokenization
{
    public record TokenId(int BookNum, int ChapterNum, int VerseNum, int WordNum, int SubWordNum)
    {
        public override string ToString()
        {
            return $"{BookNum.ToString("000")}{ChapterNum.ToString("000")}{VerseNum.ToString("000")}{WordNum.ToString("000")}{SubWordNum.ToString("000")}";
        }
    }
}
