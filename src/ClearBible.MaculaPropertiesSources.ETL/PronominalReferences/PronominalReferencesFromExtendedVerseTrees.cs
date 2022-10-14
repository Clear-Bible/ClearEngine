using ClearBible.Engine.SyntaxTree.Corpora;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ClearBible.MaculaPropertiesSources.ETL.PronominalReferences
{
    internal static class Extensions
    {
        public static List<string>? PronominalReferencesAsStrings(this XElement leaf)
        {
            var reference = leaf.Attribute("Ref")?.Value;
            if (reference == null || reference.Length < 3)
                return null;

            return reference.Substring(1, reference.Length - 2).Split().ToList();
        }

        public static List<string>? VerbSubjectReferencesAsStrings(this XElement leaf)
        {
            var reference = leaf.Attribute("SubjRef")?.Value;
            if (reference == null || reference.Length < 3)
                return null;

            return reference.Substring(1, reference.Length - 2).Split().ToList();
        }
        public static string? Notes(this XElement leaf)
        {
            var notes = leaf.Attribute("Notes")?.Value;
            if (notes == null || notes.Length == 0)
                return null;

            return notes;
        }
    }
    internal class PronominalReferencesFromExtendedVerseTrees : ExtractTransformLoadBase<PronominalReferences>
    {
        private readonly string _extractDirectoryPath;

        private readonly string _loadFilePath;

        private static readonly string DefaultExtractDirecdtoryPath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "PronominalReferences", "extendedversetrees");
        
        private static readonly string DefaultLoadFilePath = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "ClearBible.Macula.PropertiesSources", "Corpora", "pronominalreferences", "PronominalReferences.xml");

        internal PronominalReferencesFromExtendedVerseTrees(string? loadFilePath = null, string? extractDirectoryPath = null)
        {
            _loadFilePath = loadFilePath ?? DefaultLoadFilePath;
            _extractDirectoryPath = extractDirectoryPath ?? DefaultExtractDirecdtoryPath;
        }
        protected override IEnumerable<PronominalReferences> Extract()
        {
            return Directory.EnumerateFiles(_extractDirectoryPath, "*.xml")
                .SelectMany(fileName => XElement
                    .Load(Path.Combine(_extractDirectoryPath, fileName))
                    .Descendants()
                    .Where(e => e.FirstNode is XText)
                    //.Where(e => e.PronominalReference() != null || e.VerbSubjectReference() != null)
                    .Select(leaf => new PronominalReferences(
                        leaf.TokenId(),
                        leaf.MorphId(),
                        leaf.PronominalReferencesAsStrings()?.ToList(),
                        leaf.VerbSubjectReferencesAsStrings()?.ToList(),
                        leaf.Surface(),
                        leaf.English(),
                        leaf.Notes()
                    ))
                )
                .OrderBy(o => o.TokenId);
        }

        protected override IEnumerable<PronominalReferences> Transform(IEnumerable<PronominalReferences> objs)
        {
            var allObjsDict = objs.ToDictionary(pr => pr.MorphId!);
            
            return allObjsDict
                .Where(kvp => kvp.Value.PronominalReferencesAsStrings != null || kvp.Value.VerbSubjectReferencesAsStrings != null)
                .Select(kvp =>
                {
                    kvp.Value.PronominalReferenceDetails = kvp.Value.PronominalReferencesAsStrings != null ?
                        kvp.Value.PronominalReferencesAsStrings
                            .Select(pr =>
                            {
                                var value = allObjsDict.GetValueOrDefault(pr);
                                if (value == null)
                                    Console.WriteLine($"Could not find PronominalReferencesAsStrings {pr} for morphId {kvp.Value.MorphId}. Setting DereferencedPronominalReference to null. ");
                                return value?.DeepCopyIntoPronominalReferenceDetails();
                            })
                            .ToList()
                        : null;
                    kvp.Value.VerbSubjectReferenceDetails = kvp.Value.VerbSubjectReferencesAsStrings != null ?
                        kvp.Value.VerbSubjectReferencesAsStrings
                            .Select(pr =>
                            {
                                var value = allObjsDict.GetValueOrDefault(pr);
                                if (value == null)
                                    Console.WriteLine($"Could not find VerbSubjectReferences {pr} for morphId {kvp.Value.MorphId}. Setting DereferencedVerbSubjectReference to null. ");
                                return value?.DeepCopyIntoPronominalReferenceDetails();
                            })
                            .ToList()
                        : null;
                    return kvp;
                })
                .Select(kvp => kvp.Value);
        }
        protected override void Load(IEnumerable<PronominalReferences> objs)
        {
            var array = objs.ToArray();

            var xmlWriterSettings = new XmlWriterSettings() { Indent = true };
            XmlSerializer serializer = new XmlSerializer(array.GetType());
            using (XmlWriter xmlWriter = XmlWriter.Create(_loadFilePath, xmlWriterSettings))
            {
                serializer.Serialize(xmlWriter, array);
            }


            /*
            XmlSerializer x = new XmlSerializer(array.GetType());
            string foo;
            using (var writer = new StringWriter())
            {
                x.Serialize(writer, array);
                foo = writer.ToString();
            }


            XmlSerializer y = new XmlSerializer(typeof(PronominalReferences[]));
            using (var fooReader = new StringReader(foo))
            {
                PronominalReferences[]? o = (PronominalReferences[]) y.Deserialize(fooReader)!;
            }
            */
        }
    }
}
 