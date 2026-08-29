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
```bash
dotnet tool restore    # installs CSharpier as a local dotnet tool, pinned in dotnet-tools.json
dotnet restore         # restores NuGet packages
dotnet build           # builds all three projects
dotnet test            # runs the engine test suite
```

Open the folder in VS Code — `.vscode/settings.json` is checked in and already configures format-on-save, the C# formatter, and analyzer settings, so no extra editor configuration is needed.

## Commands
```bash
dotnet test                             # Run all tests
dotnet run --project src/CardGame.Cli   # Run client
dotnet run --project src/CardGame.Web # Run web client
dotnet watch run --project src/CardGame.Web # Run web client (watch mode)
```

### Create a new section, add it to the solution, and create references
```c#
dotnet new classlib -n CardGame{NewSection} -o src/CardGame{NewSection}
dotnet sln CardGame.slnx add src/CardGame{NewSection}/CardGame{NewSection}.csproj
dotnet add src/CardGame.{NewSection}/CardGame.{NewSection}.csproj reference src/CardGame.Engine/CardGame.Engine.csproj
dotnet add src/CardGame.Cli/CardGame.Cli.csproj reference src/CardGame.{NewSection}/CardGame.{NewSection}.csproj
dotnet build
```

## Add new section to tests
```c#
dotnet new xunit -n CardGame.{NwSection}.Tests -o tests/CardGame.{NwSection}.Tests
dotnet sln CardGame.slnx add tests/CardGame.{NwSection}.Tests/CardGame.{NwSection}.Tests.csproj
dotnet add tests/CardGame.{NwSection}.Tests/CardGame.{NwSection}.Tests.csproj reference src/CardGame.{NwSection}/CardGame.{NwSection}.csproj
```