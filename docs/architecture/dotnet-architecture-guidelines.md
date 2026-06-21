# Sqloom .NET Architecture and Coding Guidelines

> Repo-adopted C# and .NET design standards. Use this with `README.md`, `docs/architecture/overview.md`, and `docs/architecture/dependencies.md`, not instead of them.

## How to Use This Guide

- `README.md` is the user-facing command and workflow document.
- `docs/architecture/overview.md` is the canonical repo-layout and project-ownership document.
- `docs/architecture/dependencies.md` is the canonical project-graph and boundary-rules document.
- This file focuses on deeper coding, API, testing, and review standards that still apply after the repo-specific structure is already known.

Do not rename the current `Sqloom.*` projects into generic `Domain`, `Application`, or `Infrastructure` buckets unless there is a concrete repository need.

## Source Organization

- Keep folders simple and shallow. Add more nesting only when a project becomes hard to scan.
- One public type per file when practical.
- Align file paths and namespaces with the primary type unless the existing structure clearly says otherwise.
- Technical folders are fine for stable concepts such as `Diagnostics`, `Options`, `Validation`, `DependencyInjection`, `Internal`, `Extensions`, and `Generated`.
- Avoid vague buckets such as `Helpers`, `Managers`, `Common`, `Shared`, `Utils`, and `Base`.
- Treat the project graph as the architecture. Follow the repo-specific ownership and dependency docs instead of creating a parallel structure in code.

## Design and Naming

- Keep classes narrow. A class should have one primary responsibility and one clear reason to change.
- Keep CLI entrypoints, startup code, and HTTP surfaces thin. They should validate inputs, compose dependencies, and delegate to focused collaborators.
- Prefer precise names such as `Sqloom.<Feature>` or `Sqloom.<Provider>` over generic names.
- For public-facing ASP.NET Core contracts, prefer `Request` and `Response` suffixes over `Dto`.
- Use `Store` only for a real storage collaborator. Do not add a matching interface unless substitution is real.
- Extension methods that register or compose framework behavior should use `Add*`, `Use*`, `Map*`, `Configure*`, or `With*`.
- Public classes are sealed by default unless inheritance is intentional.

## Public API Rules

- Make the common path obvious.
- New public APIs should add or update a usage sample, README section, package README, or relevant feature doc in the same change.
- Avoid speculative abstractions and Boolean-parameter APIs.
- Prefer least-specific input types and most-specific output types where that improves clarity.
- Do not expose mutable internals through public collections.
- Keep comments sparse. Prefer clear names and signatures first, then add a brief comment only when behavior is non-obvious.

## Dependency Injection and Configuration

- The host is the composition root.
- Register services through clear extension methods rather than ad hoc startup helpers.
- Do not call `BuildServiceProvider()` during registration.
- Choose lifetimes deliberately. Background services are singletons, but they should create scopes for scoped work.
- `Directory.Build.props` is authoritative for build policy such as nullable settings, analyzers, and warnings-as-errors.
- `Directory.Packages.props` is authoritative for package versions.
- Bind configuration into validated options types. Avoid pulling raw `IConfiguration` into long-lived services when typed options or explicit constructor arguments are clearer.
- When configuration is required, add sensible defaults when possible and validate at startup.

## Error Handling and Diagnostics

- Guard arguments at public boundaries.
- Use precise exception types or explicit result types for expected failures.
- Do not log and rethrow unless you are adding meaningful context at a boundary.
- Use structured logs, not interpolated log strings.
- Do not log secrets, tokens, full connection strings, or raw credentials.
- Add stable event IDs only where they improve operations and diagnostics.

## Async and Performance

- Async methods should end with `Async`.
- `CancellationToken` should be the last parameter and should be passed downstream.
- Do not block async code with `.Result`, `.Wait()`, or similar patterns.
- Do not wrap I/O in `Task.Run`.
- Use `ValueTask`, pooling, spans, or other performance-specific shapes only when measurement justifies the added complexity.
- Isolate unusual hot-path code and add a short comment explaining why the shape is necessary.

## Testing

- Test public behavior, not private implementation details.
- Keep tests deterministic.
- Every bug fix should add a regression test when practical.
- Keep the unit, integration, and functional distinction clear.
- Run the narrowest relevant command first.
- When public CLI, package, or configuration behavior changes, cover the common path plus invalid input, cancellation, and boundary conditions where practical.
- For replay, correlate, advise, and tune issues, verify the emitted artifact chain under `artifacts/sqloom/` instead of relying only on console output.

## Documentation and Change Discipline

- If a public API, public CLI or package surface, public contract, configuration surface, or documented workflow changes, update `README.md` or the relevant docs in the same change.
- Keep samples runnable and minimal.
- Document breaking changes with a description, the new behavior, the reason, and migration steps when needed.
- Prefer short, focused comments over broad prose blocks.

## Review Checklist

### Architecture

- Code belongs in the correct project.
- Project references stay directional and minimal.
- New shared code has a clear owner and is not a dumping ground.
- Production code does not reference test projects.

### Public API

- The common path is obvious.
- Naming is precise and discoverable.
- No speculative abstraction was added without a real need.
- Request and response contracts use stable wire names.
- The relevant usage docs were updated.

### DI and Configuration

- Services are registered through clear extension methods.
- Null arguments are guarded where appropriate.
- No `BuildServiceProvider()` call was introduced in registration.
- Lifetimes are deliberate.
- Options are bound once, have validation, and validate at startup when required.

### Logging, Errors, and Async

- Logs are structured and do not leak secrets.
- Exceptions are specific and actionable.
- Async methods accept and pass `CancellationToken`.
- No sync-over-async or `Task.Run` wrapper was introduced for I/O.

### Testing and Docs

- Tests cover the changed public behavior.
- Regression tests exist for bug fixes when practical.
- Artifact-driven flows were checked through the emitted files when relevant.
- README or feature docs were updated for public surface or workflow changes.
