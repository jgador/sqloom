# Markdown Output Format

This document is the output contract for RoslynKit. Every command writes this token-saving markdown-flavored text to stdout; there is no JSON output.

The output stays close to GitHub Flavored Markdown, but it uses only the smallest useful subset for coding-agent consumption.

## Goals

- Keep output compact enough for coding-agent context windows.
- Preserve exact source text in fenced code blocks.
- Keep output deterministic: stable ordering, stable labels, stable locations, and stable section order.
- Keep failures machine-detectable through exit codes instead of output parsing.

## Supported Markdown

Output uses only these constructs:

- blank lines between logical sections;
- plain key-value lines, such as `command: symbols`;
- compact bullets for repeated items;
- inline code spans for paths, symbols, command names, options, IDs, and short code fragments;
- fenced code blocks for source text and longer display text.

Do not use headings in CLI output. The command name is a key-value line:

```markdown
command: symbols
query: `MyService`
returned: 2/2
truncated: false
```

Do not use bold, italic, strikethrough, tables, links, blockquotes, task lists, raw HTML, images, diagrams, alerts, emoji, or footnotes in CLI output.

## Location Format

Locations always render as one-based column ranges:

```text
path:startLine:startColumn-endLine:endColumn
```

Examples:

```text
src/MyApp/MyService.cs:8:14-8:23
src/MyApp/MyService.cs:8:1-24:2
```

For a point selector, the range uses matching start and end positions:

```text
src/MyApp/Program.cs:10:20-10:20
```

## Format Contract

On success, the command writes a compact markdown fragment to stdout and exits `0`. On failure, it writes a plain-text error to stdout and exits non-zero:

```text
error: usage
message: Missing required option '--target'.
```

All failures include `error:` and `message:`. Usage errors with a deterministic retry path may include a third `hint:` line:

```text
error: usage
message: Line 70 is outside the document range 1..13.
hint: Retry with --line between 1 and 13, or run document-lines to inspect valid source lines before choosing --line/--column.
```

Exit codes: `0` success, `2` usage error, `130` canceled, `1` any other failure. The `error:` value is `usage`, `canceled`, or the exception type name. A zero exit code means stdout is command output; a non-zero exit code means stdout is the plain-text error.

Success output starts with `command: <name>` followed by command-specific key-value lines, then a blank line before bullets or fences:

```markdown
command: <command>
selector: `<location-or-symbol>`

<payload>
```

For repeated items, output uses compact bullets:

```markdown
- kind: NamedType name: `MyApp.MyService` loc: `src/MyApp/MyService.cs:8:14-8:23` id: `T:MyApp.MyService`
  documentation: Runs application work for the current request.
- kind: Method name: `MyApp.MyService.Execute` loc: `src/MyApp/MyService.cs:12:17-12:24` id: `M:MyApp.MyService.Execute(System.String)`
```

`name:` carries the fully qualified display name, and `id:` carries the documentation-comment ID that chains into `--symbol` when Roslyn can provide one. For `symbols`, `document-symbols`, `definition`, `type-definition`, and `implementations`, a non-empty XML summary may render as an indented `documentation:` continuation line below the symbol bullet. `references` renders documentation once in the command header for the searched symbol. When a symbol has more than one declaration (partial types), extra `- decl:` bullets follow with one location each.

### Documentation-Comment ID Prefixes

This is the canonical prefix legend for RoslynKit `id:` values and `--symbol` selectors that use Roslyn documentation-comment IDs:

- `N:` namespace
- `T:` type, including class, struct, interface, enum, delegate, or record
- `M:` method-like member, including method, constructor, operator, or conversion; constructors use `#ctor`
- `P:` property or indexer
- `F:` field
- `E:` event

Treat the complete `id:` value as opaque when chaining it into `--symbol`; keep the prefix, containing type, member name, and signature suffix exactly as emitted.

For source text, output uses fenced code blocks with a fence longer than any backtick run inside the payload:

````markdown
loc: `src/MyApp/MyService.cs:8:1-24:2`
```csharp
public sealed class MyService
{
    public void Execute(string value)
    {
    }
}
```
````

For non-C# text documents, the fence info string is `text`.

When workspace loading produced diagnostics, every command appends them at the end:

```markdown
workspace-diagnostics: 1
- severity: Warning message: `Project skipped because ...`
```

## Command Shapes

### `workspace`

Workspace metadata renders as key-value lines with project and document bullets. Project summary bullets use the project display name. Document bullets use the owning project path, target framework when available, document kind, and document path. Paths under the loaded root render relative; paths outside the loaded root render absolute.

```markdown
command: workspace
documents: 1

- project: `MyApp` tfm: `net10.0` documents: 42
- project: `src/MyApp/MyApp.csproj` tfm: `net10.0` kind: source path: `src/MyApp/Program.cs`
```

### `init`

`init` scaffolds the embedded RoslynKit skill bundle into the current Git repository. The command requires a `.git` directory or file in the current directory. Existing files are preserved when content is identical, rejected when content differs, and replaced only when `--overwrite` is supplied.

```markdown
command: init
agent: `codex`
root: `C:\repo\MyApp`
files: 3

- created: `.agents/skills/roslynkit/SKILL.md` agent: `codex`
- unchanged: `.agents/skills/roslynkit/references/commands.md` agent: `codex`
- overwritten: `.agents/skills/roslynkit/references/output.md` agent: `codex`
```

Agent values map only the outer target folder. The scaffolded bundle-relative files remain the same:

- `codex` -> `.agents/skills/roslynkit/`
- `claude` -> `.claude/skills/roslynkit/`
- `copilot` -> `.github/skills/roslynkit/`

### `diagnostics`

Diagnostics render as bullets sorted deterministically. `loc:` is omitted for diagnostics without a source location.

```markdown
command: diagnostics
returned: 1/1
truncated: false

- severity: Error id: `CS1002` loc: `src/MyApp/Program.cs:12:24-12:24` message: `; expected`
```

### `symbols` And `document-symbols`

`symbols` renders counts first, then one bullet per symbol. `document-symbols` uses a `file:` line instead of `query:` and does not render counts.

```markdown
command: symbols
query: `MyService`
returned: 2/2
truncated: false

- kind: NamedType name: `MyApp.MyService` loc: `src/MyApp/MyService.cs:8:14-8:23` id: `T:MyApp.MyService`
  documentation: Runs application work for the current request.
```

### `definition`, `type-definition`, And `implementations`

The selector first, then one bullet per resolved symbol. `implementations` adds a `symbol:` line and counts.

```markdown
command: definition
selector: `src/MyApp/Program.cs:10:20-10:20`

- kind: NamedType name: `MyApp.MyService` loc: `src/MyApp/MyService.cs:8:14-8:23` id: `T:MyApp.MyService`
  documentation: Runs application work for the current request.
```

### `references`

The selector, the resolved symbol, optional documentation for the searched symbol, counts, and reference bullets. Implicit references carry `implicit: true`.

```markdown
command: references
selector: `M:MyApp.MyService.Execute(System.String)`
symbol: `M:MyApp.MyService.Execute(System.String)`
documentation: Executes the requested service operation.
returned: 25/31
truncated: true

- loc: `src/MyApp/Program.cs:42:17-42:23`
- loc: `src/MyApp/Startup.cs:18:9-18:15` implicit: true
```

### `quick-info`

Tags as a compact key-value line and longer display text in fences: the description section in a `csharp` fence, all remaining sections joined into one `text` fence.

````markdown
command: quick-info
selector: `src/MyApp/Program.cs:10:20-10:20`
range: `src/MyApp/Program.cs:10:17-10:25`
tags: `Class`, `Public`

description:
```csharp
class MyApp.MyService
```

documentation:
```text
Runs application work for the current request.
```
````

### `signature-help`

The active signature and parameter indices, then one bullet per signature label.

```markdown
command: signature-help
selector: `src/MyApp/Program.cs:42:17-42:17`
active-signature: 0
active-parameter: 1

- signature: `MyService.Execute(string value)`
```

### `document-text`, `document-lines`, And `symbol-source`

Each document or declaration renders as metadata plus a fenced code block. The source payload is not indented, wrapped, escaped, or trimmed. `document-text` adds `truncated: true` only when the read was truncated.

````markdown
command: symbol-source
symbol: `T:MyApp.MyService`

- kind: NamedType name: `MyApp.MyService` loc: `src/MyApp/MyService.cs:8:14-8:23` id: `T:MyApp.MyService`

loc: `src/MyApp/MyService.cs:8:1-24:2`
```csharp
public sealed class MyService
{
    public void Execute(string value)
    {
    }
}
```
````

`document-lines` reads from `--start-line` through the lesser of `--end-line` and EOF, then adds the actual returned line range before the fence:

````markdown
command: document-lines
path: `src/MyApp/MyService.cs`
range: `src/MyApp/MyService.cs:40:1-52:2`

```csharp
public void Execute(string value)
{
}
```
````

### `help` And `version`

`help` renders the command table in the same grammar; `help <command>` renders that command's description, usage lines, and option bullets. `version` prints a plain-text version line.

```markdown
command: symbols
description: Search source declarations by symbol name.
usage: `roslynkit symbols --target <target> --query <text> [--max-results <n>]`

- option: `--query` short: `-q` value: text required: true description: symbol name text to search for
```

## Escaping And Stability

- Preserve source text exactly inside fenced code blocks; fence length grows past the longest backtick run in the payload.
- Escape backticks in inline code spans only when needed to keep the code span valid.
- Avoid table-cell escaping entirely by not using tables.
- Prefer code spans over links for local paths because the CLI cannot know the GitHub repository URL.
- Do not emit relative Markdown links to source files.
- Treat line breaks as logical line separators; process output may use the host platform's newline behavior.
