---
name: dotnet-refactor
description: Analyze or refactor over-engineered .NET, ASP.NET Core, EF Core, Clean Architecture, Onion Architecture, CQRS, MediatR, AutoMapper, repository, service-layer, and dependency-injection codebases. Use when Codex should identify boilerplate, pass-through layers, single-implementation interfaces, generic repositories, redundant mappings, or deep anemic layering; produce an evidence-backed refactoring plan; or safely simplify bloated .NET code while preserving behavior, business rules, tests, and public contracts.
---

# Dotnet Refactor

Use this skill to debloat .NET architecture without flattening real business logic. Prefer a plan-first workflow for broad codebases and narrow implementation waves for approved edits.

## Operating Principles

- Preserve behavior, public contracts, domain invariants, authorization, validation, transaction boundaries, observability, and testability.
- Delete abstractions only when live evidence shows they add no isolation, strategy, boundary, policy, or reusable behavior.
- Keep interfaces for external systems, runtime strategies, plugin surfaces, clocks, file systems, queues, HTTP clients, SDKs, and other seams that are deliberately replaceable.
- Prefer direct EF Core projections for read paths and direct `DbContext` access in features when repository wrappers only forward to `DbSet`.
- Prefer vertical feature organization for simple workflows. Do not convert controllers to Minimal APIs or remove MediatR globally unless the user asks or the local app already follows that style.
- Refactor in small waves. Each wave needs a rollback-friendly diff and focused tests.

## First Pass

1. Establish the task frame: target scope, user goal, symptoms, constraints, exclusions, and proof signal.
2. Inspect architecture docs, project graph, DI registration, command/API entry points, and nearest tests before reading broadly.
3. Build an inventory of candidate bloat by category, but stop after enough evidence to rank the first wave.
4. Produce a refactoring plan before editing unless the user requested a specific narrow change.
5. If implementing, baseline the narrowest relevant tests first when feasible, then edit one wave at a time.

For full audits or plan writing, read [references/debloat-playbook.md](references/debloat-playbook.md).

## Refactor Rules

### Generic Repositories Over EF Core

Treat `IGenericRepository<T>`, `IUnitOfWork`, or entity-specific repository classes as deletion candidates when they only wrap `DbContext`, `DbSet<T>`, `FindAsync`, `Add`, `Update`, `Remove`, `SaveChanges`, or trivial LINQ.

Replace simple read paths with `AsNoTracking()` and `.Select(...)` projections. Keep specialized query services when they contain raw SQL, compiled queries, Dapper, cross-store access, caching policy, security filtering, or reusable query composition that is not a thin wrapper.

### Single-Implementation Interfaces

Treat `IFooService` with exactly one `FooService` implementation as a deletion candidate when it is injected only for local indirection. Inject the concrete type directly, or inline the method into the feature when it has no independent behavior.

Keep the interface when it models an external boundary, has multiple implementations, is part of a public package contract, protects a high-cost dependency in tests, or encodes a real runtime strategy.

### Pass-Through CQRS And MediatR

Treat request/handler pairs as candidates when the handler only calls one service or repository and maps the result. Keep MediatR, domain events, or pipeline behaviors for complex workflows, validation/audit/logging pipelines, async fan-out, command transactions, or domain coordination.

For basic CRUD, consolidate request, response, query, and endpoint/service code into a vertical feature file or folder that matches the app's existing endpoint style.

### Mapping Overhead

Treat AutoMapper profiles as candidates when mappings are identical property copies or hide inefficient entity loading. Prefer inline EF Core projections for reads and explicit mapping methods for behavior-bearing conversions.

Keep mapping layers when they normalize external contracts, protect public API compatibility, perform non-trivial transformations, or centralize versioned contract logic.

### Deep Anemic Layering

Treat chains like `Controller -> ApplicationService -> Handler -> DomainService -> Repository -> DbContext` as candidates when each hop only forwards parameters. Collapse no-op hops into the feature entry point while preserving any hop that owns policy, validation, transactions, domain decisions, or cross-cutting concerns.

## Plan Output

When asked for an audit or deep dive, return a ranked plan with:

- Scope and proof signal.
- Candidate category and evidence.
- Files and symbols involved.
- Proposed simplification.
- Preserved behavior and risks.
- Tests to run or add.
- Suggested wave order.

Do not present file-count reduction as success by itself. The target outcome is less navigation, fewer pass-through abstractions, clearer feature ownership, and equal or better test confidence.

## Verification Checklist

- Baseline and post-change tests pass, or skipped validation is explicitly stated.
- Single-implementation interfaces or pass-through classes are reduced only where evidence justified removal.
- EF Core reads project directly to response models where practical.
- Public APIs, serialized contracts, command-line behavior, and documented workflows remain compatible unless explicitly changed.
- Domain logic and business invariants remain isolated and covered.
