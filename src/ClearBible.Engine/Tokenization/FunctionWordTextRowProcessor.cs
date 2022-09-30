using SIL.Machine.Corpora;


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
