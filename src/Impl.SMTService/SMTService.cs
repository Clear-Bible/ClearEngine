using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

using ClearBible.Clear3.API;

using BuildTransModels = TransModels.BuildTransModels;



namespace ClearBible.Clear3.Impl.SMTService
{
    public class SMTService : ISMTService
    {
        public (TranslationModel, AlignmentModel) DefaultSMT(
            ParallelCorpora parallelCorpora,
            string runSpec = "1:10;H:5",
            double epsilon = 0.1)
        {
            string workFolderPath = Path.Combine(
                    Path.GetTempPath(),
                    Path.GetRandomFileName());
            Directory.CreateDirectory(workFolderPath);

            string tempPath(string name)
                => Path.Combine(workFolderPath, name);
            string
                tempSourcePath = tempPath("source"),
                tempTargetPath = tempPath("target"),
                tempSourceIdPath = tempPath("sourceId"),
                tempTargetIdPath = tempPath("targetId"),
                tempTransModelPath = tempPath("transModel"),
                tempAlignModelPath = tempPath("alignModel");

            using (StreamWriter sw =
                        new StreamWriter(tempSourcePath, false, Encoding.UTF8))
            {
                foreach (ZonePair zp in parallelCorpora.List)
                {
                    sw.WriteLine(string.Join(" ",
                        zp.SourceZone.List.Select(s => s.Lemma.Text)));
                }
            }

            using (StreamWriter sw =
                        new StreamWriter(tempSourceIdPath, false, Encoding.UTF8))
            {
                foreach (ZonePair zp in parallelCorpora.List)
                {
                    sw.WriteLine(string.Join(" ",
                        zp.SourceZone.List.Select(s => $"x_{s.SourceID.AsCanonicalString}")));
                }
            }

            using (StreamWriter sw =
                        new StreamWriter(tempTargetPath, false, Encoding.UTF8))
            {
                foreach (ZonePair zp in parallelCorpora.List)
                {
                    sw.WriteLine(string.Join(" ",
                        zp.TargetZone.List.Select(t => t.TargetText.Text.ToLower())));
                }
            }

            using (StreamWriter sw =
                        new StreamWriter(tempTargetIdPath, false, Encoding.UTF8))
            {
                foreach (ZonePair zp in parallelCorpora.List)
                {
                    sw.WriteLine(string.Join(" ",
                        zp.TargetZone.List.Select(t => $"x_{t.TargetID.AsCanonicalString}")));
                }
            }

            BuildTransModels.BuildModels(
                    tempSourcePath,
                    tempTargetPath,
                    tempSourceIdPath,
                    tempTargetIdPath,
                    runSpec,
                    epsilon,
                    tempTransModelPath,
                    tempAlignModelPath);

            ImportExportService.ImportExportService importExportService =
                new ImportExportService.ImportExportService();

            TranslationModel transModel =
                importExportService.ImportTranslationModel(tempTransModelPath);

            AlignmentModel alignModel =
                importExportService.ImportAlignmentModel(tempAlignModelPath);

            Directory.Delete(workFolderPath, true);

            return (transModel, alignModel);
        }
    }
}
