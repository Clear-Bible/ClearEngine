using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;



namespace ClearBible.Engine.Tests.Corpora
{
    public class CorpusTests
    {
		protected readonly ITestOutputHelper output_;
		public CorpusTests(ITestOutputHelper output)
		{
			output_ = output;
		}

	}
}
