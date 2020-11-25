using System;
using System.Linq;

using ClearBible.Clear3.API;
using ClearBible.Clear3.Service;

namespace ClearBible.Clear3.Subtasks
{
    public class GetStandardTreeServiceSubtask
    {
        public static ITreeService Run(string resourceFolder)
        {
            IClear30ServiceAPI clearService = Clear30Service.FindOrCreate();

            IResourceService resourceService = clearService.ResourceService;
            resourceService.SetLocalResourceFolder(resourceFolder);

            Uri treebankUri =
                new Uri("https://id.clear.bible/treebank/Clear3Dev");

            if (!resourceService.QueryLocalResources()
                .Any(r => r.Id.Equals(treebankUri)))
            {
                resourceService.DownloadResource(treebankUri);
            }

            return
                resourceService.GetTreeService(treebankUri);

            // Proposal: URIs of the form http://id.clear.bible/... serve
            // metadata about the resource, either as RDF or HTML.
            // See also: https://www.w3.org/TR/cooluris/
            // The metadata also points to a location in Github with
            // the gzipped data for the resource.
            // Clear3 uses the machine-readable metadata to download
            // resources when so requested.
        }
    }
}
