# Building and deploying Engine/Machine Nuget package

## Prerequisite

[Get nuget.exe and put it in your path `C:\Windows\System32\`](https://www.nuget.org/downloads)
Note that the nuget.exe version MUST be 5.7.3 otherwise it won't work. You can download it from here:
https://dist.nuget.org/win-x86-commandline/v5.7.3/nuget.exe

# Building and deploying Machine Nuget package

## Steps

1. Open Powershell terminal in Engine's base solution directory.		
2. Switch to DEBUG configuration
3. Build the solution


4. Change the version number in the SIL.Machine.csproj by editing AssemblyInfo.props under he Imports folder.
5. Build the project
6. Open a terminal session in SIL.Machine/bin/debug which will contain the nuget package
7. Execute `nuget push .\Clear.SIL.Machine.<YOUR VERSION>.nupkg -ApiKey <YOUR KEY> -Source https://nuget.pkg.github.com/clear-bible/index.json`



## Steps

1. Open Powershell terminal in Engine's base solution directory.		
2. Switch to Release configuration
3. Build the solution

4. Edit `ClearBible.Engine.nuspec` and change `<version>` to next version number

5. Execute `nuget pack .\ClearBible.Engine.nuspec` to just create the *.nupkg
6. Execute `nuget pack .\ClearBible.Engine.nuspec -Symbols -SymbolPackageFormat snupkg` to create both the *.nupkg and a *.snupkg
7. Execute `nuget push .\ClearBible.Engine.X.X.X.nupkg -ApiKey <YOUR KEY> -Source https://nuget.pkg.github.com/clear-bible/index.json`
8. To publish the symbol package, execute `nuget push .\ClearBible.Engine.X.X.X.snupkg -ApiKey <YOUR KEY> -Source https://nuget.pkg.github.com/clear-bible/index.json`

