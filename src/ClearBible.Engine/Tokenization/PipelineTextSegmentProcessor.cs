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
        public override TokensTextSegment Process(TokensTextSegment tokensTextSegment)
        {
			foreach (BaseTextSegmentProcessor processor in _processors)
				tokensTextSegment = processor.Process(tokensTextSegment);
			return tokensTextSegment;
		}
        public override void Train(ParallelTextCorpus parallelTextCorpus, ITextCorpus textCorpus)
        {
			foreach (BaseTextSegmentProcessor processor in _processors)
            {
				processor.Train(parallelTextCorpus, textCorpus);
			}
		}
	}
}
