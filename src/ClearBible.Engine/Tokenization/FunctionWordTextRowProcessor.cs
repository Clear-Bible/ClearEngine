using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;


namespace ClearBible.Engine.Tokenization
{
    public class FunctionWordTextRowProcessor : IRowFilter<TextRow>
    {
        public bool Process(TextRow textRow)
        {
            // perform transformation
            return true;
        }
        public static void Train(IEnumerable<ParallelTextRow> parallelTextRows)
        {
            //train
        }
    }
}
