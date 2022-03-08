using SIL.Machine.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Tokenization
{
    public class ParallelTextSegmentProcessor : ITextSegmentProcessor
	{
		private readonly ITextSegmentProcessor[] _processors;

		public ParallelTextSegmentProcessor(IEnumerable<ITextSegmentProcessor> processors)
		{
			_processors = processors.ToArray();
		}

        public TextSegment Process(TextSegment textSegment)
        {
			foreach (ITextSegmentProcessor processor in _processors)
				textSegment = processor.Process(textSegment);
			return textSegment;
		}
    }
}
