# Building and deploying Engine/Machine Nuget package

## Prerequisite

[Get nuget.exe and put it in your path](https://www.nuget.org/downloads)

## Steps

1. Open Powershell terminal in Engine's base solution directory.

2. Edit `ClearBible.Engine.nuspec` and change `<version>` to next version number

3. Execute `nuget pack .\ClearBible.Engine.nuspec` to just create the *.nupkg
4. Execute `nuget pack .\ClearBible.Engine.nuspec -Symbols -SymbolPackageFormat snupkg` to create both the *.nupkg and a *.snupkg
5. Execute `nuget push .\ClearBible.Engine.X.X.X.nupkg -ApiKey <YOUR KEY> -Source https://nuget.pkg.github.com/clear-bible/index.json`
6. To publish the symbole package, execute `nuget push .\ClearBible.Engine.X.X.X.snupkg -ApiKey <YOUR KEY> -Source https://nuget.pkg.github.com/clear-bible/index.json`

# Building and deploying Machine Nuget package

1. Change the version number in the SIL.Machine.csproj by editing AssemblyInfo.props under he Imports folder.
1. Build the project
2. Open a terminal session in SIL.Machine/bin/debug
2. Execute `nuget push .\Clear.SIL.Machine.<YOUR VERSION>.nupkg -ApiKey <YOUR KEY> -Source https://nuget.pkg.github.com/clear-bible/index.json`
