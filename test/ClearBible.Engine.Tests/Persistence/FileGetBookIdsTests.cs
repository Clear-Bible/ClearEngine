using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClearBible.Engine.Persistence;
using SIL.Machine.FiniteState;
using Xunit;
using Xunit.Abstractions;


namespace ClearBible.Engine.Tests.Persistence
{
    public class FileGetBookIdsTests
    {
        protected readonly ITestOutputHelper _output;

        public FileGetBookIdsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void PossiblyConcurrentDictionaryAccess1()
        {
            Assert.Equal(123, FileGetBookIds.BookIds.Count());
        }

        [Fact]
        public void PossiblyConcurrentDictionaryAccess2()
        {
            Assert.Equal(123, FileGetBookIds.BookIds.Count());
        }

        [Fact]
        public void PossiblyConcurrentDictionaryAccess3()
        {
            Assert.Equal(123, FileGetBookIds.BookIds.Count());
        }
    }
}