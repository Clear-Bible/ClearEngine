using ClearBible.MaculaPropertiesSources.ETL.PronominalReferences;
using System.Reflection;

if (args.Length == 0)
{
    Console.WriteLine("Starting...");
    new PronominalReferencesFromExtendedVerseTrees().Process();
}
else if (args.Length == 1)
{
    Console.WriteLine("Starting...");
    new PronominalReferencesFromExtendedVerseTrees(args[0]).Process();
}
else if (args.Length == 2)
{
    Console.WriteLine("Starting...");
    new PronominalReferencesFromExtendedVerseTrees(args[0], args[1]).Process();
}
else if (args.Length > 2)
{
    Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} [loadFilePath] [extractDirectoryPath]");
    return;
}
Console.WriteLine("...comleted.");
