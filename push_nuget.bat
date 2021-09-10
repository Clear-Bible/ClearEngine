@echo off
set version=1.0.1

nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.API\bin\Debug\ClearBible.Clear3.Api.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Clear3\bin\Debug\ClearBible.Clear3.Clear3.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Clear3Service\bin\Debug\ClearBible.Clear3.Clear3Service.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Impl.AutoAlign\bin\Debug\ClearBible.Clear3.Impl.AutoAlign.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Impl.ImportExportService\bin\Debug\ClearBible.Clear3.Impl.ImportExportService.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Impl.Miscellaneous\bin\Debug\ClearBible.Clear3.Impl.Miscellaneous.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Impl.Persistence\bin\Debug\ClearBible.Clear3.Impl.Persistence.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Impl.ResourceService\bin\Debug\ClearBible.Clear3.Impl.ResourceService.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Impl.Segmenter\bin\Debug\ClearBible.Clear3.Impl.Segmenter.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Impl.SMTService\bin\Debug\ClearBible.Clear3.Impl.SMTService.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Impl.TreeService\bin\Debug\ClearBible.Clear3.Impl.TreeService.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Impl.Utility\bin\Debug\ClearBible.Clear3.Impl.Utility.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.Models\bin\Debug\ClearBible.Clear3.Models.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.SubTasks\bin\Debug\ClearBible.Clear3.SubTasks.%version%.nupkg"
nuget.exe push -Source "AlignmentFormats" -ApiKey az "D:\Projects-GBI\ClearEngine3\src\ClearBible.Clear3.TransModels\bin\Debug\ClearBible.Clear3.TransModels.%version%.nupkg"

pause