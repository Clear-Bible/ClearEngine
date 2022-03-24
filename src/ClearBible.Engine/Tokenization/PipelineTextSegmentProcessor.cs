using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Tokenization
{
    public class PipelineTextSegmentProcessor : BaseTextSegmentProcessor
	{
		private readonly BaseTextSegmentProcessor[] _processors;

		public PipelineTextSegmentProcessor(IEnumerable<BaseTextSegmentProcessor> processors)
		{
			_processors = processors.ToArray();
		}
        public override TokensTextRow Process(TokensTextRow tokensTextRow)
        {
			foreach (BaseTextSegmentProcessor processor in _processors)
				tokensTextRow = processor.Process(tokensTextRow);
			return tokensTextRow;
		}
        public override void Train(IEnumerable<ParallelTextRow> parallelTextRows, IEnumerable<TextRow> textRows)
        {
			foreach (BaseTextSegmentProcessor processor in _processors)
            {
				processor.Train(parallelTextRows, textRows);
			}
		}
	}
}
