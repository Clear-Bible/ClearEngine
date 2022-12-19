# Building and deploying Engine/Machine Nuget package

## Prerequisite

[Get nuget.exe and put it in your path](https://www.nuget.org/downloads)

## Steps

1. Open Powershell terminal in Engine's base solution directory.

2. Edit `ClearBible.Engine.nuspec` and change `<version>` to next version number

3. Execute `nuget pack .\ClearBible.Engine.nuspec`

4. Execute `nuget push .\ClearBible.Engine.1.0.1.nupkg -ApiKey <YOUR KEY> -Source https://nuget.pkg.github.com/clear-bible/index.json`

# Building and ddeploying Machine Nuget package

1. Change the version number in the Sil.Machine.csproj
1. Build the project
2. Open a terminal session in Sil.Machine/bin/debug
2. Execute ` nuget push .\Clear.SIL.Machine.<YOUR VERSION>.nupkg -ApiKey <YOUR KY> -Source https://nuget.pkg.github.com/clear-bible/index.json`

