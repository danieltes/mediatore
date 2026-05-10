# Implementation Plan: In-Process Mediator Library for .NET (CQRS)

**Branch**: `00001-mediator-core` | **Date**: 2026-05-09 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/00001-mediator-core/spec.md`

## Summary

A lightweight, zero-dependency, in-process mediator library for .NET 10. It implements the
Mediator pattern (request/response, command, notification fan-out, streaming) with an
ordered pipeline behavior mechanism for cross-cutting concerns. The core library (`Mediatore`)
has zero external runtime dependencies; DI integration lives in a separate package
(`Mediatore.Extensions.DependencyInjection`); an optional Roslyn source generator
(`Mediatore.SourceGenerator`) eliminates reflection-based registration and produces zero heap
allocations on the hot dispatch path.

---

## Technical Context

**Language/Version**: C# 14 (latest) / .NET 10 (`net10.0`)
**Primary Dependencies**:
- Core: none (zero external runtime deps — FR-013)
- DI package: `Microsoft.Extensions.DependencyInjection.Abstractions`
- Source generator: `Microsoft.CodeAnalysis.CSharp` (build-time only, no runtime weight)
- Tests: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Assertivo 0.3.0`
- Benchmarks: `BenchmarkDotNet` (latest, `RuntimeMoniker.Net10_0`)

**Storage**: N/A — in-process library
**Testing**: xUnit v3 with Assertivo 0.3.0 (primary); xUnit assertions (fallback)
**Target Platform**: .NET 10 (`net10.0`) — single TFM, no multi-targeting in v1
**Project Type**: NuGet library (3 packages: core, DI integration, source generator)
**Solution Format**: SLNX (`.slnx`) — `dotnet new sln --name Mediatore --format slnx`
**Performance Goals**:
- Handler dispatch overhead: < 1 µs per call (no-op handler, source-gen mode)
- Zero heap allocations on hot path (source-gen mode, no behaviors)
- ≤ 1 allocation per `Send` (reflection mode — the response `Task`)
- Startup registration of 1 000 handlers: < 5 ms (source-gen), < 100 ms (reflection)

**Constraints**:
- Zero external runtime dependencies in core package
- Thread-safe; safe for `Singleton` DI lifetime registration
- Handler registry immutable after mediator build (backed by `FrozenDictionary<Type, ...>`)
- No logging inside the library core
- No ambient/static mediator access

**Scale/Scope**: Single-process library; no distributed or cross-process concerns

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Spec-First (Art. II)**: `specs/00001-mediator-core/spec.md` exists and is the authoritative
      specification before any production code is written.
- [x] **Mediator Contract (Art. III)**: `IRequest<TResponse>` → exactly one handler;
      `INotification` → zero or more handlers; all message types are closed, distinct, typed.
- [x] **Explicit over Implicit (Art. IV.1)**: `RegisterServicesFromAssembly` is opt-in; source
      generator is opt-in; no auto-discovery in core behavior.
- [x] **Fail at Config Time (Art. IV.3)**: `DuplicateHandlerException` raised at mediator build
      time (reflection path); source-gen emits `Error` diagnostic (compile time). Note:
      `HandlerNotFoundException` is necessarily a dispatch-time error (no handler known at build
      time in the reflection path) — acceptable per spec FR-003.
- [x] **Pipeline Only (Art. V)**: No logging, validation, or authorization in library core; all
      cross-cutting concerns are consumer-registered `IPipelineBehavior<,>` implementations.
- [x] **No Swallowed Exceptions (Art. VI.1)**: `SequentialPublisher` propagates on first
      exception; `ParallelPublisher` aggregates all into `AggregateException`; core never catches
      handler exceptions.
- [x] **Tests Required (Art. VII)**: Unit, Integration, and Contract test projects planned;
      all tests target public API only.
- [x] **Dependency Justified (Art. IX.1)**: One runtime dependency
      (`Microsoft.Extensions.DependencyInjection.Abstractions`, DI package only) — entry required
      in `DEPENDENCIES.md`.

**Post-Design Re-check**: All gates still pass. No violations.

---

## Project Structure

### Documentation (this feature)

```text
specs/00001-mediator-core/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research decisions
├── data-model.md        # Phase 1 entity & type model
├── quickstart.md        # Phase 1 developer quickstart
├── contracts/
│   └── api-contract.md  # Phase 1 public API contract
├── checklists/
│   └── requirements.md  # Specification quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
Mediatore.slnx                              # SLNX solution file

src/
  Mediatore/                                # Core library — NuGet: Mediatore
    Abstractions/
      IMediator.cs
      IRequest.cs
      ICommand.cs
      INotification.cs
      IStreamRequest.cs
      IRequestHandler.cs
      ICommandHandler.cs
      INotificationHandler.cs
      IStreamRequestHandler.cs
      IPipelineBehavior.cs
      INotificationPublisher.cs
    Models/
      Unit.cs
      NotificationHandlerExecutor.cs
    Exceptions/
      MediatorException.cs
      HandlerNotFoundException.cs
      DuplicateHandlerException.cs
    Publishing/
      SequentialPublisher.cs
      ParallelPublisher.cs
    Internal/
      HandlerRegistry.cs           # FrozenDictionary-backed registry
      PipelineBuilder.cs           # Assembles behavior chain
      Mediator.cs                  # IMediator implementation
    Mediatore.csproj

  Mediatore.Extensions.DependencyInjection/  # DI package — NuGet: Mediatore.Extensions.DependencyInjection
    MediatorServiceCollectionExtensions.cs
    MediatorOptions.cs
    AssemblyScanner.cs                       # Reflection-based handler discovery
    Mediatore.Extensions.DependencyInjection.csproj

  Mediatore.SourceGenerator/                 # Source generator — NuGet: Mediatore.SourceGenerator
    MediatorHandlerGenerator.cs              # IIncrementalGenerator implementation
    Diagnostics.cs                           # MED0001, MED0002 descriptors
    Mediatore.SourceGenerator.csproj

tests/
  Mediatore.UnitTests/                       # Unit tests (single handler/behavior, no real mediator)
    RequestDispatch/
    PipelineBehavior/
    NotificationPublishing/
    StreamRequests/
    Mediatore.UnitTests.csproj

  Mediatore.IntegrationTests/                # Integration tests (real DI + dispatch cycle)
    RequestHandlerScenarios/
    PipelineScenarios/
    NotificationScenarios/
    StreamScenarios/
    LifetimeScenarios/
    Mediatore.IntegrationTests.csproj

  Mediatore.ContractTests/                   # Contract tests (spec invariants — never deleted)
    Contracts/
      SingleHandlerResolutionTests.cs        # REQ-01
      UnregisteredRequestTests.cs           # REQ-02
      PipelineOrderTests.cs                 # REQ-03
      NotificationFanOutTests.cs            # REQ-04
      CancellationPropagationTests.cs       # REQ-05
      StreamingResponseTests.cs             # REQ-06
      ScopedLifetimeTests.cs               # REQ-07
    Mediatore.ContractTests.csproj

  Mediatore.Benchmarks/                      # BenchmarkDotNet suite
    MediatorBenchmarks.cs
    PipelineBenchmarks.cs
    Mediatore.Benchmarks.csproj

samples/
  CQRS.Sample/                               # ASP.NET Core API demonstrating the library
    CQRS.Sample.csproj

DEPENDENCIES.md                              # Required by Constitution Art. IX.1
```

**Structure Decision**: Multi-project single-solution layout. Three shippable packages (`src/`)
separated by concern; three test projects (`tests/`) matching the three test categories from
Constitution Art. VII.3; one benchmark project; one sample project. The `Internal/` folder in the
core package houses implementation details with no stability guarantee.

---

## Complexity Tracking

> No constitution violations — this section is informational only.

| Decision | Justification |
|---|---|
| 3 NuGet packages instead of 1 | Required by Constitution Art. IX.3 (DI integrations are separate packages) and FR-013 (zero external deps on core) |
| `FrozenDictionary` for handler registry | Part of .NET 8+ BCL (`System.Collections.Frozen`) — zero external dependency; provides optimal read-only lookup performance |
| `IIncrementalGenerator` (source gen) | `ISourceGenerator` is `[Obsolete]`; incremental generator is the only non-deprecated Roslyn API |
