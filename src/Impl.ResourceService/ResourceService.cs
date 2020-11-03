using System;
using System.IO;
using System.Linq;

using Newtonsoft.Json;


namespace ClearBible.Clear3.Impl.ResourceService
{
    using System.Collections.Generic;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using ClearBible.Clear3.API;

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


        public ITreeService GetTreeService(Uri treeResourceUri)
        {
            throw new NotImplementedException();
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
}
