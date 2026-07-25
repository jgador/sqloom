# Dotnet Refactor Debloat Playbook

Use this reference for broad audits, refactoring plans, and implementation waves in .NET codebases that may have Clean Architecture, Onion Architecture, CQRS, MediatR, repository, service-layer, or mapping bloat.

## Audit Workflow

1. Capture the current shape:
   - Solution and project graph.
   - API or command entry points.
   - DI registrations.
   - EF Core `DbContext` types and repository-like wrappers.
   - MediatR request/handler pairs.
   - AutoMapper profiles or mapping services.
   - Nearest unit and integration tests.
2. Rank candidates by impact:
   - Most files touched per trivial behavior.
   - Most call hops with no policy.
   - Read paths that load full entities then map.
   - Interfaces with one implementation and no external-boundary semantics.
   - Layers that cause test setup pain without improving test confidence.
3. Protect invariants before deleting:
   - Domain entities, aggregate roots, value objects, validators, transactions, authorization, tenant filters, concurrency checks, idempotency, audit trails, and integration boundaries.
   - Public packages, API contracts, serialized JSON, CLI syntax, and documented workflows.
4. Choose the first wave:
   - Prefer one vertical feature or one repeated bloat category.
   - Avoid mixing mechanical cleanup with behavior changes.
   - Keep diffs reviewable and tests focused.

## Candidate Evidence

### Generic Repository

Confirming evidence:

- Interface methods mirror `DbSet<T>` operations.
- Implementation has no raw SQL, caching, retry, query policy, tenant policy, or cross-store behavior.
- Callers immediately map entities to DTOs after repository calls.

Weakening evidence:

- Repository centralizes security filters, query reuse, compiled queries, non-EF storage, provider-specific SQL, or app-owned consistency rules.

Refactor:

- Inject the app `DbContext` into the feature handler, endpoint, or concrete service.
- Use `AsNoTracking()` for reads.
- Use `.Where(...).Select(...)` to project directly into response records or DTOs.
- Keep `SaveChangesAsync` at the command boundary unless an existing transaction policy owns it.

### Single-Implementation Interface

Confirming evidence:

- `IThing` has exactly one `Thing` implementation.
- DI maps the interface to that implementation.
- Tests only mock pass-through behavior and do not assert boundary interaction.
- The interface is internal or app-local.

Weakening evidence:

- Multiple implementations exist across target frameworks or composition roots.
- The interface is public API, plugin contract, external-system seam, or expensive dependency seam.
- Tests intentionally substitute behavior that cannot be exercised through integration tests.

Refactor:

- Inject the concrete type.
- Remove interface registration.
- If the concrete class only forwards once, inline it into the feature and delete the class too.

### Pass-Through MediatR Or CQRS

Confirming evidence:

- Request type has only primitive parameters.
- Handler calls a single repository/service method and returns the mapped result.
- No pipeline behavior meaningfully applies to that request.
- The request/handler split creates several files for one query.

Weakening evidence:

- The handler coordinates multiple aggregates, transactions, events, validation, authorization, retries, or pipeline behaviors.
- The app deliberately uses MediatR as its public command bus.

Refactor:

- Consolidate simple request, response, query, and endpoint code into one vertical feature.
- Keep complex command handlers and event workflows intact.

### Mapping Layer

Confirming evidence:

- AutoMapper profile maps identical names without custom rules.
- Entity is loaded in memory only to create a DTO.
- Mapping hides N+1 access or over-fetching.

Weakening evidence:

- Mapping normalizes external contracts, versioned API contracts, polymorphic shapes, computed fields, or privacy rules.

Refactor:

- Use inline EF Core projections for reads.
- Use explicit `ToResponse()` or `FromRequest()` methods when mapping has behavior worth naming.

## Plan Template

Use this format for audit output:

```markdown
## Refactor Plan

Scope:
- ...

Proof Signal:
- ...

Wave 1: <smallest high-confidence simplification>
- Evidence: ...
- Proposed change: ...
- Files/symbols: ...
- Behavior to preserve: ...
- Tests: ...
- Risk: Low/Medium/High because ...

Wave 2: <next candidate>
- Evidence: ...
- Proposed change: ...
- Files/symbols: ...
- Behavior to preserve: ...
- Tests: ...
- Risk: Low/Medium/High because ...

Not Changing Yet:
- ...
```

## Implementation Checklist

- Check `git status` before edits and do not revert unrelated user changes.
- Run the narrowest relevant tests before edits when practical.
- Read existing tests before changing production code.
- Change one category or feature slice at a time.
- Prefer explicit constructors or primary constructors according to the existing project style.
- Run formatter and focused tests after edits.
- State skipped validation and why.
