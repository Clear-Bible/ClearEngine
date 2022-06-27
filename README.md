# ClearEngine

A set of classes extending SIL's Machine that includes:

- Custom versification,
- Extensions to tokenization that include adding unique token ids and other data to text tokens,
- Manuscript syntax tree corpus,
- Syntax tree-based alignment refining technology.

## Setup

1. Clone this repository to `directory`.
2. From `directory`, clone [fork of SIL's Machine](https://github.com/russellmorley/machine.git), `git clone https://github.com/russellmorley/machine.git`.
3. Go to github and create a new application password, giving it permissions to write/read packages.
4. Change to ClearEngine directory, `cd ClearEngine`, and create new file named `nuget.config`:

```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <clear />
        <add key="ClearBible" value="https://nuget.pkg.github.com/clear-bible/index.json" />
        <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    </packageSources>
    <packageSourceMapping>
    <packageSource key="nuget.org">
        <package pattern="*" />
    </packageSource>
    <packageSource key="ClearBible">
        <package pattern="Clear*" />
    </packageSource>
    </packageSourceMapping>
        <packageSourceCredentials>
            <ClearBible>
                <add key="Username" value="GITHUB_USERNAME" />
                <add key="ClearTextPassword" value="GITHUB_APP_PASSWORD_WITH_PACKAGE_READ_WRITE_PERMISSIONS" />
            </ClearBible>
        </packageSourceCredentials>
    </configuration>
```

## Run

Open solution in Visual Studio. Open *Text Explorer* and run ClearBible tests.

