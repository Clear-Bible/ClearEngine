using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

using ClearBible.Clear3.API;

using BuildTransModels = TransModels.BuildTransModels;



namespace ClearBible.Clear3.Impl.SMTService
{
    /// <summary>
    /// (Implementation of ISMTService.)
    /// </summary>
    /// 
    public class SMTService : ISMTService
    {
        /// <summary>
        /// Implementation of ISMTService.DefaultSMT.
        /// The code here wraps the statistical machine translation
        /// functions from Clear2, which have not otherwise been touched.
        ///
        /// 2022.03.25 CL: Changed to not use epsilon since it is now encoded in runSpec.
        /// Also, runSpec has changed to <model>-<iterations>-<threshold>-<heuristic>
        /// </summary>
        /// 
        public (TranslationModel, AlignmentModel) DefaultSMT(
            ParallelCorpora parallelCorpora,
            string runSpec = "FastAlign-5-0.1-Intersection")
        {
            // Create a temporary work folder.

            string workFolderPath = Path.Combine(
                    Path.GetTempPath(),
                    Path.GetRandomFileName());
            Directory.CreateDirectory(workFolderPath);


            // Prepare to use files in the temporary work folder.

            string tempPath(string name)
                => Path.Combine(workFolderPath, name);
            string
                tempSourcePath = tempPath("source"),
                tempTargetPath = tempPath("target"),
                tempSourceIdPath = tempPath("sourceId"),
                tempTargetIdPath = tempPath("targetId"),
                tempTransModelPath = tempPath("transModel"),
                tempAlignModelPath = tempPath("alignModel");


            // Prepare input files in the temporary folder from the
            // input data.

            // Note that the wrapped code only uses the source and target
            // IDs from the sourceId and targetId files, and does not care
            // about the lemma or the target text that would be there in
            // Clear2.

            using (StreamWriter sw =
                        new StreamWriter(tempSourcePath, false, Encoding.UTF8))
            {
                foreach (ZonePair zp in parallelCorpora.List)
                {
                    sw.WriteLine(string.Join(" ",
                        zp.SourceZone.List.Select(s => s.SourceLemma.Text)));
                }
            }

            using (StreamWriter sw =
                        new StreamWriter(tempSourceIdPath, false, Encoding.UTF8))
            {
                foreach (ZonePair zp in parallelCorpora.List)
                {
                    sw.WriteLine(string.Join(" ",
                        // zp.SourceZone.List.Select(s => $"x_{s.SourceID.AsCanonicalString}")));
                        zp.SourceZone.List.Select(s => s.SourceID.AsCanonicalString)));
                }
            }

            using (StreamWriter sw =
                        new StreamWriter(tempTargetPath, false, Encoding.UTF8))
            {
                foreach (ZonePair zp in parallelCorpora.List)
                {
                    sw.WriteLine(string.Join(" ",
                        zp.TargetZone.List.Select(t => t.TargetLemma.Text)));
                }
            }

            using (StreamWriter sw =
                        new StreamWriter(tempTargetIdPath, false, Encoding.UTF8))
            {
                foreach (ZonePair zp in parallelCorpora.List)
                {
                    sw.WriteLine(string.Join(" ",
                        // zp.TargetZone.List.Select(t => $"x_{t.TargetID.AsCanonicalString}")));
                        zp.TargetZone.List.Select(t => t.TargetID.AsCanonicalString)));
                }
            }

            // Need to eventually pass this in as a parameter
            string python = "C:\\Program Files\\Python310\\python.exe";

            // Train the model and write out the translation model
            // and alignment model.
            BuildTransModels.BuildModels(
                    tempSourcePath,
                    tempTargetPath,
                    tempSourceIdPath,
                    tempTargetIdPath,
                    runSpec,
                    tempTransModelPath,
                    tempAlignModelPath,
                    python);


            // Import the translation model and alignment model from the
            // temporary files that received the data.

            ImportExportService.ImportExportService importExportService =
                new ImportExportService.ImportExportService();

            TranslationModel transModel =
                importExportService.ImportTranslationModel(tempTransModelPath);

            AlignmentModel alignModel =
                importExportService.ImportAlignmentModel(tempAlignModelPath);


            // Delete the temporary work folder and its contents.

            Directory.Delete(workFolderPath, true);


            return (transModel, alignModel);
        }
    }
}
