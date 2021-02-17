using System;
using Xunit;

namespace TransModels.Tests
{
    public class BuildTransModelTests
    {
        [Fact]
        public void Test1()
        {
            Console.WriteLine("\n>>> Running Test : GetAlignmentModel");

            // arrange
            var builder = new BuildTransModels();

            // act
            var mssg = BuildTransModels.GetTestMessage();
            Console.WriteLine($"The message from builder:{mssg}");

            // assert

        }
    }
}
