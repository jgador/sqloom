# Sqloom .NET Architecture and Coding Guidelines

> Repo-adopted guidance for structuring serious C#/.NET applications and libraries.  

---

## Table of Contents

1. [Purpose](#1-purpose)
2. [Recommended Solution Structure](#2-recommended-solution-structure)
3. [Recommended Project Structure](#3-recommended-project-structure)
4. [Dependency Direction and Layering](#4-dependency-direction-and-layering)
5. [Source Organization Inside Projects](#5-source-organization-inside-projects)
6. [Public API Design Standard](#6-public-api-design-standard)
7. [Naming Standards](#7-naming-standards)
8. [Dependency Injection Standard](#8-dependency-injection-standard)
9. [Configuration and Options Standard](#9-configuration-and-options-standard)
10. [Error Handling and Validation Standard](#10-error-handling-and-validation-standard)
11. [Logging and Diagnostics Standard](#11-logging-and-diagnostics-standard)
12. [Async, Cancellation, and Threading Standard](#12-async-cancellation-and-threading-standard)
13. [Performance Standard](#13-performance-standard)
14. [Testing Architecture Standard](#14-testing-architecture-standard)
15. [Documentation and Maintainability Standard](#15-documentation-and-maintainability-standard)
16. [Good and Bad Code Examples](#16-good-and-bad-code-examples)
17. [Pull Request Review Checklist](#17-pull-request-review-checklist)
18. [Starter Template Structure](#18-starter-template-structure)
19. [Minimal Project Reference Policy](#19-minimal-project-reference-policy)
20. [Golden Rules](#20-golden-rules)

---

## 1. Purpose

This document sets practical rules for how to structure and evolve C# and .NET code in this repository.

It is **not** a recommendation to add unnecessary layers or rename projects just to match a template. The goal is to keep the codebase understandable, testable, and easy to change.

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

## 2. Recommended Solution Structure

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
├─ docs/
│  ├─ architecture/
│  ├─ api/
│  ├─ project-layout.md
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

**Why:** Root-level build policy avoids divergent project settings.


#### 3.2 Use `src/` for production code and `tests/` for tests

Do not mix tests inside production projects.

**Why:** The directory structure should make production code and test code immediately distinguishable.


#### 3.3 Keep repo automation in `scripts/` until it needs its own area

For a repo of this size, keep build scripts and repo automation in `scripts/`. Only add another top-level automation area when the repo grows enough to justify it.


#### 3.4 Use solution filters for large systems

A serious application may have a root solution plus focused `.slnf` files:

```text
Web.slnf
Infrastructure.slnf
Benchmarks.slnf
Analyzers.slnf
```

**Why:** Solution filters can speed up inner-loop work without fragmenting the repository.


#### 3.5 Use `samples/` as executable documentation

Samples should compile and preferably run in CI.

**Why:** Samples show real usage and prevent public APIs from drifting into awkward or unusable shapes.


---

## 3. Recommended Project Structure

### 3.1 Required project types for most applications

```text
Company.Product.Domain
Company.Product.Application
Company.Product.Contracts
Company.Product.Infrastructure
Company.Product.Web
Company.Product.Worker
Company.Product.*.Tests
```

### 3.2 Optional project types

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

These project names are examples for larger applications. They are not a requirement for this repository.

### 3.3 Project responsibilities

| Project | Purpose | Must not contain |
|---|---|---|
| `Domain` | Entities, value objects, domain services, domain events, invariants. | EF Core mappings, HTTP, queues, logging-heavy orchestration, DI registrations. |
| `Application` | Use cases, commands, queries, workflows, application interfaces, authorization decisions. | ASP.NET controllers/endpoints, SQL implementation details, cloud SDK calls. |
| `Contracts` | Public request/response models, integration events, and API contracts. | Domain entities, EF entities, infrastructure-specific types. |
| `Abstractions` | Interfaces/options/builders intended for multiple implementations or external extension points. | Concrete implementations, business workflows, host startup. |
| `Infrastructure` | Implementations for persistence, messaging, email, files, external APIs. | Public web endpoints, domain invariants. |
| `Web` | HTTP host, routing, middleware, authentication, endpoint composition. | Business rules, direct SQL, provider-specific orchestration. |
| `Worker` | Background processing host. | Domain rules, direct composition outside DI. |
| `ServiceDefaults` | Shared host defaults: health checks, OpenTelemetry, resilience, discovery. | Product feature logic. |
| `Testing` / `tests/Shared` | Test fixtures, builders, fake clocks, test containers, common assertions. | Production logic. |

### 3.4 When to use an `Abstractions` project

Use an `Abstractions` project only when at least two of these are true:

- Multiple implementation projects need the same public contract.
- External consumers are expected to implement the contract.
- The abstraction must be stable independently from the implementation.
- A provider/plugin/connectors model exists.
- The abstractions need to be referenced by hosts without pulling implementation dependencies.

**Why:** `Abstractions` projects are valuable when they stabilize extension points. They become harmful when used as dumping grounds.


### 3.5 Standard project naming

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

## 4. Dependency Direction and Layering

Use this dependency model:

```text
Web / Worker / CLI
  -> Application
  -> Infrastructure
  -> Contracts

Infrastructure
  -> Application
  -> Domain
  -> Abstractions
  -> Contracts

Application
  -> Domain
  -> Contracts
  -> Abstractions

Domain
  -> BCL and domain-only packages

Contracts
  -> minimal BCL packages only

Abstractions
  -> minimal BCL and Microsoft.Extensions.* abstractions
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
  implements: stores, message publishers, external clients, storage

Web / Worker / CLI
  references: Application, Infrastructure, Contracts
  owns: composition root, auth, routing, middleware, host lifecycle

Contracts
  references: minimal BCL packages only
  owns: request/response models, integration events, and wire contracts

Abstractions
  references: minimal BCL and Microsoft.Extensions.* abstractions
  owns: stable extension points

Tests
  reference: the project under test plus approved test helpers
```

### 4.1 Hard dependency rules

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

#### Rule 2: Introduce a store abstraction only when the application actually needs one

```csharp
// Application
public interface IOrderStore
{
    Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken = default);
    Task SaveAsync(Order order, CancellationToken cancellationToken = default);
}
```

```csharp
// Infrastructure.SqlServer
internal sealed class SqlOrderStore : IOrderStore
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


---

## 5. Source Organization Inside Projects

### 5.1 Keep folders simple

Start flat. Add folders only when a project becomes hard to scan.

```text
Company.Product.Application/
├─ ApplicationServiceCollectionExtensions.cs
├─ OrderCommands.cs
├─ OrderHandlers.cs
├─ OrderValidation.cs
└─ OrderStore.cs
```

```text
Company.Product.Web/
├─ OrderEndpoints.cs
├─ OrderRequests.cs
├─ OrderResponses.cs
├─ OrderMappings.cs
└─ Program.cs
```

**Why:** Deep nesting adds ceremony quickly. Start with simple, obvious files and group them only when the project genuinely gets hard to navigate.

### 5.2 Technical folders are allowed for stable technical subsystems

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


### 5.3 File and type rules

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


---

## 6. Public API Design Standard

Public API means anything consumed outside the project:

- Public classes.
- Public interfaces.
- Public methods.
- Extension methods.
- Options.
- Builders.
- Factories.
- Request and response types for public-facing APIs.
- Endpoint contracts.
- NuGet package types.
- Shared library types.

### 6.1 Public API review rule

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


### 6.2 Make the common path obvious

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

### 6.3 Use extension methods for framework integration

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


### 6.4 Return a builder when follow-up configuration is expected

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

### 6.5 Do not expose speculative interfaces

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
public interface IOrderStore
{
    Task SaveAsync(Order order, CancellationToken cancellationToken = default);
}

internal sealed class SqlOrderStore : IOrderStore
{
    public Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
```

### 6.6 Use least-specific input and most-specific output

Good:

```csharp
public static OrderSummary CreateSummary(IEnumerable<OrderLine> lines)
```

Bad:

```csharp
public static object CreateSummary(List<OrderLine> lines)
```

### 6.7 Avoid Boolean parameter soup

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

### 6.8 Simple overloads must delegate to richer overloads

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

### 6.9 Public collections must not expose mutable internals

Good:

```csharp
private readonly List<OrderLine> _lines = [];

public IReadOnlyList<OrderLine> Lines => _lines;
```

Bad:

```csharp
public List<OrderLine> Lines { get; set; } = [];
```

### 6.10 Public classes are sealed by default unless inheritance is intentional

Inheritance is a public contract. If a type is designed for inheritance, document virtual members and invariants.

Good:

```csharp
public sealed class CreateOrderHandler
{
}
```

Allowed when designed:

```csharp
public abstract class OrderStoreSpecification
{
    protected abstract IOrderStore CreateStore();
}
```

### 6.11 Extension methods must live in a discoverable namespace

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


---

## 7. Naming Standards

### 7.1 General C# naming

Use standard .NET naming and keep naming consistent across the repo.

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


### 7.2 Project names

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

### 7.3 Type suffix standards

| Suffix | Use when | Example |
|---|---|---|
| `Options` | Bound/configured settings. | `OrderProcessingOptions` |
| `Builder` | Fluent configuration or composition. | `OrdersBuilder` |
| `Factory` | Creation requires policy or dependencies. | `PaymentClientFactory` |
| `Provider` | Supplies values from an external/contextual source. | `TenantProvider` |
| `Handler` | Handles one command/query/message. | `CreateOrderHandler` |
| `Validator` | Validates a specific input/options/domain concept. | `CreateOrderCommandValidator` |
| `Store` | Persists or retrieves app-owned records or state. Do not create an interface unless substitution is real. | `OrderStore` |
| `Client` | Talks to an external service. | `FraudDetectionClient` |
| `Request` | Public-facing ASP.NET Core request contract. | `CreateOrderRequest` |
| `Response` | Public-facing ASP.NET Core response contract. | `CreateOrderResponse` |
| `HostedService` | Background service implementation. | `OutboxHostedService` |
| `Middleware` | ASP.NET Core middleware. | `CorrelationIdMiddleware` |
| `EndpointRouteBuilderExtensions` | Endpoint mapping extensions. | `OrderEndpointRouteBuilderExtensions` |
| `ServiceCollectionExtensions` | DI registration extensions. | `OrderServiceCollectionExtensions` |
| `ApplicationBuilderExtensions` | Middleware extensions. | `OrderApplicationBuilderExtensions` |

### 7.4 Names to avoid

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
| `BaseStore` | `SqlOrderStore` plus shared composition, not inheritance. |

### 7.5 Method naming

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

## 8. Dependency Injection Standard

### 8.1 Registration shape

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
        services.TryAddScoped<SqlOrderStore>();

        return services;
    }
}
```


### 8.2 DI rules

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
    SqlOrderStore orders,
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

## 9. Configuration and Options Standard

### 9.1 Options class rules

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

### 9.2 Registration

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

### 9.3 Complex validation

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

### 9.4 Configuration rules

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


---

## 10. Error Handling and Validation Standard

### 10.1 Guard clauses

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


### 10.2 Exception rules

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
Unable to resolve SqlOrderStore. Call AddSqlServerInfrastructure or register a custom store before AddOrders.
```

Bad:

```text
Object not set.
```


---

## 11. Logging and Diagnostics Standard

### 11.1 Use structured logging

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

### 11.2 Category rules

Use typed categories:

```csharp
ILogger<CreateOrderHandler>
ILogger<OrderFulfillmentWorkflow>
ILogger<SqlOrderStore>
```

For framework-like packages, define stable logging categories:

```csharp
public static class ProductLoggerCategory
{
    public const string Orders = "Company.Product.Orders";
    public const string Payments = "Company.Product.Payments";
}
```


### 11.3 Log level standard

| Level | Use |
|---|---|
| `Trace` | Very detailed internal flow, disabled by default. |
| `Debug` | Developer diagnostics, local troubleshooting. |
| `Information` | Lifecycle events and successful significant operations. |
| `Warning` | Degraded behavior, retries, invalid external input, recoverable failures. |
| `Error` | Operation failed and requires attention. |
| `Critical` | Process-wide failure, data corruption risk, service unavailable. |

### 11.4 Sensitive data rules

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

### 11.5 Event IDs

Use stable event IDs for important logs.

```csharp
internal static class OrderEventIds
{
    public static readonly EventId OrderCreated = new(1001, nameof(OrderCreated));
    public static readonly EventId PaymentFailed = new(2001, nameof(PaymentFailed));
}
```

### 11.6 Distributed tracing

Use `ActivitySource` for distributed tracing.

```csharp
internal static class OrderDiagnostics
{
    public const string ActivitySourceName = "Company.Product.Orders";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
```

### 11.7 Metrics

Use `Meter` for metrics.

```csharp
internal static class OrderMetrics
{
    public static readonly Meter Meter = new("Company.Product.Orders");
}
```

Do not use high-cardinality metric labels. Avoid tagging metrics with order IDs, user IDs, emails, or raw URLs.


---

## 12. Async, Cancellation, and Threading Standard

### 12.1 Async naming

```csharp
Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken = default);
Task SaveAsync(Order order, CancellationToken cancellationToken = default);
IAsyncEnumerable<Order> SearchAsync(OrderSearch search, CancellationToken cancellationToken = default);
```

### 12.2 Rules

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
await _client.GetFromJsonAsync<OrderResponse>(url);
```

Good:

```csharp
await _client.GetFromJsonAsync<OrderResponse>(url, cancellationToken);
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


---

## 13. Performance Standard

### 13.1 General rule

Readable code is the default. Performance-specific code must be justified by measurement, isolated, documented, and tested.


### 13.2 Rules

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

## 14. Testing Architecture Standard

### 14.1 Test project types

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


### 14.2 Test type definitions

| Type | Purpose |
|---|---|
| Unit tests | Test one class or small behavior without real infrastructure. |
| Integration tests | Test a real infrastructure boundary such as database, queue, cache, or external adapter. |
| Functional tests | Test the app through its public host surface, usually HTTP. |
| Specification tests | Shared behavior contract reused by multiple implementations. |
| Architecture tests | Enforce dependency direction, naming, and layering. |
| Benchmark tests | Measure performance-sensitive paths. |
| AOT/trimming tests | Validate compatibility where publishing constraints matter. |

### 14.3 Folder structure

Mirror the production area structure.

```text
tests/Company.Product.Application.Tests/
├─ OrderHandlerTests.cs
├─ OrderValidationTests.cs
├─ OrderResultTests.cs
└─ TestUtilities/
```


### 14.4 Specification test pattern

Use this when multiple implementations must obey the same contract.

```csharp
public abstract class OrderStoreSpecification
{
    protected abstract IOrderStore CreateStore();

    [Fact]
    public async Task SaveAsync_persists_order()
    {
        var store = CreateStore();
        var order = OrderFactory.Create();

        await store.SaveAsync(order);

        var found = await store.FindAsync(order.Id);

        Assert.NotNull(found);
        Assert.Equal(order.Id, found.Id);
    }
}
```

```csharp
public sealed class SqlServerOrderStoreTests
    : OrderStoreSpecification
{
    protected override IOrderStore CreateStore()
        => SqlServerStoreFixture.CreateStore();
}
```


### 14.5 Test naming

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

### 14.6 Test rules

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


---

## 15. Documentation and Maintainability Standard

### 15.1 Required docs

```text
README.md
docs/architecture/overview.md
docs/architecture/dependencies.md
docs/architecture/project-layout.md
docs/api/
docs/breaking-changes/
samples/
```

### 15.2 README structure

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

### 15.3 Package README structure

For NuGet packages, include:

```text
About
Key features
How to use
Main types
Additional documentation
Feedback and issues
```


### 15.4 Keep comments sparse

Prefer clear names and obvious signatures first. Add a brief comment only when public behavior, required ordering, or error behavior would be non-obvious without it.

### 15.5 API compatibility

For shared libraries and packages:

```text
PublicAPI.Shipped.txt
PublicAPI.Unshipped.txt
ApiCompat validation
Package baseline validation
Breaking-change notes
```


### 15.6 Breaking changes

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

### 15.7 Samples

Samples must:

```text
compile
run locally
avoid secrets in source
use user-secrets or environment variables
cover common scenarios
stay minimal
```


---

## 16. Good and Bad Code Examples

### 16.1 Example A: bad architecture

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

### 16.2 Example A: good architecture

```csharp
// Application
public sealed record CreateOrderCommand(
    CustomerId CustomerId,
    IReadOnlyList<CreateOrderLine> Lines);

public sealed class CreateOrderHandler(
    IOrderStore orders,
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
internal sealed class SqlOrderStore(OrderDbContext dbContext)
    : IOrderStore
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

### 16.3 Example B: bad DI

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

### 16.4 Example B: good DI

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
        services.TryAddScoped<IOrderStore, SqlOrderStore>();

        return services;
    }
}
```

### 16.5 Example C: bad options

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

### 16.6 Example C: good options

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

### 16.7 Example D: bad logging

```csharp
_logger.LogInformation(
    $"Charging card {request.CardNumber} for {request.CustomerEmail}");
```

### 16.8 Example D: good logging

```csharp
_logger.LogInformation(
    "Authorizing payment {PaymentId} for order {OrderId}",
    payment.Id,
    order.Id);
```

---

## 17. Pull Request Review Checklist

### 17.1 Architecture

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

### 17.2 Public API

```text
[ ] New public API has a usage sample.
[ ] API name is scenario-oriented and discoverable.
[ ] No speculative interface or abstraction.
[ ] Extension method uses Add*, Use*, Map*, With*, or Configure* correctly.
[ ] Overloads are minimal and delegate to a main overload.
[ ] Public collections do not expose mutable internals.
[ ] Public API comments are added only when the behavior is not obvious from the code and signature.
[ ] Exceptions and ordering requirements are documented.
[ ] API compatibility files or baselines are updated when required.
```

### 17.3 Naming and style

```text
[ ] Names avoid Manager, Helper, Utils, Common, Shared, and Base unless justified.
[ ] Fields use _camelCase, static fields use s_camelCase, constants use PascalCase.
[ ] Async methods end with Async.
[ ] Files contain one public type.
[ ] Namespace matches folder/project.
[ ] .editorconfig warnings are fixed, not casually suppressed.
```

### 17.4 DI and configuration

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

### 17.5 Error handling

```text
[ ] Public methods validate arguments.
[ ] Exceptions use specific exception types.
[ ] Exception messages are actionable.
[ ] Expected business failures are represented without exceptions.
[ ] Code does not log and rethrow without adding value.
```

### 17.6 Logging and diagnostics

```text
[ ] Logs are structured, not interpolated.
[ ] Logs do not leak secrets or personal data.
[ ] Important logs use stable event IDs.
[ ] Correlation IDs or trace context are preserved.
[ ] Metrics avoid high-cardinality labels.
[ ] Traces are added at meaningful boundaries, not every method.
```

### 17.7 Async and cancellation

```text
[ ] Async methods accept CancellationToken as the last parameter.
[ ] CancellationToken is passed downstream.
[ ] No .Result, .Wait(), or GetAwaiter().GetResult() in async flow.
[ ] No Task.Run wrapper around I/O.
[ ] ValueTask is justified by measurement or a known hot path.
```

### 17.8 Performance

```text
[ ] Hot-path changes are benchmarked.
[ ] Performance claims include evidence.
[ ] Avoidable allocations are removed from hot paths.
[ ] Unusual performance code is isolated and commented.
[ ] Reflection, LINQ, closures, and allocations are avoided where measurement requires it.
```

### 17.9 Testing

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

### 17.10 Documentation

```text
[ ] README or feature docs updated.
[ ] Samples updated for new public scenarios.
[ ] Breaking change documented.
[ ] Configuration documentation updated.
[ ] Operational diagnostics documented.
```

---

## 18. Starter Template Structure

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
├─ scripts/
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
│  │  ├─ ApplicationServiceCollectionExtensions.cs
│  │  ├─ OrderCommands.cs
│  │  ├─ OrderHandlers.cs
│  │  ├─ OrderResults.cs
│  │  ├─ OrderStore.cs
│  │  └─ Company.Product.Application.csproj
│  ├─ Company.Product.Infrastructure.SqlServer/
│  │  ├─ SqlOrderStore.cs
│  │  ├─ OrderEntityConfiguration.cs
│  │  ├─ Options/
│  │  │  └─ SqlServerOptions.cs
│  │  ├─ DependencyInjection/
│  │  │  └─ SqlServerServiceCollectionExtensions.cs
│  │  └─ Company.Product.Infrastructure.SqlServer.csproj
│  ├─ Company.Product.Web/
│  │  ├─ OrderEndpoints.cs
│  │  ├─ OrderRequests.cs
│  │  ├─ OrderResponses.cs
│  │  ├─ OrderMappings.cs
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
│  │  ├─ OrderHandlerTests.cs
│  │  └─ OrderValidationTests.cs
│  ├─ Company.Product.Infrastructure.SqlServer.Tests/
│  │  └─ SqlOrderStoreTests.cs
│  ├─ Company.Product.Web.FunctionalTests/
│  │  └─ OrderEndpointTests.cs
│  ├─ Company.Product.Specification.Tests/
│  │  └─ OrderStoreSpecification.cs
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

## 19. Minimal Project Reference Policy

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

## 20. Golden Rules

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
