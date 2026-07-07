# Sqloom Map

Resident architecture context for first-pass navigation. Atlas stores durable routing facts only; use `git ls-files`, `rg`, RoslynKit live commands, tests, generated artifacts, and direct file reads for current facts.

## Shape

- Solution: [Sqloom.slnx](../../Sqloom.slnx)
- Focused solution filters: [Sqloom.UnitTests.slnf](../../Sqloom.UnitTests.slnf), [Sqloom.IntegrationTests.slnf](../../Sqloom.IntegrationTests.slnf)
- Production: [src/Sqloom.Core/](../../src/Sqloom.Core/), [src/Sqloom.Testing/](../../src/Sqloom.Testing/), [src/Sqloom.Host/](../../src/Sqloom.Host/)
- Tests and sample harness: [tests/Sqloom.UnitTests/](../../tests/Sqloom.UnitTests/), [tests/Sqloom.IntegrationTests/](../../tests/Sqloom.IntegrationTests/), [tests/Sqloom.TestApp/](../../tests/Sqloom.TestApp/), [tests/Sqloom.TestApp.Harness/](../../tests/Sqloom.TestApp.Harness/)
- Docs and tooling: [README.md](../../README.md), [docs/](../../docs/), [scripts/](../../scripts/)
- Generated artifacts: `artifacts/sqloom/`
- Agent assets: [AGENTS.md](../../AGENTS.md), [.agents/skills/roslynkit/](../../.agents/skills/roslynkit/), [.agents/skills/roslynkit/references/commands.md](../../.agents/skills/roslynkit/references/commands.md), [.agents/skills/roslynkit/references/output.md](../../.agents/skills/roslynkit/references/output.md), [.agents/skills/security-audit/SKILL.md](../../.agents/skills/security-audit/SKILL.md), [.codex/agents/](../agents/), [.codex/atlas/repo-map.md](repo-map.md)

## Runtime Diagram

```mermaid
flowchart TB
  subgraph Entry["CLI Entry And Startup"]
    User["User / automation"]
    Tool["sqloom / sqloom-local<br/>packaged .NET tool"]
    Program["Program.cs"]
    Runtime["HostRuntime<br/>startup parsing, help/version, app creation"]
    Startup["Startup options<br/>target, command, diagnostics"]
    App["HostApplication<br/>resolve target and dispatch command"]
    Registry["CommandRegistry<br/>HostCommandKind -> command"]
  end

  User --> Tool --> Program --> Runtime
  Runtime --> Startup --> App --> Registry

  subgraph Commands["Stage Commands In Sqloom.Host"]
    Tune["TuneCommand<br/>common workflow front door"]
    Replay["ReplayCommand<br/>ASP.NET Core replay"]
    Observe["ObserveCommand<br/>SQL Server Query Store collection"]
    Correlate["CorrelateCommand<br/>replay-to-Query Store correlation"]
    Advise["AdviceCommand<br/>evidence pack, schema, advice, SQL proposals"]
  end

  Registry --> Tune
  Registry --> Replay
  Registry --> Observe
  Registry --> Correlate
  Registry --> Advise

  Tune --> Replay
  Tune --> Observe
  Tune --> Correlate
  Tune --> Advise

  subgraph Resolution["Target And Harness Resolution"]
    TargetInput["Target input<br/>project, assembly, solution, solution filter, directory"]
    Resolver["Resolution pipeline<br/>find/load harness assembly"]
    HarnessContract["Sqloom.Testing<br/>ISqloomApplication + session contracts"]
    HarnessApp["App-owned harness<br/>exactly one public non-abstract ISqloomApplication"]
    Session["ISqloomApplicationSession<br/>app session for replay and setup"]
  end

  App --> TargetInput --> Resolver
  Resolver --> HarnessContract
  Resolver --> HarnessApp --> Session

  subgraph ReplayFlow["Replay Stage"]
    ReplayArgs["ReplayArgumentParser"]
    ReplayPlan["Replay plan<br/>endpoint request selection"]
    AspNetReplay["ASP.NET Core replay runner"]
    EndpointExecution["Endpoint execution contracts"]
    ReplayEvidence["Replay evidence models"]
    ReplayArtifact["Replay stage artifacts"]
  end

  Replay --> ReplayArgs --> ReplayPlan --> AspNetReplay
  AspNetReplay --> Session
  AspNetReplay --> EndpointExecution --> ReplayEvidence --> ReplayArtifact

  subgraph ObserveFlow["Observe Stage"]
    ObserveArgs["ObserveArgumentParser"]
    QueryStoreCollector["SQL Server Query Store collector"]
    DiscoveredObjects["Discovered SQL objects"]
    WorkloadClassifier["Workload classifier"]
    QueryStoreEvidence["Query Store evidence models"]
    ObserveArtifact["Observe stage artifacts"]
  end

  Observe --> ObserveArgs --> QueryStoreCollector
  QueryStoreCollector --> DiscoveredObjects
  QueryStoreCollector --> WorkloadClassifier
  QueryStoreCollector --> QueryStoreEvidence --> ObserveArtifact

  subgraph CorrelateFlow["Correlate Stage"]
    CorrelateArgs["CorrelateArgumentParser"]
    Correlator["QueryStoreCorrelator"]
    StatementHandles["statement/query/plan handle matching"]
    CorrelationReport["Correlation report models"]
    CorrelationArtifact["query-store-correlation.json"]
  end

  Correlate --> CorrelateArgs --> Correlator
  ReplayArtifact --> Correlator
  ObserveArtifact --> Correlator
  Correlator --> StatementHandles --> CorrelationReport --> CorrelationArtifact

  subgraph AdviseFlow["Advise Stage"]
    AdviseArgs["AdviseArgumentParser"]
    SchemaSource["Schema source<br/>DACPAC or catalog-derived schema"]
    DacpacExtractor["SqlServerDacpacSchemaExtractor"]
    EvidencePack["OpenAIAdviceEvidencePackBuilder"]
    AdviceGenerator["OpenAIAdviceGenerator"]
    AdviceContracts["OpenAI advice request/response contracts"]
    LocalValidation["Local proposal validation and normalization"]
    AdviceArtifact["tuning-advice.json"]
    SqlProposal["sql-tuning-proposal.sql"]
  end

  Advise --> AdviseArgs
  AdviseArgs --> SchemaSource
  SchemaSource --> DacpacExtractor
  CorrelationArtifact --> EvidencePack
  ObserveArtifact --> EvidencePack
  ReplayArtifact --> EvidencePack
  DacpacExtractor --> EvidencePack
  EvidencePack --> AdviceGenerator --> AdviceContracts
  AdviceContracts --> LocalValidation
  LocalValidation --> AdviceArtifact
  LocalValidation --> SqlProposal

  subgraph TuneFlow["Tune Workflow"]
    TuneArgs["TuneArgumentParser"]
    TuneContext["Tune command context"]
    TuneReport["TuneWorkflowReport"]
  end

  Tune --> TuneArgs --> TuneContext
  TuneContext --> Replay
  TuneContext --> Observe
  TuneContext --> Correlate
  TuneContext --> Advise
  ReplayArtifact --> TuneReport
  ObserveArtifact --> TuneReport
  CorrelationArtifact --> TuneReport
  AdviceArtifact --> TuneReport
  SqlProposal --> TuneReport

  subgraph ArtifactRoot["artifacts/sqloom"]
    ReplayRun["replay output"]
    ObserveRun["query-store output"]
    CorrelateRun["correlation output"]
    AdviceRun["advice output"]
    ProposalRun["SQL proposal output"]
    WorkflowRun["tune workflow report"]
  end

  ReplayArtifact --> ReplayRun
  ObserveArtifact --> ObserveRun
  CorrelationArtifact --> CorrelateRun
  AdviceArtifact --> AdviceRun
  SqlProposal --> ProposalRun
  TuneReport --> WorkflowRun

  subgraph CoreContracts["Sqloom.Core Runtime Contracts"]
    CoreArtifacts["Artifact layout and JSON contracts"]
    CoreExecution["Execution and replay evidence contracts"]
    CoreQueryStore["Query Store and correlation contracts"]
    CoreOpenAI["OpenAI advice contracts"]
    CoreHelpers["Provider-neutral helpers"]
  end

  ReplayEvidence --> CoreExecution
  QueryStoreEvidence --> CoreQueryStore
  CorrelationReport --> CoreQueryStore
  AdviceContracts --> CoreOpenAI
  ReplayArtifact --> CoreArtifacts
  ObserveArtifact --> CoreArtifacts
  CorrelationArtifact --> CoreArtifacts
  AdviceArtifact --> CoreArtifacts
  SqlProposal --> CoreArtifacts

  classDef entry fill:#e8f2ff,stroke:#2b5fab,color:#102033
  classDef command fill:#fff4df,stroke:#9a6500,color:#1f1600
  classDef harness fill:#f4ecff,stroke:#6d45a3,color:#21142f
  classDef replay fill:#e9f8ee,stroke:#2f7d42,color:#102215
  classDef observe fill:#eef7ff,stroke:#3178a8,color:#0b2230
  classDef correlate fill:#fff0f0,stroke:#a33d3d,color:#2b1010
  classDef advise fill:#f7f0ff,stroke:#7b4ca0,color:#25122f
  classDef artifact fill:#f8f8ec,stroke:#74742b,color:#20200c
  classDef core fill:#eef0f3,stroke:#555f6d,color:#171a1f

  class User,Tool,Program,Runtime,Startup,App,Registry entry
  class Tune,Replay,Observe,Correlate,Advise,TuneArgs,TuneContext,TuneReport command
  class TargetInput,Resolver,HarnessContract,HarnessApp,Session harness
  class ReplayArgs,ReplayPlan,AspNetReplay,EndpointExecution,ReplayEvidence,ReplayArtifact replay
  class ObserveArgs,QueryStoreCollector,DiscoveredObjects,WorkloadClassifier,QueryStoreEvidence,ObserveArtifact observe
  class CorrelateArgs,Correlator,StatementHandles,CorrelationReport,CorrelationArtifact correlate
  class AdviseArgs,SchemaSource,DacpacExtractor,EvidencePack,AdviceGenerator,AdviceContracts,LocalValidation,AdviceArtifact,SqlProposal advise
  class ReplayRun,ObserveRun,CorrelateRun,AdviceRun,ProposalRun,WorkflowRun artifact
  class CoreArtifacts,CoreExecution,CoreQueryStore,CoreOpenAI,CoreHelpers core
```

## Runtime Flow

- User-facing pipeline: `replay -> observe -> correlate -> advise`.
- Convenience front door: `tune` runs the common workflow and writes stage-owned artifacts under `artifacts/sqloom/`.
- [src/Sqloom.Host/Program.cs](../../src/Sqloom.Host/Program.cs) calls `HostRuntime.RunAsync`.
- [src/Sqloom.Host/HostRuntime.cs](../../src/Sqloom.Host/HostRuntime.cs) parses startup options, handles help/version, creates `HostApplication`, and delegates command execution.
- [src/Sqloom.Host/Dispatch/HostApplication.cs](../../src/Sqloom.Host/Dispatch/HostApplication.cs) resolves the selected harness or bound `ISqloomApplication`, chooses a `HostCommandKind`, creates command context, and dispatches through `CommandRegistry`.
- Stage commands own their stage behavior: [src/Sqloom.Host/ReplayCommand.cs](../../src/Sqloom.Host/ReplayCommand.cs), [src/Sqloom.Host/ObserveCommand.cs](../../src/Sqloom.Host/ObserveCommand.cs), [src/Sqloom.Host/CorrelateCommand.cs](../../src/Sqloom.Host/CorrelateCommand.cs), [src/Sqloom.Host/AdviceCommand.cs](../../src/Sqloom.Host/AdviceCommand.cs), and [src/Sqloom.Host/TuneCommand.cs](../../src/Sqloom.Host/TuneCommand.cs).

## Domains

- CLI dispatch and startup: [src/Sqloom.Host/Program.cs](../../src/Sqloom.Host/Program.cs), [src/Sqloom.Host/HostRuntime.cs](../../src/Sqloom.Host/HostRuntime.cs), [src/Sqloom.Host/Startup/](../../src/Sqloom.Host/Startup/), [src/Sqloom.Host/Dispatch/](../../src/Sqloom.Host/Dispatch/), [src/Sqloom.Host/Output/HostConsoleWriter.cs](../../src/Sqloom.Host/Output/HostConsoleWriter.cs); tests usually start in [tests/Sqloom.UnitTests/Host/HostStartupCommandLineTests.cs](../../tests/Sqloom.UnitTests/Host/HostStartupCommandLineTests.cs), [tests/Sqloom.UnitTests/Host/HostApplicationTests.cs](../../tests/Sqloom.UnitTests/Host/HostApplicationTests.cs), and [tests/Sqloom.IntegrationTests/Host/HostProcessTests.cs](../../tests/Sqloom.IntegrationTests/Host/HostProcessTests.cs).
- Replay: [src/Sqloom.Host/ReplayCommand.cs](../../src/Sqloom.Host/ReplayCommand.cs), [src/Sqloom.Host/ReplayArgumentParser.cs](../../src/Sqloom.Host/ReplayArgumentParser.cs), [src/Sqloom.Host/Replay/](../../src/Sqloom.Host/Replay/), endpoint execution contracts in [src/Sqloom.Core/Execution/](../../src/Sqloom.Core/Execution/); tests usually start in [tests/Sqloom.UnitTests/Host/ReplayArgumentParserTests.cs](../../tests/Sqloom.UnitTests/Host/ReplayArgumentParserTests.cs), [tests/Sqloom.UnitTests/Endpoints/EndpointReplayPlanBuilderTests.cs](../../tests/Sqloom.UnitTests/Endpoints/EndpointReplayPlanBuilderTests.cs), [tests/Sqloom.UnitTests/Endpoints/EndpointReplayRequestResolverTests.cs](../../tests/Sqloom.UnitTests/Endpoints/EndpointReplayRequestResolverTests.cs), [tests/Sqloom.UnitTests/Endpoints/EndpointReplayRunnerTests.cs](../../tests/Sqloom.UnitTests/Endpoints/EndpointReplayRunnerTests.cs), and [tests/Sqloom.IntegrationTests/Host/HostRuntimeTests.cs](../../tests/Sqloom.IntegrationTests/Host/HostRuntimeTests.cs).
- Observe and Query Store: [src/Sqloom.Host/ObserveCommand.cs](../../src/Sqloom.Host/ObserveCommand.cs), [src/Sqloom.Host/ObserveArgumentParser.cs](../../src/Sqloom.Host/ObserveArgumentParser.cs), [src/Sqloom.Host/QueryStore/](../../src/Sqloom.Host/QueryStore/), Query Store contracts in [src/Sqloom.Core/QueryStore/](../../src/Sqloom.Core/QueryStore/); tests usually start in [tests/Sqloom.UnitTests/Host/ObserveArgumentParserTests.cs](../../tests/Sqloom.UnitTests/Host/ObserveArgumentParserTests.cs), [tests/Sqloom.UnitTests/QueryStore/SqlServerQueryStoreCollectorTests.cs](../../tests/Sqloom.UnitTests/QueryStore/SqlServerQueryStoreCollectorTests.cs), [tests/Sqloom.UnitTests/QueryStore/SqlServerDiscoveredObjectCollectorTests.cs](../../tests/Sqloom.UnitTests/QueryStore/SqlServerDiscoveredObjectCollectorTests.cs), and [tests/Sqloom.UnitTests/QueryStore/WorkloadClassifierTests.cs](../../tests/Sqloom.UnitTests/QueryStore/WorkloadClassifierTests.cs).
- Correlate: [src/Sqloom.Host/CorrelateCommand.cs](../../src/Sqloom.Host/CorrelateCommand.cs), [src/Sqloom.Host/CorrelateArgumentParser.cs](../../src/Sqloom.Host/CorrelateArgumentParser.cs), [src/Sqloom.Host/QueryStore/QueryStoreCorrelator.cs](../../src/Sqloom.Host/QueryStore/QueryStoreCorrelator.cs), correlation contracts in [src/Sqloom.Core/QueryStore/](../../src/Sqloom.Core/QueryStore/); tests usually start in [tests/Sqloom.UnitTests/Host/CorrelateArgumentParserTests.cs](../../tests/Sqloom.UnitTests/Host/CorrelateArgumentParserTests.cs), [tests/Sqloom.UnitTests/QueryStore/QueryStoreCorrelatorTests.cs](../../tests/Sqloom.UnitTests/QueryStore/QueryStoreCorrelatorTests.cs), and [tests/Sqloom.UnitTests/QueryStore/QueryStoreCorrelationAdvisorTests.cs](../../tests/Sqloom.UnitTests/QueryStore/QueryStoreCorrelationAdvisorTests.cs).
- Advise and SQL proposals: [src/Sqloom.Host/AdviceCommand.cs](../../src/Sqloom.Host/AdviceCommand.cs), [src/Sqloom.Host/AdviseArgumentParser.cs](../../src/Sqloom.Host/AdviseArgumentParser.cs), [src/Sqloom.Host/Providers/OpenAIAdviceGenerator.cs](../../src/Sqloom.Host/Providers/OpenAIAdviceGenerator.cs), [src/Sqloom.Host/Providers/OpenAIAdviceEvidencePackBuilder.cs](../../src/Sqloom.Host/Providers/OpenAIAdviceEvidencePackBuilder.cs), [src/Sqloom.Host/SqlServerDacpacSchemaExtractor.cs](../../src/Sqloom.Host/SqlServerDacpacSchemaExtractor.cs), advice contracts in [src/Sqloom.Core/Execution/](../../src/Sqloom.Core/Execution/) and [src/Sqloom.Core/OpenAI/](../../src/Sqloom.Core/OpenAI/); tests usually start in [tests/Sqloom.UnitTests/Host/AdviseArgumentParserTests.cs](../../tests/Sqloom.UnitTests/Host/AdviseArgumentParserTests.cs), [tests/Sqloom.UnitTests/Host/AdviceCommandTests.cs](../../tests/Sqloom.UnitTests/Host/AdviceCommandTests.cs), [tests/Sqloom.UnitTests/Host/OpenAIAdviceGeneratorTests.cs](../../tests/Sqloom.UnitTests/Host/OpenAIAdviceGeneratorTests.cs), and [tests/Sqloom.UnitTests/Host/SqlServerDacpacSchemaExtractorTests.cs](../../tests/Sqloom.UnitTests/Host/SqlServerDacpacSchemaExtractorTests.cs).
- Tune workflow: [src/Sqloom.Host/TuneCommand.cs](../../src/Sqloom.Host/TuneCommand.cs), [src/Sqloom.Host/TuneArgumentParser.cs](../../src/Sqloom.Host/TuneArgumentParser.cs), [src/Sqloom.Host/TuneWorkflowReport.cs](../../src/Sqloom.Host/TuneWorkflowReport.cs), plus replay, observe, correlate, and advise domains; tests usually start in [tests/Sqloom.UnitTests/Host/TuneArgumentParserTests.cs](../../tests/Sqloom.UnitTests/Host/TuneArgumentParserTests.cs), [tests/Sqloom.IntegrationTests/Host/HostRuntimeTests.cs](../../tests/Sqloom.IntegrationTests/Host/HostRuntimeTests.cs), and [tests/Sqloom.IntegrationTests/Host/HostCatalogAdviceTests.cs](../../tests/Sqloom.IntegrationTests/Host/HostCatalogAdviceTests.cs).
- Artifact and JSON contracts: [src/Sqloom.Core/Artifacts/](../../src/Sqloom.Core/Artifacts/), persisted models under [src/Sqloom.Core/Execution/](../../src/Sqloom.Core/Execution/) and [src/Sqloom.Core/QueryStore/](../../src/Sqloom.Core/QueryStore/), and [src/Sqloom.Host/TuneWorkflowReport.cs](../../src/Sqloom.Host/TuneWorkflowReport.cs); tests usually start in [tests/Sqloom.UnitTests/Artifacts/ArtifactLayoutTests.cs](../../tests/Sqloom.UnitTests/Artifacts/ArtifactLayoutTests.cs) and [tests/Sqloom.UnitTests/Artifacts/JsonContractTests.cs](../../tests/Sqloom.UnitTests/Artifacts/JsonContractTests.cs).
- Harness model: [src/Sqloom.Testing/](../../src/Sqloom.Testing/), [tests/Sqloom.TestApp/](../../tests/Sqloom.TestApp/), [tests/Sqloom.TestApp.Harness/](../../tests/Sqloom.TestApp.Harness/), and host resolution under [src/Sqloom.Host/Resolution/](../../src/Sqloom.Host/Resolution/); tests usually start in [tests/Sqloom.UnitTests/Host/AppResolverTests.cs](../../tests/Sqloom.UnitTests/Host/AppResolverTests.cs), [tests/Sqloom.UnitTests/Host/TestAppIntegrations.cs](../../tests/Sqloom.UnitTests/Host/TestAppIntegrations.cs), [tests/Sqloom.IntegrationTests/Host/HostSeedScriptTests.cs](../../tests/Sqloom.IntegrationTests/Host/HostSeedScriptTests.cs), and [tests/Sqloom.IntegrationTests/Host/HostCatalogAdviceTests.cs](../../tests/Sqloom.IntegrationTests/Host/HostCatalogAdviceTests.cs).
- Packaging and local tooling: [src/Sqloom.Host/Sqloom.Host.csproj](../../src/Sqloom.Host/Sqloom.Host.csproj), [src/Sqloom.Host/PackageReadme.md](../../src/Sqloom.Host/PackageReadme.md), [scripts/Sqloom.Tooling.ps1](../../scripts/Sqloom.Tooling.ps1), [scripts/deploy-sqloom-local.ps1](../../scripts/deploy-sqloom-local.ps1), [docs/dotnet-tool-release.md](../../docs/dotnet-tool-release.md), and [docs/command-reference.md](../../docs/command-reference.md).
- Agent/navigation policy: [AGENTS.md](../../AGENTS.md), [docs/agents/README.md](../../docs/agents/README.md), [.codex/agents/](../agents/), [.codex/atlas/repo-map.md](repo-map.md), and [.agents/skills/roslynkit/](../../.agents/skills/roslynkit/).

## Test Routing

- CLI startup, command dispatch, and help/version output -> [tests/Sqloom.UnitTests/Host/HostStartupCommandLineTests.cs](../../tests/Sqloom.UnitTests/Host/HostStartupCommandLineTests.cs), [tests/Sqloom.UnitTests/Host/HostApplicationTests.cs](../../tests/Sqloom.UnitTests/Host/HostApplicationTests.cs), [tests/Sqloom.IntegrationTests/Host/HostProcessTests.cs](../../tests/Sqloom.IntegrationTests/Host/HostProcessTests.cs)
- Parser changes -> matching `*ArgumentParserTests.cs` under [tests/Sqloom.UnitTests/Host/](../../tests/Sqloom.UnitTests/Host/)
- Replay planning and request execution -> [tests/Sqloom.UnitTests/Endpoints/](../../tests/Sqloom.UnitTests/Endpoints/), [tests/Sqloom.UnitTests/Host/ReplayArgumentParserTests.cs](../../tests/Sqloom.UnitTests/Host/ReplayArgumentParserTests.cs), [tests/Sqloom.IntegrationTests/Host/HostRuntimeTests.cs](../../tests/Sqloom.IntegrationTests/Host/HostRuntimeTests.cs)
- Query Store collection and classification -> [tests/Sqloom.UnitTests/QueryStore/](../../tests/Sqloom.UnitTests/QueryStore/)
- Correlation -> [tests/Sqloom.UnitTests/QueryStore/QueryStoreCorrelatorTests.cs](../../tests/Sqloom.UnitTests/QueryStore/QueryStoreCorrelatorTests.cs), [tests/Sqloom.UnitTests/QueryStore/QueryStoreCorrelationAdvisorTests.cs](../../tests/Sqloom.UnitTests/QueryStore/QueryStoreCorrelationAdvisorTests.cs), [tests/Sqloom.UnitTests/Host/CorrelateArgumentParserTests.cs](../../tests/Sqloom.UnitTests/Host/CorrelateArgumentParserTests.cs)
- Advice, OpenAI evidence, DACPAC schema extraction, and SQL proposals -> [tests/Sqloom.UnitTests/Host/AdviceCommandTests.cs](../../tests/Sqloom.UnitTests/Host/AdviceCommandTests.cs), [tests/Sqloom.UnitTests/Host/OpenAIAdviceGeneratorTests.cs](../../tests/Sqloom.UnitTests/Host/OpenAIAdviceGeneratorTests.cs), [tests/Sqloom.UnitTests/Host/SqlServerDacpacSchemaExtractorTests.cs](../../tests/Sqloom.UnitTests/Host/SqlServerDacpacSchemaExtractorTests.cs), [tests/Sqloom.IntegrationTests/Host/HostCatalogAdviceTests.cs](../../tests/Sqloom.IntegrationTests/Host/HostCatalogAdviceTests.cs)
- Artifacts and public JSON contracts -> [tests/Sqloom.UnitTests/Artifacts/](../../tests/Sqloom.UnitTests/Artifacts/)
- Packaging and local tool behavior -> [scripts/](../../scripts/), [docs/dotnet-tool-release.md](../../docs/dotnet-tool-release.md), [README.md](../../README.md), plus build/pack/local-wrapper smoke commands

## Commands

- Restore: `dotnet restore .\Sqloom.slnx`
- Build: `dotnet build .\Sqloom.slnx --tl:off --nologo "-clp:ErrorsOnly;NoSummary"`
- Unit tests: `dotnet test --solution .\Sqloom.UnitTests.slnf`
- Integration tests: `dotnet test --solution .\Sqloom.IntegrationTests.slnf`
- Local tool deploy: `pwsh .\scripts\deploy-sqloom-local.ps1`
- Local tool smoke: `sqloom-local --version`

## Navigation Rules

- Follow the runtime flow first for command behavior, then use RoslynKit or direct line reads for the narrow unclear hop.
- Read nearby tests before implementation when a matching test exists.
- For public CLI, JSON artifact, package, configuration, or documented workflow changes, update [README.md](../../README.md) or the relevant docs in the same change.
- Inspect `artifacts/sqloom/` before guessing about replay, correlate, advise, or tune behavior from code alone.
- Use [.agents/skills/roslynkit/SKILL.md](../../.agents/skills/roslynkit/SKILL.md) for C# semantic inspection when the current environment exposes RoslynKit and the task benefits from symbols, definitions, references, implementations, quick info, or line-range reads.
- Do not use Atlas as a file inventory, test inventory, symbol graph, reference graph, artifact cache, or source cache.
- Ignore first: `artifacts/`, `TestResults/`, `.vs/`, `.tools/`, `bin/`, `obj/`, and package output unless the task is explicitly about generated artifacts, local tooling, or packaging.

Last verified: `2026-07-07`
