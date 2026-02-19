# AGENTS.md
# Guidance for agentic contributors to this repo.
# Keep this file in sync with repo conventions.

## Scope
- Repo: Umbraco Storage Providers (C# / .NET)
- Solution: `Umbraco.StorageProviders.sln`
- SDK: .NET 8 (see `global.json`)
- Analyzers: StyleCop + .editorconfig + .globalconfig

## Build / Lint / Test
Note: there are no test projects in the repo today. Commands below include
standard patterns for when tests are added.

### Restore
- `dotnet restore Umbraco.StorageProviders.sln --locked-mode`
  - Pipeline uses locked mode; ensure packages.lock.json exists if required.

### Build (also runs analyzers)
- `dotnet build Umbraco.StorageProviders.sln -c Release --no-restore -p:ContinuousIntegrationBuild=true`
- `dotnet build Umbraco.StorageProviders.sln -c Debug --no-restore`

### Pack
- `dotnet pack Umbraco.StorageProviders.sln -c Release --no-build --output ./artifacts/nupkg`

### Lint / code quality
- There is no separate lint command; analyzers run during build.
- To enforce formatting, use your editor + `.editorconfig` settings.

### Tests (when present)
- All tests: `dotnet test Umbraco.StorageProviders.sln -c Release`
- Single test project: `dotnet test path\to\Project.Tests.csproj -c Release`
- Single test (by fully qualified name):
  `dotnet test path\to\Project.Tests.csproj -c Release --filter "FullyQualifiedName=Namespace.Type.Method"`
- Single test (contains):
  `dotnet test path\to\Project.Tests.csproj -c Release --filter "FullyQualifiedName~TypeName"`

## Code Style (C#)
Source of truth: `.editorconfig` and `.globalconfig`.

### Imports / Usings
- Place `using` directives outside namespaces.
- Sort and group with `System.*` first.
- Avoid `using static` or alias ordering violations (StyleCop).

### Formatting
- Indent: 4 spaces for C# (2 for XML/props/targets/json/yaml).
- Braces required for multi-line and single-line blocks.
- New line before open brace for all constructs.
- New line before `else`, `catch`, `finally`.
- Preserve single-line blocks, but not single-line statements.
- Spaces: around binary operators; after commas; no extra inside parentheses.
- Trailing whitespace trimmed (except Markdown).

### Types and Nullability
- `Nullable` is enabled; treat nullable warnings as errors.
- Prefer built-in type keywords (`int`, `string`) over CLR types.
- `var`:
  - Use when type is apparent.
  - Avoid for built-in types or when not obvious.
- Prefer object/collection initializers when possible.
- Prefer pattern matching (`is` patterns, `switch` patterns).
- Prefer coalesce and null-propagation over manual null checks when suitable.

### Naming
- Namespaces, types, members: PascalCase.
- Interfaces: prefix with `I` (e.g. `IFileSystem`).
- Generic type parameters: prefix with `T` (e.g. `TOptions`).
- Parameters and locals: camelCase.
- Private instance fields: `_camelCase`.
- Constants and static readonly (public/protected/internal): PascalCase.
- Avoid non-private instance fields (StyleCop rule).
- File name should match primary type name.

### Access Modifiers
- Always specify accessibility (`public`, `internal`, etc.).
- Prefer `readonly` fields when possible.

### Expression Preferences
- Expression-bodied members are acceptable and encouraged.
- Prefer `throw` expressions and `ArgumentNullException.ThrowIfNull` guards.
- Prefer inferred tuple names and anonymous member names.
- Prefer compound assignments where applicable.

### Error Handling
- Use guard clauses at method entry for null arguments.
- Throw specific exceptions (`ArgumentNullException`, `InvalidOperationException`).
- Use exception filters for expected cases (e.g. status code checks).
- Return sentinel values only when the interface requires it.
- Avoid catching broad exceptions unless you rethrow or wrap with context.

### Documentation
- Public APIs typically include XML docs.
- `GenerateDocumentationFile` is enabled; keep docs accurate.
- Update README when public behavior changes.

## Project Conventions
- Target framework: `net8.0` (see `Directory.Build.props`).
- Implicit usings enabled.
- Analyzer mode: `All`.
- Nullable warnings are treated as errors (`WarningsAsErrors=Nullable`).
- Central package management via `Directory.Packages.props`.

## Repo Structure Notes
- Core library: `src\Umbraco.StorageProviders`
- Azure Blob provider: `src\Umbraco.StorageProviders.AzureBlob`
- ImageSharp cache provider: `src\Umbraco.StorageProviders.AzureBlob.ImageSharp`
- Example site: `examples\Umbraco.StorageProviders.AzureBlob.TestSite`

## Cursor / Copilot Rules
- No Cursor rules found in `.cursor/rules/` or `.cursorrules`.
- No Copilot instructions found in `.github/copilot-instructions.md`.

## When in doubt
- Follow existing patterns in source files.
- Keep changes small and focused.
- Prefer analyzer-compliant code over stylistic preferences.
