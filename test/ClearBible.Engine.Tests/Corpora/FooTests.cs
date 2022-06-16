using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

using SIL.Machine.Corpora;
using SIL.Scripture;
using MediatR;
using ClearBible.Engine.Tests.Corpora.Handlers;
using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Alignment.DataServices.Corpora;
using ClearBible.Engine.Corpora;

namespace ClearBible.Engine.Tests.Corpora
{
    public class FooTests
    {
		[Fact]
		public async void FooTest()
		{
			IMediator mediator = new MediatorMock();
			var result = await mediator.Send(
				new CreateParallelTokenizedCorpusCommand(new ParallelCorpusVersionId(new Guid(), DateTime.UtcNow), new TokenizedCorpusId(new Guid()), new TokenizedCorpusId(new Guid())));
		}
	}
}