# Sqloom .NET Architecture and Coding Guidelines

> Repo-adopted guidance for structuring serious C#/.NET applications and libraries.  
> Derived from recurring patterns in public .NET repositories such as `dotnet/runtime`, `dotnet/aspnetcore`, `dotnet/efcore`, `dotnet/roslyn`, `dotnet/extensions`, `dotnet/aspire`, and `microsoft/semantic-kernel`.

---

## Table of Contents

1. [Purpose](#1-purpose)
2. [Source Repository Patterns](#2-source-repository-patterns)
3. [Recommended Solution Structure](#3-recommended-solution-structure)
4. [Recommended Project Structure](#4-recommended-project-structure)
5. [Dependency Direction and Layering](#5-dependency-direction-and-layering)
6. [Source Organization Inside Projects](#6-source-organization-inside-projects)
7. [Public API Design Standard](#7-public-api-design-standard)
8. [Naming Standards](#8-naming-standards)
9. [.editorconfig Recommendations](#9-editorconfig-recommendations)
10. [Dependency Injection Standard](#10-dependency-injection-standard)
11. [Configuration and Options Standard](#11-configuration-and-options-standard)
12. [Error Handling and Validation Standard](#12-error-handling-and-validation-standard)
13. [Logging and Diagnostics Standard](#13-logging-and-diagnostics-standard)
14. [Async, Cancellation, and Threading Standard](#14-async-cancellation-and-threading-standard)
15. [Performance Standard](#15-performance-standard)
16. [Testing Architecture Standard](#16-testing-architecture-standard)
17. [Documentation and Maintainability Standard](#17-documentation-and-maintainability-standard)
18. [Good and Bad Code Examples](#18-good-and-bad-code-examples)
19. [Pull Request Review Checklist](#19-pull-request-review-checklist)
20. [Starter Template Structure](#20-starter-template-structure)
21. [Minimal Project Reference Policy](#21-minimal-project-reference-policy)
22. [Golden Rules](#22-golden-rules)
23. [Reference Links](#23-reference-links)

---

## 1. Purpose

This standard adapts recurring architectural practices from large public .NET repositories into rules a product team can apply to ordinary business applications.

It is **not** a recommendation to copy the internal complexity of `dotnet/runtime`, `dotnet/aspnetcore`, `dotnet/efcore`, or `dotnet/roslyn`. Instead, it extracts repeatable habits:

- Clear solution and project boundaries.
- Explicit dependency direction.
- Reviewed public APIs.
- Stable naming rules.
- Typed and validated configuration.
- Disciplined dependency injection.
- Structured diagnostics.
- Compatibility controls.
- Test suites organized by behavior and system boundary.
- Documentation, samples, and maintainability practices treated as part of the feature.

The goal is to make a .NET application easy to extend, test, operate, and review.

---

## 2. Source Repository Patterns

The standard is inspired by the following public repository patterns.

| Repository | Pattern Extracted |
|---|---|
| `dotnet/runtime` | Root build policy, `eng/` engineering assets, strict coding guidelines, package/API compatibility, performance guidance, abstractions plus implementations, shared source discipline. |
| `dotnet/aspnetcore` | Modular service registration, `Add*`, `Use*`, and `Map*` APIs, endpoint composition, middleware organization, structured diagnostics, functional testing patterns. |
| `dotnet/efcore` | Core/relational/provider separation, abstractions, provider-specific projects, specification tests, functional tests, API baseline tests, diagnostics, options, and internal/public separation. |
| `dotnet/roslyn` | Large-solution organization, solution filters, subsystem-oriented source trees, analyzers, source generation, test infrastructure. |
| `dotnet/extensions` | Production-grade libraries for dependency injection, options, logging, telemetry, resilience, compliance, analyzers, and testing support. |
| `dotnet/aspire` | Service defaults, app-host style composition, integration and end-to-end testing separation, local orchestration, distributed app concerns. |
| `microsoft/semantic-kernel` | Abstractions/core/connectors/plugins split, samples as executable documentation, integration tests, connectors hidden behind abstractions, configuration through secrets and environment variables. |

---

## 3. Recommended Solution Structure

Use one root solution and one root build policy.

```text
Company.Product/
├─ Company.Product.slnx                  # or .sln if your SDK/tooling does not support .slnx
├─ Company.Product.slnf                  # optional filtered solution for inner-loop work
├─ global.json
├─ Directory.Build.props
├─ Directory.Build.targets
├─ Directory.Packages.props
├─ .editorconfig
├─ .globalconfig                         # optional analyzer/global analyzer settings
├─ NuGet.config                          # only if required
├─ README.md
├─ SECURITY.md
├─ eng/
│  ├─ build.ps1
│  ├─ build.sh
│  ├─ Version.props
│  └─ pipelines/
├─ docs/
│  ├─ architecture/
│  ├─ api/
│  ├─ decisions/
│  └─ breaking-changes/
├─ src/
│  ├─ Company.Product.Domain/
│  ├─ Company.Product.Application/
│  ├─ Company.Product.Contracts/
│  ├─ Company.Product.Abstractions/
│  ├─ Company.Product.Infrastructure/
│  ├─ Company.Product.Infrastructure.SqlServer/
│  ├─ Company.Product.Infrastructure.Redis/
│  ├─ Company.Product.Web/
│  ├─ Company.Product.Worker/
│  ├─ Company.Product.ServiceDefaults/
│  ├─ Company.Product.Analyzers/          # optional
│  └─ Company.Product.SourceGeneration/   # optional
├─ tests/
│  ├─ Company.Product.Domain.Tests/
│  ├─ Company.Product.Application.Tests/
│  ├─ Company.Product.Infrastructure.Tests/
│  ├─ Company.Product.Web.FunctionalTests/
│  ├─ Company.Product.Specification.Tests/
│  ├─ Company.Product.Architecture.Tests/
│  ├─ Company.Product.Benchmarks/
│  └─ Shared/
├─ samples/
│  ├─ BasicUsage/
│  └─ EndToEnd/
├─ tools/
└─ artifacts/                             # generated; ignored by git
```

### Rules

#### 3.1 The root owns build policy

Put common language version, nullable settings, analyzers, warnings, package versions, signing, source link, and test settings in root-level files:

```text
Directory.Build.props
Directory.Build.targets
Directory.Packages.props
global.json
.editorconfig
```

**Why:** Large .NET repositories use root-level build policy to avoid divergent project settings.

**Inspired by:** `dotnet/runtime`, `dotnet/efcore`, `dotnet/aspnetcore`.

#### 3.2 Use `src/` for production code and `tests/` for tests

Do not mix tests inside production projects.

**Why:** The directory structure should make production code and test code immediately distinguishable.

**Inspired by:** `dotnet/efcore`, `dotnet/aspnetcore`, `dotnet/aspire`, `microsoft/semantic-kernel`.

#### 3.3 Use `eng/` for engineering automation

Build scripts, pipeline templates, versioning, signing, and repo automation belong in `eng/`, not inside product projects.

**Why:** Engineering automation should be visible and separate from product logic.

**Inspired by:** `dotnet/runtime`, `dotnet/aspnetcore`, `dotnet/efcore`, `dotnet/roslyn`.

#### 3.4 Use solution filters for large systems

A serious application may have a root solution plus focused `.slnf` files:

```text
Web.slnf
Infrastructure.slnf
Benchmarks.slnf
Analyzers.slnf
```

**Why:** Large solutions need smaller inner-loop entry points without fragmenting the repository.

**Inspired by:** `dotnet/roslyn`, `dotnet/efcore`.

#### 3.5 Use `samples/` as executable documentation

Samples should compile and preferably run in CI.

**Why:** Samples show real usage and prevent public APIs from drifting into awkward or unusable shapes.

**Inspired by:** `microsoft/semantic-kernel`, `dotnet/aspnetcore`, `dotnet/extensions`.

---

## 4. Recommended Project Structure

### 4.1 Required project types for most applications

```text
Company.Product.Domain
Company.Product.Application
Company.Product.Contracts
Company.Product.Infrastructure
Company.Product.Web
Company.Product.Worker
Company.Product.*.Tests
```

### 4.2 Optional project types

```text
Company.Product.Abstractions
Company.Product.Infrastructure.SqlServer
Company.Product.Infrastructure.PostgreSql
Company.Product.Infrastructure.Redis
Company.Product.Infrastructure.AzureStorage
Company.Product.ServiceDefaults
Company.Product.Client
Company.Product.Testing
Company.Product.Analyzers
Company.Product.SourceGeneration
Company.Product.Benchmarks
```

### 4.3 Project responsibilities

| Project | Purpose | Must not contain |
|---|---|---|
| `Domain` | Entities, value objects, domain services, domain events, invariants. | EF Core mappings, HTTP, queues, logging-heavy orchestration, DI registrations. |
| `Application` | Use cases, commands, queries, workflows, application interfaces, authorization decisions. | ASP.NET controllers/endpoints, SQL implementation details, cloud SDK calls. |
| `Contracts` | Public DTOs, request/response models, integration events, API contracts. | Domain entities, EF entities, infrastructure-specific types. |
| `Abstractions` | Interfaces/options/builders intended for multiple implementations or external extension points. | Concrete implementations, business workflows, host startup. |
| `Infrastructure` | Implementations for persistence, messaging, email, files, external APIs. | Public web endpoints, domain invariants. |
| `Web` | HTTP host, routing, middleware, authentication, endpoint composition. | Business rules, direct SQL, provider-specific orchestration. |
| `Worker` | Background processing host. | Domain rules, direct composition outside DI. |
| `ServiceDefaults` | Shared host defaults: health checks, OpenTelemetry, resilience, discovery. | Product feature logic. |
| `Testing` / `tests/Shared` | Test fixtures, builders, fake clocks, test containers, common assertions. | Production logic. |

### 4.4 When to use an `Abstractions` project

Use an `Abstractions` project only when at least two of these are true:

- Multiple implementation projects need the same public contract.
- External consumers are expected to implement the contract.
- The abstraction must be stable independently from the implementation.
- A provider/plugin/connectors model exists.
- The abstractions need to be referenced by hosts without pulling implementation dependencies.

**Why:** `Abstractions` projects are valuable when they stabilize extension points. They become harmful when used as dumping grounds.

**Inspired by:** `Microsoft.Extensions.DependencyInjection.Abstractions`, EF Core abstractions/provider split, Semantic Kernel abstractions/core/connectors split.

### 4.5 Standard project naming

Use:

```text
Company.Product.Feature
Company.Product.Feature.Abstractions
Company.Product.Feature.SqlServer
Company.Product.Feature.InMemory
Company.Product.Feature.Tests
Company.Product.Feature.FunctionalTests
Company.Product.Feature.Specification.Tests
Company.Product.Feature.Benchmarks
```

Avoid:

```text
Company.Product.Common
Company.Product.Shared
Company.Product.Helpers
Company.Product.Core
Company.Product.Managers
Company.Product.Services
```

`Common`, `Shared`, `Core`, and `Services` are allowed only when they have a precise meaning and are not dumping grounds.

---

## 5. Dependency Direction and Layering

Use this dependency model:

```text
                    ┌────────────────────────┐
                    │        Web Host         │
                    │   Web / Worker / CLI    │
                    └───────────┬────────────┘
                                │
          ┌─────────────────────┼─────────────────────┐
          │                     │                     │
          ▼                     ▼                     ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│   Application    │  │  Infrastructure  │  │    Contracts      │
└────────┬─────────┘  └────────┬─────────┘  └──────────────────┘
         │                     │
         ▼                     ▼
┌──────────────────┐  ┌──────────────────┐
│      Domain      │  │   Abstractions    │
└──────────────────┘  └──────────────────┘
```

More precisely:

```text
Domain
  references: nothing except BCL and approved domain-only packages

Application
  references: Domain, Contracts, Abstractions
  defines: use cases, ports/interfaces, validators, policies

Infrastructure
  references: Application, Domain, Abstractions, Contracts
  implements: repositories, message publishers, external clients, storage

Web / Worker / CLI
  references: Application, Infrastructure, Contracts
  owns: composition root, auth, routing, middleware, host lifecycle

Contracts
  references: minimal BCL packages only
  owns: DTOs and wire contracts

Abstractions
  references: minimal BCL and Microsoft.Extensions.* abstractions
  owns: stable extension points

Tests
  reference: the project under test plus approved test helpers
```

### 5.1 Hard dependency rules

#### Rule 1: Domain must not reference infrastructure

No EF Core attributes, HTTP clients, logging dependencies, queue SDKs, `IConfiguration`, or DI inside domain entities.

Bad:

```csharp
public sealed class Order
{
    private readonly ILogger<Order> _logger;
    private readonly DbContext _dbContext;
}
```

Good:

```csharp
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public OrderId Id { get; }
    public IReadOnlyList<OrderLine> Lines => _lines;

    public void AddLine(ProductId productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        _lines.Add(new OrderLine(productId, quantity));
    }
}
```

#### Rule 2: Application defines ports; infrastructure implements them

```csharp
// Application
public interface IOrderRepository
{
    Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken = default);
    Task SaveAsync(Order order, CancellationToken cancellationToken = default);
}
```

```csharp
// Infrastructure.SqlServer
internal sealed class SqlOrderRepository : IOrderRepository
{
    public Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
```

#### Rule 3: Only hosts compose concrete infrastructure

```csharp
builder.Services
    .AddApplication()
    .AddSqlServerInfrastructure(builder.Configuration)
    .AddRedisInfrastructure(builder.Configuration);
```

#### Rule 4: No production project may reference a test project

Test infrastructure may reference production code. Production code must never reference tests.

#### Rule 5: No circular project references

Enforce with architecture tests or build-time analyzers.

#### Rule 6: No feature may reach sideways through another feature’s internals

Shared behavior must move to a named abstraction or shared kernel with a clear owner.

#### Rule 7: Provider-specific code gets provider-specific projects

Examples:

```text
Company.Product.Infrastructure.SqlServer
Company.Product.Infrastructure.PostgreSql
Company.Product.Infrastructure.Redis
Company.Product.Infrastructure.AzureStorage
```

**Why:** Provider-specific implementations carry different dependencies, configuration, failure behavior, and test requirements.

**Inspired by:** EF Core’s core/relational/provider architecture.

---

## 6. Source Organization Inside Projects

### 6.1 Prefer shallow project- or subsystem-oriented layout

For application and web projects, prefer shallow folders organized around stable project areas or namespaces. Add deeper per-feature nesting only when an area is large enough to need it.

```text
Company.Product.Application/
├─ Orders/
│  ├─ CreateOrderCommand.cs
│  ├─ CreateOrderHandler.cs
│  ├─ CancelOrderHandler.cs
│  ├─ CreateOrderValidator.cs
│  ├─ CreateOrderResult.cs
│  └─ IOrderRepository.cs
├─ Invoices/
├─ Customers/
└─ DependencyInjection/
   └─ ApplicationServiceCollectionExtensions.cs
```

```text
Company.Product.Web/
├─ Orders/
│  ├─ OrderEndpoints.cs
│  ├─ OrderRequests.cs
│  ├─ OrderResponses.cs
│  └─ OrderMappers.cs
├─ Diagnostics/
├─ Middleware/
├─ OpenApi/
└─ Program.cs
```

**Why:** Large Microsoft-maintained .NET repositories commonly organize source by stable subsystems and namespaces such as `Diagnostics`, `Infrastructure`, `Metadata`, `Query`, `Routing`, or `Builder`, rather than by deep vertical-slice trees. A shallow layout is easier to scan and usually avoids unnecessary nesting.

### 6.2 Technical folders are allowed for stable technical subsystems

Technical folders are appropriate when they represent stable concepts.

Recommended technical folders:

```text
Diagnostics/
Extensions/
Internal/
Options/
Validation/
Generated/
DependencyInjection/
```

Avoid vague folders:

```text
Helpers/
Common/
Managers/
Misc/
Base/
Utils/
```

**Inspired by:** EF Core’s use of technical namespaces such as `ChangeTracking`, `Diagnostics`, `Extensions`, `Infrastructure`, `Internal`, `Metadata`, `Query`, `Storage`, and `Update`.

### 6.3 File and type rules

1. One public type per file.
2. File path should match namespace.
3. Use `Internal/` for implementation details.
4. Use partial classes only for generated code, very large framework-style types, or clean separation of platform-specific members.
5. Prefer partial platform files over broad `#if` blocks.

Example:

```text
Storage/
├─ BlobLeaseManager.cs
├─ BlobLeaseManager.Linux.cs
├─ BlobLeaseManager.Windows.cs
└─ Internal/
   └─ BlobLeaseRenewalLoop.cs
```

**Inspired by:** `dotnet/runtime` coding and project guidelines.

---

## 7. Public API Design Standard

Public API means anything consumed outside the project:

- Public classes.
- Public interfaces.
- Public methods.
- Extension methods.
- Options.
- Builders.
- Factories.
- DTOs.
- Endpoint contracts.
- NuGet package types.
- Shared library types.

### 7.1 Public API review rule

Every new public API must have:

```text
1. Problem statement
2. Intended users
3. Usage sample
4. Alternatives considered
5. Naming rationale
6. Error behavior
7. Async/cancellation behavior
8. Compatibility impact
9. Test plan
```

**Why:** A public API is a long-term contract. Sample-first review reveals whether the API is understandable and difficult to misuse.

**Inspired by:** `dotnet/runtime` API review process and Framework Design Guidelines.

### 7.2 Make the common path obvious

Bad:

```csharp
var processor = new OrderProcessor();
processor.Init(config, logger, cache, mode: 3);
processor.Enable();
processor.Start();
```

Good:

```csharp
builder.Services.AddOrders(builder.Configuration);
app.MapOrderEndpoints();
```

### 7.3 Use extension methods for framework integration

Standard names:

| Prefix | Meaning |
|---|---|
| `AddX` | Registers services into `IServiceCollection`. |
| `UseX` | Adds middleware to `IApplicationBuilder`. |
| `MapX` | Maps endpoints to `IEndpointRouteBuilder`. |
| `WithX` | Customizes a builder or endpoint convention. |
| `ConfigureX` | Configures existing options or behavior. |
| `CreateX` | Creates a new object when construction has policy. |

Examples:

```csharp
builder.Services.AddOrderProcessing(builder.Configuration);
app.UseOrderCorrelation();
app.MapOrderEndpoints();
```

**Inspired by:** ASP.NET Core service registration, middleware, and endpoint routing APIs.

### 7.4 Return a builder when follow-up configuration is expected

```csharp
public interface IOrdersBuilder
{
    IServiceCollection Services { get; }
}

internal sealed class OrdersBuilder(IServiceCollection services) : IOrdersBuilder
{
    public IServiceCollection Services { get; } = services;
}

public static IOrdersBuilder AddOrders(this IServiceCollection services)
{
    ArgumentNullException.ThrowIfNull(services);

    services.TryAddScoped<CreateOrderHandler>();
    return new OrdersBuilder(services);
}
```

### 7.5 Do not expose speculative interfaces

Create an interface only when there is at least one implementation and at least one consumer that benefits from substitution.

Bad:

```csharp
public interface IOrderNameFormatter
{
    string Format(Order order);
}

public sealed class OrderNameFormatter : IOrderNameFormatter
{
    public string Format(Order order) => order.Id.ToString();
}
```

Good when substitution is real:

```csharp
public interface IOrderRepository
{
    Task SaveAsync(Order order, CancellationToken cancellationToken = default);
}

internal sealed class SqlOrderRepository : IOrderRepository
{
    public Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
```

### 7.6 Use least-specific input and most-specific output

Good:

```csharp
public static OrderSummary CreateSummary(IEnumerable<OrderLine> lines)
```

Bad:

```csharp
public static object CreateSummary(List<OrderLine> lines)
```

### 7.7 Avoid Boolean parameter soup

Bad:

```csharp
Process(order, true, false, true);
```

Good:

```csharp
Process(order, new OrderProcessingOptions
{
    ValidateInventory = true,
    SendConfirmation = false,
    ReserveCredit = true
});
```

### 7.8 Simple overloads must delegate to richer overloads

```csharp
public static IServiceCollection AddOrders(this IServiceCollection services)
    => services.AddOrders(_ => { });

public static IServiceCollection AddOrders(
    this IServiceCollection services,
    Action<OrderOptions> configure)
{
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configure);

    services.AddOptions<OrderOptions>().Configure(configure);
    return services;
}
```

### 7.9 Public collections must not expose mutable internals

Good:

```csharp
private readonly List<OrderLine> _lines = [];

public IReadOnlyList<OrderLine> Lines => _lines;
```

Bad:

```csharp
public List<OrderLine> Lines { get; set; } = [];
```

### 7.10 Public classes are sealed by default unless inheritance is intentional

Inheritance is a public contract. If a type is designed for inheritance, document virtual members and invariants.

Good:

```csharp
public sealed class CreateOrderHandler
{
}
```

Allowed when designed:

```csharp
public abstract class OrderRepositorySpecification
{
    protected abstract IOrderRepository CreateRepository();
}
```

### 7.11 Extension methods must live in a discoverable namespace

Service registration extensions belong in:

```csharp
namespace Microsoft.Extensions.DependencyInjection;
```

Endpoint extensions belong in:

```csharp
namespace Microsoft.AspNetCore.Builder;
```

Application-specific extensions may use:

```csharp
namespace Company.Product;
namespace Company.Product.DependencyInjection;
```

**Inspired by:** ASP.NET Core MVC and endpoint routing extension method placement.

---

## 8. Naming Standards

### 8.1 General C# naming

Adopt the .NET runtime and Roslyn style baseline.

| Symbol | Standard |
|---|---|
| Public types | `PascalCase` |
| Public methods | `PascalCase` |
| Public properties | `PascalCase` |
| Public events | `PascalCase` |
| Interfaces | `IPascalCase` |
| Parameters | `camelCase` |
| Locals | `camelCase` |
| Private/internal instance fields | `_camelCase` |
| Static fields | `s_camelCase` |
| Thread-static fields | `t_camelCase` |
| Constants | `PascalCase` |
| Async methods | `VerbNounAsync` |
| Generic type parameters | `T` or `TDescriptiveName` |

**Inspired by:** `dotnet/runtime` coding style and Roslyn `.editorconfig`.

### 8.2 Project names

Use:

```text
Company.Product.Orders
Company.Product.Orders.Abstractions
Company.Product.Orders.SqlServer
Company.Product.Orders.Tests
Company.Product.Orders.FunctionalTests
Company.Product.Orders.Benchmarks
```

Avoid:

```text
Company.Product.Common
Company.Product.Shared
Company.Product.Core
Company.Product.Services
Company.Product.Managers
Company.Product.BusinessLogic
```

### 8.3 Type suffix standards

| Suffix | Use when | Example |
|---|---|---|
| `Options` | Bound/configured settings. | `OrderProcessingOptions` |
| `Builder` | Fluent configuration or composition. | `OrdersBuilder` |
| `Factory` | Creation requires policy or dependencies. | `PaymentClientFactory` |
| `Provider` | Supplies values from an external/contextual source. | `TenantProvider` |
| `Handler` | Handles one command/query/message. | `CreateOrderHandler` |
| `Validator` | Validates a specific input/options/domain concept. | `CreateOrderCommandValidator` |
| `Repository` | Collection-like persistence abstraction for aggregate roots. | `IOrderRepository` |
| `Client` | Talks to an external service. | `FraudDetectionClient` |
| `Store` | Persists/retrieves lower-level records/tokens/state. | `RefreshTokenStore` |
| `HostedService` | Background service implementation. | `OutboxHostedService` |
| `Middleware` | ASP.NET Core middleware. | `CorrelationIdMiddleware` |
| `EndpointRouteBuilderExtensions` | Endpoint mapping extensions. | `OrderEndpointRouteBuilderExtensions` |
| `ServiceCollectionExtensions` | DI registration extensions. | `OrderServiceCollectionExtensions` |
| `ApplicationBuilderExtensions` | Middleware extensions. | `OrderApplicationBuilderExtensions` |

### 8.4 Names to avoid

Avoid these unless they are part of a precise domain term:

```text
Manager
Helper
Utility
Utils
Common
Shared
Base
Processor
Service
Engine
Context
Data
Info
Model
```

Preferred replacements:

| Vague | Better |
|---|---|
| `OrderManager` | `OrderFulfillmentWorkflow` |
| `PaymentHelper` | `PaymentRequestSigner` |
| `UserService` | `UserRegistrationHandler` |
| `DataProcessor` | `InvoiceImportPipeline` |
| `CommonExtensions` | `OrderEndpointRouteBuilderExtensions` |
| `BaseRepository` | `SqlOrderRepository` plus shared composition, not inheritance. |

### 8.5 Method naming

Use verbs that describe observable behavior.

Good:

```csharp
CreateOrderAsync
CancelOrderAsync
AuthorizePaymentAsync
MapOrderEndpoints
AddOrderProcessing
ValidateInventoryAsync
```

Avoid:

```csharp
DoWork
Process
HandleStuff
Execute
Run
Manage
```

Generic names such as `Execute` are acceptable only when implementing a known framework contract or command pattern where the context is already precise.

---

## 9. `.editorconfig` Recommendations

Use the repository root as the source of truth. This baseline follows the runtime and Roslyn style direction.

```editorconfig
root = true

[*]
indent_style = space
insert_final_newline = true
trim_trailing_whitespace = true
charset = utf-8

[*.cs]
indent_size = 4
end_of_line = crlf

# Braces and formatting
csharp_new_line_before_open_brace = all
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true

# Usings
dotnet_sort_system_directives_first = true
csharp_using_directive_placement = outside_namespace:suggestion

# Qualification
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion

# Language keywords
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion

# var
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion

# Null and object creation
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_null_propagation = true:suggestion
csharp_style_implicit_object_creation_when_type_is_apparent = true:suggestion
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion

# Immutability
dotnet_style_readonly_field = true:suggestion
csharp_prefer_readonly_struct = true:suggestion

# Braces
csharp_prefer_braces = true:suggestion

# Naming: private/internal instance fields
dotnet_naming_symbols.private_or_internal_fields.applicable_kinds = field
dotnet_naming_symbols.private_or_internal_fields.applicable_accessibilities = private, internal
dotnet_naming_style.underscore_prefix.required_prefix = _
dotnet_naming_style.underscore_prefix.capitalization = camel_case
dotnet_naming_rule.private_or_internal_fields_should_be_underscore.severity = suggestion
dotnet_naming_rule.private_or_internal_fields_should_be_underscore.symbols = private_or_internal_fields
dotnet_naming_rule.private_or_internal_fields_should_be_underscore.style = underscore_prefix

# Naming: static fields
dotnet_naming_symbols.static_fields.applicable_kinds = field
dotnet_naming_symbols.static_fields.required_modifiers = static
dotnet_naming_style.static_prefix.required_prefix = s_
dotnet_naming_style.static_prefix.capitalization = camel_case
dotnet_naming_rule.static_fields_should_be_s_prefix.severity = suggestion
dotnet_naming_rule.static_fields_should_be_s_prefix.symbols = static_fields
dotnet_naming_rule.static_fields_should_be_s_prefix.style = static_prefix

# Naming: constants
dotnet_naming_symbols.constants.applicable_kinds = field
dotnet_naming_symbols.constants.required_modifiers = const
dotnet_naming_style.pascal_case.capitalization = pascal_case
dotnet_naming_rule.constants_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.constants_should_be_pascal_case.symbols = constants
dotnet_naming_rule.constants_should_be_pascal_case.style = pascal_case

[*.{csproj,props,targets,xml,resx}]
indent_size = 2

[*.{json,yml,yaml}]
indent_size = 2

[*.sh]
end_of_line = lf

[*.{cmd,bat}]
end_of_line = crlf
```

### 9.1 Team enforcement

Use these settings in `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

Suppressions must be local, justified, and reviewed.

---

## 10. Dependency Injection Standard

### 10.1 Registration shape

Every feature that registers services must expose one primary `AddX` method.

```csharp
namespace Microsoft.Extensions.DependencyInjection;

public static class OrderServiceCollectionExtensions
{
    public static IServiceCollection AddOrders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<OrderProcessingOptions>()
            .Bind(configuration.GetSection(OrderProcessingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddScoped<CreateOrderHandler>();
        services.TryAddScoped<IOrderRepository, SqlOrderRepository>();

        return services;
    }
}
```

**Inspired by:** ASP.NET Core and Microsoft Extensions service registration APIs.

### 10.2 DI rules

#### Rule 1: The host is the composition root

`Program.cs` may know concrete infrastructure. Application and domain must not.

#### Rule 2: Feature projects own their registration extension

```text
Company.Product.Application
  AddApplication()

Company.Product.Infrastructure.SqlServer
  AddSqlServerInfrastructure()

Company.Product.Web
  MapProductEndpoints()
```

#### Rule 3: Use idempotent defaults

```csharp
services.TryAddScoped<IClock, SystemClock>();

services.TryAddEnumerable(
    ServiceDescriptor.Singleton<IValidateOptions<OrderOptions>, OrderOptionsValidator>());
```

#### Rule 4: Do not call `BuildServiceProvider()` during registration

Bad:

```csharp
var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<OrderService>>();
```

Good:

```csharp
services.TryAddScoped<OrderService>();
```

#### Rule 5: Choose lifetimes deliberately

| Lifetime | Use for | Avoid |
|---|---|---|
| Singleton | Stateless services, caches, factories, `TimeProvider`, metadata, expensive thread-safe clients. | Capturing scoped services, mutable request state. |
| Scoped | Unit of work, EF `DbContext`, request-specific application services. | Long-running background state. |
| Transient | Small stateless operations, mappers, validators. | Expensive clients, stateful workflows. |

#### Rule 6: Background services are singletons but create scopes

```csharp
internal sealed class OutboxHostedService(IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();

            await dispatcher.DispatchAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

#### Rule 7: Do not inject `IServiceProvider` except in infrastructure factories or framework integration points

Bad:

```csharp
public sealed class CreateOrderHandler(IServiceProvider serviceProvider)
{
}
```

Good:

```csharp
public sealed class CreateOrderHandler(
    IOrderRepository orders,
    TimeProvider timeProvider,
    ILogger<CreateOrderHandler> logger)
{
}
```

#### Rule 8: Do not hide global state inside static service locators

Bad:

```csharp
public static class Services
{
    public static IServiceProvider Provider { get; set; } = default!;
}
```

---

## 11. Configuration and Options Standard

### 11.1 Options class rules

Use one options class per feature.

```csharp
public sealed class OrderProcessingOptions
{
    public const string SectionName = "Orders";

    [Range(1, 100)]
    public int MaxBatchSize { get; init; } = 25;

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    public TimeSpan ReservationTimeout { get; init; } = TimeSpan.FromMinutes(10);
}
```

### 11.2 Registration

```csharp
services
    .AddOptions<OrderProcessingOptions>()
    .Bind(configuration.GetSection(OrderProcessingOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => options.ReservationTimeout > TimeSpan.Zero,
        "ReservationTimeout must be greater than zero.")
    .ValidateOnStart();
```

### 11.3 Complex validation

```csharp
internal sealed class OrderProcessingOptionsValidator
    : IValidateOptions<OrderProcessingOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        OrderProcessingOptions options)
    {
        if (options.MaxBatchSize <= 0)
        {
            return ValidateOptionsResult.Fail("MaxBatchSize must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("ConnectionString is required.");
        }

        return ValidateOptionsResult.Success;
    }
}
```

### 11.4 Configuration rules

1. Do not inject `IConfiguration` into application services.
2. Bind configuration at startup.
3. Inject `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>`.
4. Use defaults in property initializers.
5. Use `ValidateOnStart()` for required operational settings.
6. Use `IValidateOptions<T>` for cross-field validation.
7. Options classes must not contain services.
8. Configuration section names must be constants.
9. Secrets must not appear in samples, test snapshots, logs, or README files.
10. Prefer environment-specific configuration over conditional code.

**Inspired by:** Microsoft Extensions options pattern and Semantic Kernel sample configuration using Secret Manager and environment variables.

---

## 12. Error Handling and Validation Standard

### 12.1 Guard clauses

Public methods must validate arguments at the boundary.

```csharp
public Task<Order?> FindAsync(
    OrderId id,
    CancellationToken cancellationToken = default)
{
    if (id == OrderId.Empty)
    {
        throw new ArgumentException("Order id cannot be empty.", nameof(id));
    }

    return FindCoreAsync(id, cancellationToken);
}
```

Use modern guard helpers where appropriate:

```csharp
ArgumentNullException.ThrowIfNull(value);
ArgumentException.ThrowIfNullOrWhiteSpace(value);
ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
```

**Inspired by:** ASP.NET Core and Microsoft.Extensions public API guard patterns.

### 12.2 Exception rules

#### Rule 1: Use exceptions for programmer errors and failed invariants

```csharp
throw new InvalidOperationException(
    "Order cannot be shipped before payment is authorized.");
```

#### Rule 2: Use result types for expected business outcomes

```csharp
public sealed record CreateOrderResult(
    bool Succeeded,
    string? FailureCode,
    OrderId? OrderId);
```

#### Rule 3: Do not throw `Exception`, `SystemException`, or generic exceptions

Bad:

```csharp
throw new Exception("Failed");
```

Good:

```csharp
throw new InvalidOperationException(
    "Order cannot be cancelled after it has shipped.");
```

#### Rule 4: Validation belongs at the correct layer

| Validation | Layer |
|---|---|
| JSON shape, route values, content type. | Web |
| Command/query input. | Application |
| Business invariants. | Domain |
| Connection strings, timeouts, feature flags. | Options validation |
| External service failures, retries, timeouts. | Infrastructure |

#### Rule 5: Do not log and rethrow unless adding meaningful context at a boundary

Bad:

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Failed");
    throw;
}
```

Good:

```csharp
catch (PaymentGatewayException ex)
{
    throw new OrderPaymentException(order.Id, "Payment authorization failed.", ex);
}
```

#### Rule 6: Exception messages must explain the fix where possible

Good:

```text
Unable to resolve IOrderRepository. Call AddSqlServerInfrastructure or register a custom IOrderRepository before AddOrders.
```

Bad:

```text
Object not set.
```

**Inspired by:** Framework Design Guidelines exception recommendations.

---

## 13. Logging and Diagnostics Standard

### 13.1 Use structured logging

Bad:

```csharp
_logger.LogInformation($"Created order {order.Id} for {customer.Email}");
```

Good:

```csharp
_logger.LogInformation(
    "Created order {OrderId} for customer {CustomerId}",
    order.Id,
    customer.Id);
```

### 13.2 Category rules

Use typed categories:

```csharp
ILogger<CreateOrderHandler>
ILogger<OrderFulfillmentWorkflow>
ILogger<SqlOrderRepository>
```

For framework-like packages, define stable logging categories:

```csharp
public static class ProductLoggerCategory
{
    public const string Orders = "Company.Product.Orders";
    public const string Payments = "Company.Product.Payments";
}
```

**Inspired by:** EF Core diagnostics and Microsoft.Extensions logging categories.

### 13.3 Log level standard

| Level | Use |
|---|---|
| `Trace` | Very detailed internal flow, disabled by default. |
| `Debug` | Developer diagnostics, local troubleshooting. |
| `Information` | Lifecycle events and successful significant operations. |
| `Warning` | Degraded behavior, retries, invalid external input, recoverable failures. |
| `Error` | Operation failed and requires attention. |
| `Critical` | Process-wide failure, data corruption risk, service unavailable. |

### 13.4 Sensitive data rules

Never log:

```text
passwords
tokens
API keys
connection strings
credit card numbers
full request bodies by default
full email addresses unless explicitly classified as safe
personal data not required for diagnosis
```

Prefer:

```text
resource IDs
correlation IDs
tenant IDs where safe
status codes
durations
counts
enum values
sanitized failure codes
```

### 13.5 Event IDs

Use stable event IDs for important logs.

```csharp
internal static class OrderEventIds
{
    public static readonly EventId OrderCreated = new(1001, nameof(OrderCreated));
    public static readonly EventId PaymentFailed = new(2001, nameof(PaymentFailed));
}
```

### 13.6 Distributed tracing

Use `ActivitySource` for distributed tracing.

```csharp
internal static class OrderDiagnostics
{
    public const string ActivitySourceName = "Company.Product.Orders";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
```

### 13.7 Metrics

Use `Meter` for metrics.

```csharp
internal static class OrderMetrics
{
    public static readonly Meter Meter = new("Company.Product.Orders");
}
```

Do not use high-cardinality metric labels. Avoid tagging metrics with order IDs, user IDs, emails, or raw URLs.

**Inspired by:** Microsoft.Extensions diagnostics, telemetry, resilience, and compliance libraries.

---

## 14. Async, Cancellation, and Threading Standard

### 14.1 Async naming

```csharp
Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken = default);
Task SaveAsync(Order order, CancellationToken cancellationToken = default);
IAsyncEnumerable<Order> SearchAsync(OrderSearch search, CancellationToken cancellationToken = default);
```

### 14.2 Rules

#### Rule 1: Every async method ends with `Async`

Good:

```csharp
CreateOrderAsync
SaveAsync
FindAsync
```

Bad:

```csharp
CreateOrder
Save
Find
```

#### Rule 2: `CancellationToken` is the last parameter and defaults to `default`

```csharp
public Task SaveAsync(Order order, CancellationToken cancellationToken = default)
```

#### Rule 3: Always pass cancellation tokens downstream

Bad:

```csharp
await _client.GetFromJsonAsync<OrderDto>(url);
```

Good:

```csharp
await _client.GetFromJsonAsync<OrderDto>(url, cancellationToken);
```

#### Rule 4: Do not block async code

Bad:

```csharp
var result = client.GetAsync(url).Result;
```

Good:

```csharp
var result = await client.GetAsync(url, cancellationToken);
```

#### Rule 5: Use `Task` by default

Use `ValueTask` only for hot paths that often complete synchronously and where measurement justifies the extra complexity.

#### Rule 6: Do not implement sync APIs by blocking async APIs

Bad:

```csharp
public Order Get(OrderId id) => GetAsync(id).GetAwaiter().GetResult();
```

#### Rule 7: Use `ConfigureAwait(false)` in reusable libraries when appropriate

In ASP.NET Core application code it is usually unnecessary. In reusable packages, use it consistently when continuation context is not part of the contract.

#### Rule 8: Do not use `Task.Run` to wrap I/O

Bad:

```csharp
await Task.Run(() => httpClient.GetAsync(url));
```

#### Rule 9: Dispose async resources with `await using`

```csharp
await using var scope = serviceScopeFactory.CreateAsyncScope();
```

**Inspired by:** .NET runtime async/performance guidance and common Microsoft library design practices.

---

## 15. Performance Standard

### 15.1 General rule

Readable code is the default. Performance-specific code must be justified by measurement, isolated, documented, and tested.

**Inspired by:** `dotnet/runtime` performance guidelines.

### 15.2 Rules

#### Rule 1: Benchmark before claiming performance improvement

Put benchmarks here:

```text
tests/Company.Product.Benchmarks/
```

#### Rule 2: Do not optimize cold startup code at the expense of clarity unless startup is measured

Optimize the paths that matter.

#### Rule 3: Avoid avoidable allocations in hot paths

Bad:

```csharp
foreach (var item in items.Where(x => x.IsActive).Select(x => x.Id))
{
    ids.Add(item);
}
```

Good for hot paths:

```csharp
foreach (var item in items)
{
    if (item.IsActive)
    {
        ids.Add(item.Id);
    }
}
```

#### Rule 4: Cache immutable empty arrays and delegates where measurement justifies it

```csharp
private static readonly string[] s_emptyTags = [];
```

**Inspired by:** ASP.NET Core endpoint routing allocation-conscious helpers.

#### Rule 5: Use `ReadOnlySpan<T>`, `Span<T>`, `Memory<T>`, and pooling only where lifetime rules are clear

These tools improve performance only when used correctly.

#### Rule 6: Avoid reflection in hot paths

Prefer source generation or cached compiled delegates where appropriate.

#### Rule 7: Do not expose pooled buffers through public APIs unless ownership is explicit

Buffer ownership bugs are difficult to diagnose.

#### Rule 8: Separate hot-path code

```text
Internal/
├─ FastPath/
├─ Pooling/
└─ Generated/
```

#### Rule 9: Measure allocations

Include allocation assertions or benchmark columns for high-throughput code.

#### Rule 10: Performance-sensitive code requires comments explaining why the unusual shape exists

Bad:

```csharp
// Fast.
```

Good:

```csharp
// Avoid LINQ here because this method runs once per message and appears in the top allocation path.
// See OrderImportBenchmarks.ParseLines.
```

---

## 16. Testing Architecture Standard

### 16.1 Test project types

```text
Company.Product.Domain.Tests
Company.Product.Application.Tests
Company.Product.Infrastructure.Tests
Company.Product.Web.FunctionalTests
Company.Product.Specification.Tests
Company.Product.Architecture.Tests
Company.Product.Benchmarks
Company.Product.AotTests
Company.Product.TrimmingTests
```

**Inspired by:** EF Core’s unit, functional, specification, provider-specific, native AOT, trimming, API baseline, shared infrastructure, and tooling test projects.

### 16.2 Test type definitions

| Type | Purpose |
|---|---|
| Unit tests | Test one class or small behavior without real infrastructure. |
| Integration tests | Test a real infrastructure boundary such as database, queue, cache, or external adapter. |
| Functional tests | Test the app through its public host surface, usually HTTP. |
| Specification tests | Shared behavior contract reused by multiple implementations. |
| Architecture tests | Enforce dependency direction, naming, and layering. |
| Benchmark tests | Measure performance-sensitive paths. |
| AOT/trimming tests | Validate compatibility where publishing constraints matter. |

### 16.3 Folder structure

Mirror the production area structure.

```text
tests/Company.Product.Application.Tests/
├─ Orders/
│  ├─ CreateOrderHandlerTests.cs
│  ├─ CreateOrderValidatorTests.cs
│  └─ CancelOrderHandlerTests.cs
├─ Invoices/
└─ TestUtilities/
```

**Inspired by:** EF Core tests mirroring product areas such as change tracking, diagnostics, infrastructure, metadata, query, storage, and update.

### 16.4 Specification test pattern

Use this when multiple implementations must obey the same contract.

```csharp
public abstract class OrderRepositorySpecification
{
    protected abstract IOrderRepository CreateRepository();

    [Fact]
    public async Task SaveAsync_persists_order()
    {
        var repository = CreateRepository();
        var order = OrderFactory.Create();

        await repository.SaveAsync(order);

        var found = await repository.FindAsync(order.Id);

        Assert.NotNull(found);
        Assert.Equal(order.Id, found.Id);
    }
}
```

```csharp
public sealed class SqlServerOrderRepositoryTests
    : OrderRepositorySpecification
{
    protected override IOrderRepository CreateRepository()
        => SqlServerRepositoryFixture.CreateRepository();
}
```

**Inspired by:** EF Core specification tests for provider behavior.

### 16.5 Test naming

Use one standard form:

```text
MethodName_State_ExpectedResult
Operation_condition_expectedOutcome
```

Examples:

```csharp
CreateAsync_when_customer_is_blocked_returns_failure()
SaveAsync_when_order_exists_updates_existing_record()
AddOrders_when_services_is_null_throws()
```

### 16.6 Test rules

1. Test public behavior, not private implementation.
2. Do not use reflection to test private methods.
3. Extract behavior into a public/internal collaborator if it needs direct testing.
4. Use fixtures for expensive resources.
5. Shared test helpers live under `tests/Shared` or `Company.Product.Testing`.
6. Tests must be deterministic.
7. Use `TimeProvider`, fixed random seeds, explicit cultures, and stable time zones.
8. Functional tests should use real host startup.
9. CI must allow filtering.
10. Every bug fix must include a regression test.
11. Every public API must have tests for the common path, invalid input, cancellation, and boundary conditions.
12. Performance claims require benchmark evidence.

Fixture examples:

```text
SqlServerFixture
RedisFixture
WebApplicationFixture
FakeClock
```

**Inspired by:** EF Core fixtures/specification tests and Aspire test filtering/end-to-end separation.

---

## 17. Documentation and Maintainability Standard

### 17.1 Required docs

```text
README.md
docs/architecture/overview.md
docs/architecture/dependencies.md
docs/architecture/project-layout.md
docs/api/
docs/breaking-changes/
samples/
```

### 17.2 README structure

```markdown
# Company.Product

## What it does
## Supported scenarios
## Quick start
## Configuration
## Local development
## Running tests
## Deployment
## Observability
## Security
## Contributing
```

### 17.3 Package README structure

For NuGet packages, include:

```text
About
Key features
How to use
Main types
Additional documentation
Feedback and issues
```

**Inspired by:** `dotnet/runtime` package README and packaging guidance.

### 17.4 XML docs

Public APIs must have XML docs.

```csharp
/// <summary>
/// Registers order processing services.
/// </summary>
/// <param name="services">The service collection.</param>
/// <param name="configuration">The application configuration.</param>
/// <returns>The same service collection for chaining.</returns>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="services" /> or <paramref name="configuration" /> is <see langword="null" />.
/// </exception>
public static IServiceCollection AddOrders(
    this IServiceCollection services,
    IConfiguration configuration)
```

### 17.5 API compatibility

For shared libraries and packages:

```text
PublicAPI.Shipped.txt
PublicAPI.Unshipped.txt
ApiCompat validation
Package baseline validation
Breaking-change notes
```

**Inspired by:** `dotnet/runtime` API compatibility and package baseline validation, plus EF Core API baseline tests.

### 17.6 Breaking changes

A breaking change requires:

```text
1. Description
2. Previous behavior
3. New behavior
4. Reason
5. Migration steps
6. Affected APIs/configuration
7. Version introduced
```

### 17.7 Samples

Samples must:

```text
compile
run locally
avoid secrets in source
use user-secrets or environment variables
cover common scenarios
stay minimal
```

**Inspired by:** Semantic Kernel getting-started samples and ASP.NET Core samples.

---

## 18. Good and Bad Code Examples

### 18.1 Example A: bad architecture

```csharp
public sealed class OrderController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IConfiguration configuration, ILogger<OrderController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("/orders")]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        var connectionString = _configuration["ConnectionStrings:Orders"];

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        if (request.Total <= 0)
        {
            return BadRequest("Invalid total");
        }

        _logger.LogInformation($"Creating order for {request.CustomerEmail}");

        // SQL, business rules, and HTTP response logic all mixed together.
        return Ok();
    }
}
```

Problems:

```text
Web layer owns business rules.
Web layer owns SQL.
Configuration is pulled directly inside request handling.
Logging leaks email and uses interpolation.
No cancellation token.
No application use case.
No testable persistence abstraction.
```

### 18.2 Example A: good architecture

```csharp
// Application
public sealed record CreateOrderCommand(
    CustomerId CustomerId,
    IReadOnlyList<CreateOrderLine> Lines);

public sealed class CreateOrderHandler(
    IOrderRepository orders,
    TimeProvider timeProvider,
    ILogger<CreateOrderHandler> logger)
{
    public async Task<CreateOrderResult> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = Order.Create(
            command.CustomerId,
            command.Lines,
            timeProvider.GetUtcNow());

        await orders.SaveAsync(order, cancellationToken);

        logger.LogInformation(
            "Created order {OrderId} for customer {CustomerId}",
            order.Id,
            command.CustomerId);

        return CreateOrderResult.Success(order.Id);
    }
}
```

```csharp
// Web
public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/orders", CreateOrderAsync)
            .WithName("CreateOrder");

        return endpoints;
    }

    private static async Task<IResult> CreateOrderAsync(
        CreateOrderRequest request,
        CreateOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.Succeeded
            ? Results.Created($"/orders/{result.OrderId}", result)
            : Results.BadRequest(result);
    }
}
```

```csharp
// Infrastructure.SqlServer
internal sealed class SqlOrderRepository(OrderDbContext dbContext)
    : IOrderRepository
{
    public async Task SaveAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

### 18.3 Example B: bad DI

```csharp
public static class StartupHelpers
{
    public static void RegisterStuff(IServiceCollection services, IConfiguration config)
    {
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<object>>();

        services.AddSingleton(new OrderService(config["ConnectionString"], logger));
    }
}
```

Problems:

```text
Vague name.
No extension method.
Builds a temporary service provider.
Captures configuration manually.
Creates singleton with unclear lifetime safety.
Returns void, so chaining is impossible.
```

### 18.4 Example B: good DI

```csharp
namespace Microsoft.Extensions.DependencyInjection;

public static class OrderServiceCollectionExtensions
{
    public static IServiceCollection AddOrderProcessing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<OrderProcessingOptions>()
            .Bind(configuration.GetSection(OrderProcessingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddScoped<CreateOrderHandler>();
        services.TryAddScoped<IOrderRepository, SqlOrderRepository>();

        return services;
    }
}
```

### 18.5 Example C: bad options

```csharp
public sealed class Settings
{
    public string? Url { get; set; }
    public int Timeout { get; set; }
}
```

Problems:

```text
Vague type name.
No section name.
Nullable by accident.
No defaults.
No validation.
Unit ambiguity for Timeout.
```

### 18.6 Example C: good options

```csharp
public sealed class FraudDetectionOptions
{
    public const string SectionName = "FraudDetection";

    [Required]
    public Uri Endpoint { get; init; } = new("https://localhost");

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 10;

    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
}
```

### 18.7 Example D: bad logging

```csharp
_logger.LogInformation(
    $"Charging card {request.CardNumber} for {request.CustomerEmail}");
```

### 18.8 Example D: good logging

```csharp
_logger.LogInformation(
    "Authorizing payment {PaymentId} for order {OrderId}",
    payment.Id,
    order.Id);
```

---

## 19. Pull Request Review Checklist

### 19.1 Architecture

```text
[ ] Code is in the correct project.
[ ] Domain has no infrastructure, HTTP, EF, DI, logging, or configuration dependency.
[ ] Application depends only on domain/contracts/abstractions.
[ ] Infrastructure implements application abstractions.
[ ] Host composes dependencies but does not contain business logic.
[ ] No circular project references.
[ ] No production project references test code.
[ ] New shared code has a clear owner and is not a dumping ground.
```

### 19.2 Public API

```text
[ ] New public API has a usage sample.
[ ] API name is scenario-oriented and discoverable.
[ ] No speculative interface or abstraction.
[ ] Extension method uses Add*, Use*, Map*, With*, or Configure* correctly.
[ ] Overloads are minimal and delegate to a main overload.
[ ] Public collections do not expose mutable internals.
[ ] Public API has XML docs.
[ ] Exceptions and ordering requirements are documented.
[ ] API compatibility files or baselines are updated when required.
```

### 19.3 Naming and style

```text
[ ] Names avoid Manager, Helper, Utils, Common, Shared, and Base unless justified.
[ ] Fields use _camelCase, static fields use s_camelCase, constants use PascalCase.
[ ] Async methods end with Async.
[ ] Files contain one public type.
[ ] Namespace matches folder/project.
[ ] .editorconfig warnings are fixed, not casually suppressed.
```

### 19.4 DI and configuration

```text
[ ] Services are registered through a clear AddX extension method.
[ ] Registration methods guard null arguments.
[ ] Defaults use TryAdd/TryAddEnumerable where appropriate.
[ ] No BuildServiceProvider in registration.
[ ] Lifetimes are correct.
[ ] Options are bound once at startup.
[ ] Options have defaults and validation.
[ ] Required options use ValidateOnStart.
[ ] No application service injects IConfiguration directly.
```

### 19.5 Error handling

```text
[ ] Public methods validate arguments.
[ ] Exceptions use specific exception types.
[ ] Exception messages are actionable.
[ ] Expected business failures are represented without exceptions.
[ ] Code does not log and rethrow without adding value.
```

### 19.6 Logging and diagnostics

```text
[ ] Logs are structured, not interpolated.
[ ] Logs do not leak secrets or personal data.
[ ] Important logs use stable event IDs.
[ ] Correlation IDs or trace context are preserved.
[ ] Metrics avoid high-cardinality labels.
[ ] Traces are added at meaningful boundaries, not every method.
```

### 19.7 Async and cancellation

```text
[ ] Async methods accept CancellationToken as the last parameter.
[ ] CancellationToken is passed downstream.
[ ] No .Result, .Wait(), or GetAwaiter().GetResult() in async flow.
[ ] No Task.Run wrapper around I/O.
[ ] ValueTask is justified by measurement or a known hot path.
```

### 19.8 Performance

```text
[ ] Hot-path changes are benchmarked.
[ ] Performance claims include evidence.
[ ] Avoidable allocations are removed from hot paths.
[ ] Unusual performance code is isolated and commented.
[ ] Reflection, LINQ, closures, and allocations are avoided where measurement requires it.
```

### 19.9 Testing

```text
[ ] Tests cover public behavior.
[ ] Tests are in the correct test project.
[ ] Unit/integration/functional distinction is clear.
[ ] New infrastructure implementation passes shared specification tests where applicable.
[ ] Regression test exists for bug fixes.
[ ] Tests are deterministic.
[ ] Test helpers are not referenced by production code.
[ ] Benchmark/AOT/trimming/API baseline tests are updated when relevant.
```

### 19.10 Documentation

```text
[ ] README or feature docs updated.
[ ] Samples updated for new public scenarios.
[ ] Breaking change documented.
[ ] Configuration documentation updated.
[ ] Operational diagnostics documented.
```

---

## 20. Starter Template Structure

```text
Company.Product/
├─ Company.Product.slnx
├─ global.json
├─ Directory.Build.props
├─ Directory.Build.targets
├─ Directory.Packages.props
├─ .editorconfig
├─ README.md
├─ docs/
│  ├─ architecture/
│  │  ├─ overview.md
│  │  ├─ dependencies.md
│  │  └─ project-layout.md
├─ eng/
│  ├─ build.ps1
│  └─ build.sh
├─ src/
│  ├─ Company.Product.Domain/
│  │  ├─ Orders/
│  │  │  ├─ Order.cs
│  │  │  ├─ OrderId.cs
│  │  │  └─ OrderLine.cs
│  │  └─ Company.Product.Domain.csproj
│  ├─ Company.Product.Contracts/
│  │  ├─ Orders/
│  │  │  ├─ CreateOrderRequest.cs
│  │  │  └─ CreateOrderResponse.cs
│  │  └─ Company.Product.Contracts.csproj
│  ├─ Company.Product.Application/
│  │  ├─ Orders/
│  │  │  ├─ CreateOrderCommand.cs
│  │  │  ├─ CreateOrderHandler.cs
│  │  │  ├─ CreateOrderResult.cs
│  │  │  └─ IOrderRepository.cs
│  │  ├─ DependencyInjection/
│  │  │  └─ ApplicationServiceCollectionExtensions.cs
│  │  └─ Company.Product.Application.csproj
│  ├─ Company.Product.Infrastructure.SqlServer/
│  │  ├─ Orders/
│  │  │  ├─ SqlOrderRepository.cs
│  │  │  └─ OrderEntityConfiguration.cs
│  │  ├─ Options/
│  │  │  └─ SqlServerOptions.cs
│  │  ├─ DependencyInjection/
│  │  │  └─ SqlServerServiceCollectionExtensions.cs
│  │  └─ Company.Product.Infrastructure.SqlServer.csproj
│  ├─ Company.Product.Web/
│  │  ├─ Orders/
│  │  │  ├─ OrderEndpoints.cs
│  │  │  ├─ OrderRequests.cs
│  │  │  └─ OrderMappers.cs
│  │  ├─ Diagnostics/
│  │  ├─ Program.cs
│  │  └─ Company.Product.Web.csproj
│  └─ Company.Product.ServiceDefaults/
│     ├─ Extensions.cs
│     └─ Company.Product.ServiceDefaults.csproj
├─ tests/
│  ├─ Company.Product.Domain.Tests/
│  │  └─ Orders/
│  │     └─ OrderTests.cs
│  ├─ Company.Product.Application.Tests/
│  │  └─ Orders/
│  │     ├─ CreateOrderHandlerTests.cs
│  │     └─ CreateOrderValidatorTests.cs
│  ├─ Company.Product.Infrastructure.SqlServer.Tests/
│  │  └─ Orders/
│  │     └─ SqlOrderRepositoryTests.cs
│  ├─ Company.Product.Web.FunctionalTests/
│  │  └─ Orders/
│  │     └─ OrderEndpointTests.cs
│  ├─ Company.Product.Specification.Tests/
│  │  └─ Orders/
│  │     └─ OrderRepositorySpecification.cs
│  ├─ Company.Product.Architecture.Tests/
│  │  └─ DependencyRulesTests.cs
│  ├─ Company.Product.Benchmarks/
│  │  └─ Orders/
│  │     └─ CreateOrderBenchmarks.cs
│  └─ Shared/
│     ├─ FakeClock.cs
│     └─ TestData.cs
└─ samples/
   └─ BasicUsage/
      ├─ Program.cs
      └─ README.md
```

---

## 21. Minimal Project Reference Policy

Use this as the default `ProjectReference` policy.

```text
Company.Product.Domain
  -> no project references

Company.Product.Contracts
  -> no project references, unless shared primitive contracts are isolated

Company.Product.Abstractions
  -> Contracts only, if needed

Company.Product.Application
  -> Domain
  -> Contracts
  -> Abstractions

Company.Product.Infrastructure.SqlServer
  -> Application
  -> Domain
  -> Contracts

Company.Product.Web
  -> Application
  -> Infrastructure.SqlServer
  -> Contracts
  -> ServiceDefaults

Company.Product.Worker
  -> Application
  -> Infrastructure.SqlServer
  -> ServiceDefaults

Tests
  -> project under test
  -> tests/Shared or Company.Product.Testing
```

Forbidden:

```text
Domain -> Application
Domain -> Infrastructure
Application -> Infrastructure
Application -> Web
Infrastructure -> Web
Contracts -> Domain
Production -> Tests
```

---

## 22. Golden Rules

1. The project graph is the architecture. Keep it clean.
2. Domain has no infrastructure dependency.
3. Application defines use cases and ports.
4. Infrastructure implements ports.
5. Hosts compose; they do not own business rules.
6. Public APIs require usage samples before implementation.
7. Use `Add*`, `Use*`, and `Map*` consistently.
8. Bind configuration to validated options.
9. Use structured logs and never log secrets.
10. Async APIs pass cancellation tokens.
11. Performance changes need measurement.
12. Tests verify behavior through public surfaces.
13. Shared code must have a precise name and owner.
14. Compatibility, docs, and samples are part of the feature, not cleanup work.

---

## 23. Reference Links

These links are included as source references for the repository patterns behind this standard.

- `dotnet/runtime`: <https://github.com/dotnet/runtime>
- `dotnet/runtime` coding style: <https://raw.githubusercontent.com/dotnet/runtime/main/docs/coding-guidelines/coding-style.md>
- `dotnet/runtime` project guidelines: <https://raw.githubusercontent.com/dotnet/runtime/main/docs/coding-guidelines/project-guidelines.md>
- `dotnet/runtime` performance guidelines: <https://raw.githubusercontent.com/dotnet/runtime/main/docs/coding-guidelines/performance-guidelines.md>
- `dotnet/runtime` API review process: <https://raw.githubusercontent.com/dotnet/runtime/main/docs/project/api-review-process.md>
- `dotnet/runtime` Framework Design Guidelines digest: <https://raw.githubusercontent.com/dotnet/runtime/main/docs/coding-guidelines/framework-design-guidelines-digest.md>
- `dotnet/aspnetcore`: <https://github.com/dotnet/aspnetcore>
- ASP.NET Core MVC service registration source: <https://source.dot.net/Microsoft.AspNetCore.Mvc/MvcServiceCollectionExtensions.cs.html>
- ASP.NET Core endpoint route builder source: <https://source.dot.net/Microsoft.AspNetCore.Routing/Builder/EndpointRouteBuilderExtensions.cs.html>
- `dotnet/efcore`: <https://github.com/dotnet/efcore>
- `dotnet/efcore` source tree: <https://github.com/dotnet/efcore/tree/main/src>
- `dotnet/efcore` test tree: <https://github.com/dotnet/efcore/tree/main/test>
- `dotnet/roslyn`: <https://github.com/dotnet/roslyn>
- `dotnet/extensions`: <https://github.com/dotnet/extensions>
- `dotnet/aspire`: <https://github.com/dotnet/aspire>
- `dotnet/aspire` test README: <https://github.com/dotnet/aspire/blob/main/tests/README.md>
- `microsoft/semantic-kernel`: <https://github.com/microsoft/semantic-kernel>
- Semantic Kernel getting-started samples: <https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/GettingStarted/README.md>
