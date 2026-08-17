# Card Game Engine

## Setup

Prerequisites:
- [.NET 10 SDK](https://dotnet.microsoft.com/download) — install via the official Microsoft installer, not Homebrew
- [VS Code](https://code.visualstudio.com/) + the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension
- [CSharpier](https://marketplace.visualstudio.com/items?itemName=csharpier.csharpier-vscode) VS Code extension (format-on-save)

Install the VS Code extensions from the command line:
```
code --install-extension ms-dotnettools.csdevkit
code --install-extension csharpier.csharpier-vscode
```

Clone the repo, then from its root:
```
dotnet tool restore   # installs CSharpier as a local dotnet tool, pinned in dotnet-tools.json
dotnet restore         # restores NuGet packages
dotnet build           # builds all three projects
dotnet test            # runs the engine test suite
```

Open the folder in VS Code — `.vscode/settings.json` is checked in and already configures format-on-save, the C# formatter, and analyzer settings, so no extra editor configuration is needed.

## Commands
// Run all tests
`dotnet test` 

// Run client
dotnet run --project src/CardGame.Cli