using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;


namespace ClearBible.Clear3.Impl.ResourceService
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.TreeService;

    public class ResourceService : IResourceService
    {
        /// <summary>
        /// Implements IResourceService.SetLocalResourceFolder().
        /// </summary>
        /// 
        public void SetLocalResourceFolder(string path)
        {
            // Check that path is not blank.
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "resource directory path is blank or null");
            }

            ResourceFolder = path;

            // Create resource folder if it does not already exist.
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                dir.Create();
                WriteIndex(BuiltInResources.GetLocalResources());
            }
        }


        /// <summary>
        /// Implements IResourceService.QueryLocalResources().
        /// </summary>
        /// 
        public IEnumerable<LocalResource> QueryLocalResources()
        {
            CheckResourceFolder();

            // Read the index file from the resource folder to
            // obtain a list of LocalResource.
            string json = File.ReadAllText(IndexFile);

            return JsonConvert.DeserializeObject<List<LocalResource>>(json);
        }


        /// <summary>
        /// Implements IResourceService.DownloadResource().
        /// </summary>
        ///
        /// FIXME
        /// This method is supposed to go out and query the URI to get
        /// metadata about the resource, and then find the resource
        /// (probably on Github) and install it in the resource folder.
        /// But right now this method only works for:
        ///    https://id.clear.bible/treebank/Clear3Dev
        /// and installs this treebank into the resource folder from
        /// this source location:
        ///   ../TestSandbox1/SyntaxTrees
        ///   
        public void DownloadResource(Uri uri)
        {
            // Check that the URI is for the Clear3Dev treebank.
            //
            if (!uri.Equals("https://id.clear.bible/treebank/Clear3Dev"))
            {
                throw new NotImplementedException(
                    "prototype can only download Clear3Dev treebank resource");
            }

            // Locate the destination directory within the resource folder,
            // creating or re-creating it if necessary.
            //
            string destinationPath =
                Path.Combine(ResourceFolder, "treebank", "Clear3Dev");
            DirectoryInfo destinationDir = new DirectoryInfo(destinationPath);
            if (destinationDir.Exists)
            {
                destinationDir.Delete(recursive: true);
            }
            destinationDir.Create();

            // Locate the source directory for the Clear3Dev treebank.
            //
            string sourcePath =
                Path.Combine("..", "TestSandbox1", "SyntaxTrees");
            DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);

            // Copy each file in the source directory to the destination
            // directory.
            //
            foreach (FileInfo sourceFileInfo in sourceDir.EnumerateFiles())
            {
                string sourceFilePath = sourceFileInfo.FullName;
                string destinationFilePath =
                    Path.Combine(destinationPath, sourceFileInfo.Name);
                File.Copy(sourceFilePath, destinationFilePath);
            }

            // Add (or replace) the LocalResource record for
            // the Clear3Dev treebank in the index.
            //
            List<LocalResource> index =
                QueryLocalResources()
                .Where(e => !e.Id.Equals(uri))
                .Append(new LocalResource(
                    uri,
                    DateTime.Now,
                    true,
                    false,
                    "downloaded",
                    "internal treebank for Clear3 development"))
                .ToList();
            WriteIndex(index);
        }

        /// <summary>
        /// Implements IResourceService.GetTreeService().
        /// </summary>
        ///
        /// FIXME
        /// At present this method is only capable of getting
        /// the tree service for the Clear3Dev treebank.
        /// 
        public ITreeService GetTreeService(Uri treeResourceUri)
        {
            // Check that the URI is for the Clear3Dev treebank.
            //
            if (!treeResourceUri.Equals(
                "https://id.clear.bible/treebank/Clear3Dev"))
            {
                throw new NotImplementedException(
                    "prototype can only get the Clear3Dev tree service");
            }

            return new TreeService(
                Path.Combine(ResourceFolder, "treebank", "Clear3Dev"),
                BookNames.LoadBookNames3());
        }


        /// <summary>
        /// Path to the resource folder, or null if not yet set.
        /// </summary>
        /// 
        public string ResourceFolder { get; set; }

        public void CheckResourceFolder()
        {
            // Check that resource folder path is not null.
            if (ResourceFolder is null)
            {
                throw new InvalidOperationException(
                    "resource folder not initialized");
            }
        }

        /// <summary>
        /// Path to the index file within the resource folder.
        /// </summary>
        /// 
        public string IndexFile =>
            Path.Combine(ResourceFolder, "index");

        public Segmenter CreateSegmenter(Uri segmenterAlgorithmUri)
        {
            throw new NotImplementedException();
        }

        

        public Dictionary<string, string> GetStringsDictionary(Uri stringsDictionaryUri)
        {
            throw new NotImplementedException();
        }

        public HashSet<string> GetStringSet(Uri stringSetUri)
        {
            throw new NotImplementedException();
        }


        public Versification GetVersification(Uri versificationUri)
        {
            throw new NotImplementedException();
        }


        


        /// <summary>
        /// Write index, consisting of a list of LocalResource,
        /// to the index file in the resource directory.
        /// </summary>
        /// 
        public void WriteIndex(List<LocalResource> index)
        {
            CheckResourceFolder();

            string json = JsonConvert.SerializeObject(
                index,
                Formatting.Indented);

            File.WriteAllText(IndexFile, json);
        }
    }


    /// <summary>
    /// Information about built-in resources.
    /// </summary>
    /// 
    public class BuiltInResources
    {
        /// <summary>
        /// Prefix for URI of a built-in resource.
        /// </summary>
        /// 
        public static string UriBuiltInPrefix =
            "https://id.clear.bible/clear3builtin/";

        /// <summary>
        /// Database of information about built-in resources:
        /// (1) tail of the URI,
        /// (2) description,
        /// (3) object that internally represents the resource.
        /// </summary>
        /// FIXME (3) not yet implemented
        /// 
        public static Tuple<string, string, object>[] Db =
            new Tuple<string, string, object>[]
            {
                Tuple.Create(
                    "punctuation1",
                    "Clear3 default punctuation v1",
                    new object()),
                Tuple.Create(
                    "stopwords1",
                    "Clear3 default stopwords v1",
                    new object()),
                Tuple.Create(
                    "segmentation1",
                    "Clear3 builtin segmentation algorithm v1",
                    new object()),
                Tuple.Create(
                    "functionWords/biblical1",
                    "Clear3 default function words for Biblical languages v1",
                    new object()),
                Tuple.Create(
                    "functionWords/english1",
                    "Clear3 default function words for English v1",
                    new object())
            };


        /// <summary>
        /// Produce a list of LocalResource that describes the built-in
        /// resources, suitable for initializing an index file in a fresh
        /// resource directory.
        /// </summary>
        /// 
        public static List<LocalResource> GetLocalResources() =>
            Db.Select(e => new LocalResource(
                new Uri($"{UriBuiltInPrefix}{e.Item1}"),
                DateTime.Now,
                true,
                true,
                "Clear3 built-in resource",
                e.Item2))
            .ToList();
    }


    // FIXME
    // Temporary measure to get the prototype working, just copied from
    // CLEAR2.
    // It seems like these book names, which are being used to find the files
    // in a treebank, need to become part of the metadata associated with
    // the treebank somehow.
    //
    public class BookNames
    {
        public static Dictionary<string, string> LoadBookNames3()
        {
            Dictionary<string, string> bookNames2 = new Dictionary<string, string>();

            bookNames2.Add("01", "gn");
            bookNames2.Add("02", "ex");
            bookNames2.Add("03", "lv");
            bookNames2.Add("04", "nu");
            bookNames2.Add("05", "dt");
            bookNames2.Add("06", "js");
            bookNames2.Add("07", "ju");
            bookNames2.Add("08", "ru");
            bookNames2.Add("09", "1s");
            bookNames2.Add("10", "2s");
            bookNames2.Add("11", "1k");
            bookNames2.Add("12", "2k");
            bookNames2.Add("13", "1c");
            bookNames2.Add("14", "2c");
            bookNames2.Add("15", "er");
            bookNames2.Add("16", "ne");
            bookNames2.Add("17", "es");
            bookNames2.Add("18", "jb");
            bookNames2.Add("19", "ps");
            bookNames2.Add("20", "pr");
            bookNames2.Add("21", "ec");
            bookNames2.Add("22", "ca");
            bookNames2.Add("23", "is");
            bookNames2.Add("24", "je");
            bookNames2.Add("25", "lm");
            bookNames2.Add("26", "ek");
            bookNames2.Add("27", "da");
            bookNames2.Add("28", "ho");
            bookNames2.Add("29", "jl");
            bookNames2.Add("30", "am");
            bookNames2.Add("31", "ob");
            bookNames2.Add("32", "jn");
            bookNames2.Add("33", "mi");
            bookNames2.Add("34", "na");
            bookNames2.Add("35", "hb");
            bookNames2.Add("36", "zp");
            bookNames2.Add("37", "hg");
            bookNames2.Add("38", "zc");
            bookNames2.Add("39", "ma");
            bookNames2.Add("40", "Mat");
            bookNames2.Add("41", "Mrk");
            bookNames2.Add("42", "Luk");
            bookNames2.Add("43", "Jhn");
            bookNames2.Add("44", "Act");
            bookNames2.Add("45", "Rom");
            bookNames2.Add("46", "1Co");
            bookNames2.Add("47", "2Co");
            bookNames2.Add("48", "Gal");
            bookNames2.Add("49", "Eph");
            bookNames2.Add("50", "Php");
            bookNames2.Add("51", "Col");
            bookNames2.Add("52", "1Th");
            bookNames2.Add("53", "2Th");
            bookNames2.Add("54", "1Tm");
            bookNames2.Add("55", "2Tm");
            bookNames2.Add("56", "Tit");
            bookNames2.Add("57", "Phm");
            bookNames2.Add("58", "Heb");
            bookNames2.Add("59", "Jms");
            bookNames2.Add("60", "1Pe");
            bookNames2.Add("61", "2Pe");
            bookNames2.Add("62", "1Jn");
            bookNames2.Add("63", "2Jn");
            bookNames2.Add("64", "3Jn");
            bookNames2.Add("65", "Jud");
            bookNames2.Add("66", "Rev");

            return bookNames2;
        }
    }
}
