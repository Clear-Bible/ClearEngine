using SIL.Machine.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Tokenization
{
    public class FunctionWordTextRowProcessor : IRowProcessor<TextRow>
    {
        public TextRow Process(TextRow textRow)
        {
            // perform transformation
            return textRow;
        }
        public static void Train(IEnumerable<ParallelTextRow> parallelTextRows)
        {
            //train
        }
    }
}
