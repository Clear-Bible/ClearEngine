using System;
using System.IO;


namespace ClearBible.Engine.Tests.Corpora
{
    internal static class TestDataHelpers
    {
        public static readonly string TestDataPath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "Corpora", "TestData");
        public static readonly string UsfmTestProjectPath = Path.Combine(TestDataPath, "usfm", "Tes");
    }
}
