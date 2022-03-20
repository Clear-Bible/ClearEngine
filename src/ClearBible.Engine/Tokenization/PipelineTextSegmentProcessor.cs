﻿using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Tokenization
{
    public class PipelineTextSegmentProcessor : ITextSegmentProcessor
	{
		private readonly ITextSegmentProcessor[] _processors;

		public PipelineTextSegmentProcessor(IEnumerable<ITextSegmentProcessor> processors)
		{
			_processors = processors.ToArray();
		}

        public TokensTextSegment Process(TokensTextSegment tokensTextSegment)
        {
			foreach (ITextSegmentProcessor processor in _processors)
				tokensTextSegment = processor.Process(tokensTextSegment);
			return tokensTextSegment;
		}
    }
}
