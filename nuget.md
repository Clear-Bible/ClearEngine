# Building and deploying Engine/Machine Nuget package

## Prerequisite

[Get nuget.exe and put it in your path](https://www.nuget.org/downloads)

## Steps

1. Open Powershell terminal in Engine's base solution directory.

2. Edit `ClearBible.Engine.nuspec` and change `<version>` to next version number

3. Execute `nuget pack .\ClearBible.Engine.nuspec`

4. Execute `nuget push .\ClearBible.Engine.1.0.1.nupkg -ApiKey <YOUR KEY> -Source https://nuget.pkg.github.com/clear-bible/index.json`
