using System;

// Work in progress.
// Clear3 is currently a prototype.
// Many more unit tests are desirable.

namespace ClearBible.Clear3.UnitTest.Impl.AutoAlign
{
    class Program
    {
        static void Main(string[] args)
        {
            UnitTest_TargetRange.Basic();
            UnitTest_Candidate.Basic();

            Console.WriteLine("OK");
        }
    }
}
