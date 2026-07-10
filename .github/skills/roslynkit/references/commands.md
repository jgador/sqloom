# RoslynKit Command Reference

This reference lists command names, usage strings, and options exposed by the installed RoslynKit CLI. For emitted `id:` values and documentation-comment ID prefix meanings, see [references/output.md](output.md). Agent routing guidance remains in [SKILL.md](../SKILL.md).

## Commands

- `version`: Print the installed RoslynKit version.
- `init`: Scaffold the RoslynKit coding-agent skill bundle into the current Git repository.
- `workspace`: List projects and repo-relevant documents loaded from a solution or project.
- `diagnostics`: Return source compiler diagnostics for the loaded target.
- `symbols`: Search source declarations by symbol name.
- `document-text`: Read the full text of one resolved document.
- `document-lines`: Read a bounded one-based line range from one resolved document.
- `document-symbols`: List declared symbols in one source or source-generated C# document.
- `definition`: Resolve a symbol selector or the symbol at a one-based line and column to source definitions.
- `type-definition`: Resolve the type of the symbol at a one-based line and column to source definitions.
- `references`: Find source references for a symbol selector or the symbol at a one-based line and column.
- `implementations`: Find implementations for a symbol selector or the symbol at a one-based line and column.
- `quick-info`: Return Roslyn quick info for the symbol at a one-based line and column.
- `signature-help`: Return Roslyn signature help for the position at a one-based line and column.
- `symbol-source`: Return the full declaration source text for a symbol selector.

## `version`

Print the installed RoslynKit version.

### Usage

```powershell
roslynkit version
roslynkit --version
```

### Options

No options.

## `init`

Scaffold the RoslynKit coding-agent skill bundle into the current Git repository.

### Usage

```powershell
roslynkit init [--agent <codex|claude|copilot|all>] [--overwrite]
```

### Options

- `--agent` `<agent>`: agent target: codex, claude, copilot, or all
- `--overwrite`: replace existing scaffolded skill files when content differs

## `workspace`

List projects and repo-relevant documents loaded from a solution or project.

### Usage

```powershell
roslynkit workspace --target <solution.slnx|solution.sln|project.csproj> [--include-generated] [--include-additional] [--include-analyzer-config]
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--include-generated`: include source-generated and generated source documents
- `--include-additional`: include additional files
- `--include-analyzer-config`: include analyzer config documents such as .editorconfig

## `diagnostics`

Return source compiler diagnostics for the loaded target.

### Usage

```powershell
roslynkit diagnostics --target <target> [--max-results <n>] [--include-hidden] [--include-generated]
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--max-results` `<n>`: maximum results to return
- `--include-hidden`: include hidden diagnostics
- `--include-generated`: include diagnostics from generated and obj documents

## `symbols`

Search source declarations by symbol name.

### Usage

```powershell
roslynkit symbols --target <target> --query <text> [--max-results <n>] [--case-sensitive] [--exact] [--kind <kind>]
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--query` / `-q` `<text>` (required): symbol name text to search for
- `--max-results` `<n>`: maximum results to return
- `--case-sensitive`: match query text case-sensitively
- `--exact`: match the declaration name exactly
- `--kind` `<kind>`: filter declarations by kind: namespace, type, member, method, property, field, event, class, interface, struct, enum, delegate

## `document-text`

Read the full text of one resolved document.

### Usage

```powershell
roslynkit document-text --target <target> --file <path> [--project <path>] [--tfm <framework>] [--document-kind <kind>]
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--file` / `-f` `<path>`: document file path in the loaded target
- `--project` `<path>`: owning project file path when a document path is ambiguous
- `--tfm` `<framework>`: target framework when a document path is ambiguous across project contexts
- `--document-kind` `<kind>`: document kind when a path maps to source, sourceGenerated, additional, or analyzerConfig

## `document-lines`

Read a bounded one-based line range from one resolved document.

### Usage

```powershell
roslynkit document-lines --target <target> --file <path> [--project <path>] [--tfm <framework>] [--document-kind <kind>] --start-line <n> --end-line <n>
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--file` / `-f` `<path>`: document file path in the loaded target
- `--project` `<path>`: owning project file path when a document path is ambiguous
- `--tfm` `<framework>`: target framework when a document path is ambiguous across project contexts
- `--document-kind` `<kind>`: document kind when a path maps to source, sourceGenerated, additional, or analyzerConfig
- `--start-line` `<n>` (required): one-based first document line
- `--end-line` `<n>` (required): one-based last document line

## `document-symbols`

List declared symbols in one source or source-generated C# document.

### Usage

```powershell
roslynkit document-symbols --target <target> --file <path> [--project <path>] [--tfm <framework>] [--document-kind <kind>]
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--file` / `-f` `<path>`: document file path in the loaded target
- `--project` `<path>`: owning project file path when a document path is ambiguous
- `--tfm` `<framework>`: target framework when a document path is ambiguous across project contexts
- `--document-kind` `<kind>`: document kind when a path maps to source, sourceGenerated, additional, or analyzerConfig

## `definition`

Resolve a symbol selector or the symbol at a one-based line and column to source definitions.

### Usage

```powershell
roslynkit definition --target <target> --file <path> [--project <path>] [--tfm <framework>] [--document-kind <kind>] --line <n> --column <n>
roslynkit definition --target <target> --symbol <selector>
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--file` / `-f` `<path>`: document file path in the loaded target
- `--project` `<path>`: owning project file path when a document path is ambiguous
- `--tfm` `<framework>`: target framework when a document path is ambiguous across project contexts
- `--document-kind` `<kind>`: document kind when a path maps to source, sourceGenerated, additional, or analyzerConfig
- `--line` `<n>`: one-based source line
- `--column` `<n>`: one-based source column
- `--symbol` `<selector>`: documentation-comment ID or qualified symbol name

## `type-definition`

Resolve the type of the symbol at a one-based line and column to source definitions.

### Usage

```powershell
roslynkit type-definition --target <target> --file <path> [--project <path>] [--tfm <framework>] [--document-kind <kind>] --line <n> --column <n>
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--file` / `-f` `<path>`: document file path in the loaded target
- `--project` `<path>`: owning project file path when a document path is ambiguous
- `--tfm` `<framework>`: target framework when a document path is ambiguous across project contexts
- `--document-kind` `<kind>`: document kind when a path maps to source, sourceGenerated, additional, or analyzerConfig
- `--line` `<n>` (required): one-based source line
- `--column` `<n>` (required): one-based source column

## `references`

Find source references for a symbol selector or the symbol at a one-based line and column.

### Usage

```powershell
roslynkit references --target <target> --file <path> [--project <path>] [--tfm <framework>] [--document-kind <kind>] --line <n> --column <n> [--max-results <n>]
roslynkit references --target <target> --symbol <selector> [--max-results <n>]
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--file` / `-f` `<path>`: document file path in the loaded target
- `--project` `<path>`: owning project file path when a document path is ambiguous
- `--tfm` `<framework>`: target framework when a document path is ambiguous across project contexts
- `--document-kind` `<kind>`: document kind when a path maps to source, sourceGenerated, additional, or analyzerConfig
- `--line` `<n>`: one-based source line
- `--column` `<n>`: one-based source column
- `--symbol` `<selector>`: documentation-comment ID or qualified symbol name
- `--max-results` `<n>`: maximum results to return

## `implementations`

Find implementations for a symbol selector or the symbol at a one-based line and column.

### Usage

```powershell
roslynkit implementations --target <target> --file <path> [--project <path>] [--tfm <framework>] [--document-kind <kind>] --line <n> --column <n> [--max-results <n>]
roslynkit implementations --target <target> --symbol <selector> [--max-results <n>]
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--file` / `-f` `<path>`: document file path in the loaded target
- `--project` `<path>`: owning project file path when a document path is ambiguous
- `--tfm` `<framework>`: target framework when a document path is ambiguous across project contexts
- `--document-kind` `<kind>`: document kind when a path maps to source, sourceGenerated, additional, or analyzerConfig
- `--line` `<n>`: one-based source line
- `--column` `<n>`: one-based source column
- `--symbol` `<selector>`: documentation-comment ID or qualified symbol name
- `--max-results` `<n>`: maximum results to return

## `quick-info`

Return Roslyn quick info for the symbol at a one-based line and column.

### Usage

```powershell
roslynkit quick-info --target <target> --file <path> [--project <path>] [--tfm <framework>] [--document-kind <kind>] --line <n> --column <n>
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--file` / `-f` `<path>`: document file path in the loaded target
- `--project` `<path>`: owning project file path when a document path is ambiguous
- `--tfm` `<framework>`: target framework when a document path is ambiguous across project contexts
- `--document-kind` `<kind>`: document kind when a path maps to source, sourceGenerated, additional, or analyzerConfig
- `--line` `<n>` (required): one-based source line
- `--column` `<n>` (required): one-based source column

## `signature-help`

Return Roslyn signature help for the position at a one-based line and column.

### Usage

```powershell
roslynkit signature-help --target <target> --file <path> [--project <path>] [--tfm <framework>] [--document-kind <kind>] --line <n> --column <n>
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--file` / `-f` `<path>`: document file path in the loaded target
- `--project` `<path>`: owning project file path when a document path is ambiguous
- `--tfm` `<framework>`: target framework when a document path is ambiguous across project contexts
- `--document-kind` `<kind>`: document kind when a path maps to source, sourceGenerated, additional, or analyzerConfig
- `--line` `<n>` (required): one-based source line
- `--column` `<n>` (required): one-based source column

## `symbol-source`

Return the full declaration source text for a symbol selector.

### Usage

```powershell
roslynkit symbol-source --target <target> --symbol <selector>
```

### Options

- `--target` / `-t` `<target>` (required): solution or project file to load
- `--symbol` `<selector>` (required): documentation-comment ID or qualified symbol name
