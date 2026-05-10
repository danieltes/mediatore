---
description: "Implementation task list — In-Process Mediator Library for .NET (CQRS)"
---

# Tasks: In-Process Mediator Library for .NET (CQRS)

**Input**: Design documents from `specs/00001-mediator-core/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/api-contract.md ✅ | quickstart.md ✅

**Tests**: Per Constitution Art. VII, every ratified spec MUST have at least one corresponding test
file. Tests are organized into three categories: Unit, Integration, and Contract. Contract tests
(`tests/Mediatore.ContractTests/`) are NEVER deleted — they may only be updated when the
underlying spec is formally revised.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing
of each story. The source generator (US feature) ships as Polish, after all four dispatch paths
are proven correct via reflection-based tests.

## Format: `[ID] [P?] [Story?] Description — file path`

- **[P]**: Can run in parallel (different files, no intra-phase dependency)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- **No story label**: Setup, Foundational, or Polish phase tasks

---

## Phase 1: Setup

**Purpose**: Create the SLNX solution, all project files, shared configuration, and
`DEPENDENCIES.md`. No user story work begins until this phase is complete.

- [X] T001 Create `Mediatore.slnx` via `dotnet new sln --name Mediatore --format slnx` at the repo root
- [X] T002 [P] Create `src/Mediatore/Mediatore.csproj` — `net10.0`, no external dependencies, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true`
- [X] T003 [P] Create `src/Mediatore.Extensions.DependencyInjection/Mediatore.Extensions.DependencyInjection.csproj` — `net10.0`, references Mediatore + `Microsoft.Extensions.DependencyInjection.Abstractions`
- [X] T004 [P] Create `src/Mediatore.SourceGenerator/Mediatore.SourceGenerator.csproj` — Roslyn analyzer project (`netstandard2.0`, `IsRoslynComponent true`), references `Microsoft.CodeAnalysis.CSharp`
- [X] T005 [P] Create `tests/Mediatore.UnitTests/Mediatore.UnitTests.csproj` — `net10.0`, references Mediatore, `xunit` v3, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Assertivo 0.3.0`
- [X] T006 [P] Create `tests/Mediatore.IntegrationTests/Mediatore.IntegrationTests.csproj` — `net10.0`, references Mediatore + Mediatore.Extensions.DependencyInjection, same test packages as T005
- [X] T007 [P] Create `tests/Mediatore.ContractTests/Mediatore.ContractTests.csproj` — `net10.0`, references Mediatore + Mediatore.Extensions.DependencyInjection, same test packages as T005
- [X] T008 [P] Create `tests/Mediatore.Benchmarks/Mediatore.Benchmarks.csproj` — `net10.0`, references Mediatore + Mediatore.Extensions.DependencyInjection, `BenchmarkDotNet` (latest)
- [X] T009 [P] Create `samples/CQRS.Sample/CQRS.Sample.csproj` — `net10.0`, references Mediatore + Mediatore.Extensions.DependencyInjection
- [X] T010 Add all 9 projects to `Mediatore.slnx` via `dotnet sln Mediatore.slnx add` (src/*, tests/*, samples/*)
- [X] T011 [P] Create `Directory.Build.props` at repo root — shared `<LangVersion>preview</LangVersion>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- [X] T012 [P] Create `DEPENDENCIES.md` at repo root — document `Microsoft.Extensions.DependencyInjection.Abstractions` (version `>= 10.0.0`) in `Mediatore.Extensions.DependencyInjection` package, include the exact `<PackageReference>` version chosen in T003, reason (IServiceCollection is defined in this package; inlining would duplicate BCL types), and Constitution Art. IX.1 reference
- [X] T072 [P] Create `tests/Mediatore.SourceGenerator.Tests/Mediatore.SourceGenerator.Tests.csproj` — `netstandard2.0`, references Mediatore.SourceGenerator, `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit` (latest), `xunit` v3, `Microsoft.NET.Test.Sdk`; add to `Mediatore.slnx`

**Checkpoint**: Solution builds (`dotnet build`) with no source files yet — only empty projects.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: All public abstractions, value types, exception hierarchy, built-in publishers,
handler registry, and DI configuration types. These are required by every user story.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Message Marker Interfaces

- [X] T013 [P] Implement `IRequest<out TResponse>` marker interface in `src/Mediatore/Abstractions/IRequest.cs`
- [X] T014 [P] Implement `ICommand : IRequest<Unit>` marker interface in `src/Mediatore/Abstractions/ICommand.cs`
- [X] T015 [P] Implement `INotification` marker interface in `src/Mediatore/Abstractions/INotification.cs`
- [X] T016 [P] Implement `IStreamRequest<out TResponse>` marker interface in `src/Mediatore/Abstractions/IStreamRequest.cs`

### Handler Interfaces

- [X] T017 [P] Implement `IRequestHandler<in TRequest, TResponse>` with `Handle` method signature in `src/Mediatore/Abstractions/IRequestHandler.cs`
- [X] T018 [P] Implement `ICommandHandler<in TCommand>` (adapter over `IRequestHandler<TCommand, Unit>`) in `src/Mediatore/Abstractions/ICommandHandler.cs`
- [X] T019 [P] Implement `INotificationHandler<in TNotification>` with `Handle` method signature in `src/Mediatore/Abstractions/INotificationHandler.cs`
- [X] T020 [P] Implement `IStreamRequestHandler<in TRequest, out TResponse>` returning `IAsyncEnumerable<TResponse>` in `src/Mediatore/Abstractions/IStreamRequestHandler.cs`

### Pipeline Interface

- [X] T021 [P] Implement `RequestHandlerDelegate<TResponse>` delegate and `IPipelineBehavior<in TRequest, TResponse>` interface in `src/Mediatore/Abstractions/IPipelineBehavior.cs`

### Publisher Interface

- [X] T022 [P] Implement `INotificationPublisher` interface in `src/Mediatore/Abstractions/INotificationPublisher.cs`

### Mediator Interface

- [X] T023 [P] Implement `IMediator` interface (Send, Publish, CreateStream overloads per api-contract.md) in `src/Mediatore/Abstractions/IMediator.cs`

### Value Types & Models

- [X] T024 [P] Implement `Unit` as `readonly record struct` with `static readonly Unit Value` singleton in `src/Mediatore/Models/Unit.cs`
- [X] T025 [P] Implement `NotificationHandlerExecutor` sealed class (`HandlerType`, `HandlerCallback`) in `src/Mediatore/Models/NotificationHandlerExecutor.cs`

### Exception Hierarchy

- [X] T026 [P] Implement `MediatorException` abstract class (base: `InvalidOperationException`, two protected constructors) in `src/Mediatore/Exceptions/MediatorException.cs`
- [X] T027 [P] Implement `HandlerNotFoundException` sealed class (`RequestType` property, message format per api-contract.md) in `src/Mediatore/Exceptions/HandlerNotFoundException.cs`
- [X] T028 [P] Implement `DuplicateHandlerException` sealed class (`RequestType`, `HandlerTypes` properties, message format per api-contract.md) in `src/Mediatore/Exceptions/DuplicateHandlerException.cs`

### Built-in Publishers

- [X] T029 [P] Implement `SequentialPublisher : INotificationPublisher` — awaits each `HandlerCallback` in order, propagates first exception immediately in `src/Mediatore/Publishing/SequentialPublisher.cs`
- [X] T030 [P] Implement `ParallelPublisher : INotificationPublisher` — `Task.WhenAll`, wraps all exceptions in `AggregateException` in `src/Mediatore/Publishing/ParallelPublisher.cs`

### Internal Registry

- [X] T031 Implement `HandlerRegistry` backed by `FrozenDictionary<Type, object>` — separate dictionaries for request handlers, notification handlers, and stream handlers; raises `DuplicateHandlerException` when duplicate `IRequestHandler<TRequest,TResponse>` registrations are detected for the same closed `TRequest` in `src/Mediatore/Internal/HandlerRegistry.cs`

### DI Configuration Types

- [X] T032 [P] Implement `MediatorOptions` (`Lifetime`, `NotificationPublisher`, `RegisterServicesFromAssembly`, `RegisterServicesFromAssemblyContaining<T>()`) in `src/Mediatore.Extensions.DependencyInjection/MediatorOptions.cs`
- [X] T033 Implement `AssemblyScanner` (reflection-based discovery of `IRequestHandler<,>`, `ICommandHandler<>`, `INotificationHandler<>`, `IStreamRequestHandler<,>` implementations) in `src/Mediatore.Extensions.DependencyInjection/AssemblyScanner.cs`

**Checkpoint**: Foundation ready — `dotnet build` succeeds across all projects. User story implementation can now begin.

---

## Phase 3: User Story 1 — Send a Request and Receive a Response (Priority: P1) 🎯 MVP

**Goal**: A developer defines `IRequest<TResponse>`, registers one handler, calls
`mediator.Send(request)`, and receives the typed response. `HandlerNotFoundException` is thrown
for unregistered types. `DuplicateHandlerException` is raised at mediator build time for duplicate
registrations.

**Independent Test**: A handler can be registered and dispatched in complete isolation — no
pipeline, no notifications — and the correct `TResponse` is returned.

### Tests for User Story 1 *(required per Constitution Art. VII)*

> **Write these tests FIRST and confirm they FAIL before implementing T039–T041.**
> Contract tests in `tests/Mediatore.ContractTests/` MUST NEVER be deleted.

- [X] T034 [P] [US1] Implement unit tests for request dispatch: single handler invoked exactly once, `ICommand` dispatched via `Send(ICommand)` convenience overload, `Unit` returned for commands — `tests/Mediatore.UnitTests/RequestDispatch/RequestDispatchTests.cs`
- [X] T035 [P] [US1] Implement contract test REQ-01 — `IRequest<TResponse>` type is resolved to exactly one handler; response matches handler return value — `tests/Mediatore.ContractTests/Contracts/SingleHandlerResolutionTests.cs`
- [X] T036 [P] [US1] Implement contract test REQ-02 — `Send` for an unregistered request type raises `HandlerNotFoundException` with correct `RequestType` property and message — `tests/Mediatore.ContractTests/Contracts/UnregisteredRequestTests.cs`
- [X] T037 [P] [US1] Implement contract test REQ-05 (request path) — `CancellationToken` passed to `Send` is forwarded unchanged to `IRequestHandler.Handle`; pre-cancelled token triggers `OperationCanceledException` before handler runs — `tests/Mediatore.ContractTests/Contracts/CancellationPropagationRequestTests.cs`
- [X] T038 [US1] Implement integration tests for full DI + dispatch cycle: `AddMediator`, resolve `IMediator`, `Send`, verify `DuplicateHandlerException` raised at `BuildServiceProvider` not at `Send` — `tests/Mediatore.IntegrationTests/RequestHandlerScenarios/RequestHandlerScenarioTests.cs`

### Implementation for User Story 1

- [X] T039 [US1] Implement `PipelineBuilder` — assembles a no-behavior delegate chain that resolves the handler from DI and invokes `Handle`; returns `RequestHandlerDelegate<TResponse>` — `src/Mediatore/Internal/PipelineBuilder.cs`
- [X] T040 [US1] Implement `Mediator` internal class — `Send<TResponse>` and `Send(ICommand)` dispatch via `HandlerRegistry` + `PipelineBuilder`, `CancellationToken` forwarding, `HandlerNotFoundException` on registry miss — `src/Mediatore/Internal/Mediator.cs`
- [X] T041 [US1] Implement `MediatorServiceCollectionExtensions.AddMediator` — registers `HandlerRegistry` (validates duplicates), scans assemblies via `AssemblyScanner`, registers `IMediator` implementation, wires `MediatorOptions` — `src/Mediatore.Extensions.DependencyInjection/MediatorServiceCollectionExtensions.cs`

**Checkpoint**: User Story 1 is fully functional and independently testable. `dotnet test tests/Mediatore.UnitTests` + `tests/Mediatore.ContractTests` + `tests/Mediatore.IntegrationTests` all pass for US1 scenarios.

---

## Phase 4: User Story 2 — Execute Cross-Cutting Concerns via Pipeline Behaviors (Priority: P2)

**Goal**: One or more `IPipelineBehavior<TRequest, TResponse>` implementations are registered in
order, wrap the handler execution, and execute in B1→B2→Handler→B2→B1 order. Short-circuiting
(not calling `next`) is supported.

**Independent Test**: A single behavior that records entry/exit order can be registered, a request
sent, and the execution sequence verified without notifications or streaming present.

### Tests for User Story 2 *(required per Constitution Art. VII)*

> Contract tests MUST NEVER be deleted.

- [X] T042 [P] [US2] Implement unit tests for pipeline order — three behaviors B1, B2, B3 registered in that order produce B1→B2→B3→Handler→B3-exit→B2-exit→B1-exit execution trace — `tests/Mediatore.UnitTests/PipelineBehavior/PipelineBehaviorOrderTests.cs`
- [X] T043 [P] [US2] Implement unit tests for pipeline short-circuit — behavior that does not call `next` returns result before handler executes; handler is not invoked — `tests/Mediatore.UnitTests/PipelineBehavior/PipelineShortCircuitTests.cs`
- [X] T044 [P] [US2] Implement contract test REQ-03 — behaviors execute in registration order for every invocation; order is consistent and not per-call random — `tests/Mediatore.ContractTests/Contracts/PipelineOrderTests.cs`
- [X] T045 [US2] Implement integration tests for pipeline scenarios — logging behavior, validation short-circuit, behavior after-execution code runs on handler throw — `tests/Mediatore.IntegrationTests/PipelineScenarios/PipelineScenarioTests.cs`

### Implementation for User Story 2

- [X] T046 [US2] Extend `PipelineBuilder` to compose ordered `IPipelineBehavior<TRequest, TResponse>` chain — resolves behaviors from DI in registration order, wraps handler delegate — `src/Mediatore/Internal/PipelineBuilder.cs`
- [X] T047 [US2] Update `AddMediator` to register `IPipelineBehavior<,>` types from `MediatorOptions` assemblies, preserving registration order for pipeline construction — `src/Mediatore.Extensions.DependencyInjection/MediatorServiceCollectionExtensions.cs`

**Checkpoint**: User Story 2 fully functional. Pipeline integration tests pass. US1 tests still pass (regression-free).

---

## Phase 5: User Story 3 — Publish a Notification to Multiple Handlers (Priority: P3)

**Goal**: A developer calls `mediator.Publish(notification)`. All registered
`INotificationHandler<TNotification>` implementations are invoked via the configured
`INotificationPublisher`. Zero handlers is not an error.

**Independent Test**: A notification with two handlers published with `SequentialPublisher` — both
handlers are invoked in registration order, independently of request dispatch.

### Tests for User Story 3 *(required per Constitution Art. VII)*

> Contract tests MUST NEVER be deleted.

- [X] T048 [P] [US3] Implement unit tests for `SequentialPublisher` — all handlers invoked in order, first exception propagates immediately and subsequent handlers are not called — `tests/Mediatore.UnitTests/NotificationPublishing/SequentialPublisherTests.cs`
- [X] T049 [P] [US3] Implement unit tests for `ParallelPublisher` — all handlers run concurrently, all exceptions wrapped in `AggregateException`, zero handlers completes without error — `tests/Mediatore.UnitTests/NotificationPublishing/ParallelPublisherTests.cs`
- [X] T050 [P] [US3] Implement contract test REQ-04 — `Publish` invokes all registered handlers; zero-handler `Publish` completes successfully with no exception — `tests/Mediatore.ContractTests/Contracts/NotificationFanOutTests.cs`
- [X] T051 [US3] Implement contract test REQ-05 (notification path) — `CancellationToken` propagation for `INotificationHandler.Handle`; pre-cancelled token propagates before any handler executes — `tests/Mediatore.ContractTests/Contracts/CancellationPropagationNotificationTests.cs` *(sequential after T037 — separate file, no write conflict)*
- [X] T052 [US3] Implement integration tests for notification scenarios — `SequentialPublisher` with two handlers, `ParallelPublisher` with concurrent handlers, exception propagation strategy, zero-handler scenario — `tests/Mediatore.IntegrationTests/NotificationScenarios/NotificationScenarioTests.cs`

### Implementation for User Story 3

- [X] T053 [US3] Implement `Mediator.Publish<TNotification>` — resolves all registered `INotificationHandler<TNotification>` from DI, wraps each in `NotificationHandlerExecutor`, delegates to `INotificationPublisher`; `CancellationToken` forwarded — `src/Mediatore/Internal/Mediator.cs`
- [X] T054 [US3] Update `AddMediator` to register all `INotificationHandler<>` types and the `INotificationPublisher` instance from `MediatorOptions.NotificationPublisher` — `src/Mediatore.Extensions.DependencyInjection/MediatorServiceCollectionExtensions.cs`

**Checkpoint**: User Story 3 fully functional. Notification integration tests pass. US1 + US2 tests still pass.

---

## Phase 6: User Story 4 — Consume a Streaming Response (Priority: P4)

**Goal**: A developer calls `mediator.CreateStream(request)` and receives an
`IAsyncEnumerable<TResponse>` that lazily yields items from the registered
`IStreamRequestHandler`. Mid-stream cancellation via `CancellationToken` is supported.

**Independent Test**: A stream handler yielding `[1, 2, 3]` is dispatched; iterating the
result returns exactly those items in order, without pipeline or notification infrastructure.

### Tests for User Story 4 *(required per Constitution Art. VII)*

> Contract tests MUST NEVER be deleted.

- [X] T055 [P] [US4] Implement unit tests for stream handler dispatch — handler yields items in declared order, enumeration is lazy (handler not invoked until first `MoveNextAsync`) — `tests/Mediatore.UnitTests/StreamRequests/StreamRequestTests.cs`
- [X] T056 [P] [US4] Implement unit tests for mid-stream cancellation — `OperationCanceledException` propagates when token is cancelled during iteration; no further items yielded — `tests/Mediatore.UnitTests/StreamRequests/StreamCancellationTests.cs`
- [X] T057 [P] [US4] Implement contract test REQ-06 — `CreateStream` returns lazy `IAsyncEnumerable<TResponse>`; `CancellationToken` forwarded to `IStreamRequestHandler.Handle`; `HandlerNotFoundException` thrown for unregistered stream request type — `tests/Mediatore.ContractTests/Contracts/StreamingResponseTests.cs`
- [X] T058 [US4] Implement contract test REQ-05 (stream path) — `CancellationToken` propagation for `IStreamRequestHandler.Handle`; pre-cancelled token propagates before handler is invoked — `tests/Mediatore.ContractTests/Contracts/CancellationPropagationStreamTests.cs` *(sequential after T037 — separate file, no write conflict)*
- [X] T059 [US4] Implement integration tests for stream scenarios — full DI + `CreateStream` dispatch, mid-stream cancellation, `HandlerNotFoundException` for missing stream handler — `tests/Mediatore.IntegrationTests/StreamScenarios/StreamScenarioTests.cs`

### Implementation for User Story 4

- [X] T060 [US4] Implement `Mediator.CreateStream<TResponse>` — resolves `IStreamRequestHandler<TRequest, TResponse>` from `HandlerRegistry`, invokes `Handle`, forwards `CancellationToken`; throws `HandlerNotFoundException` on registry miss — `src/Mediatore/Internal/Mediator.cs`
- [X] T061 [US4] Update `AddMediator` to register all `IStreamRequestHandler<,>` types from scanned assemblies — `src/Mediatore.Extensions.DependencyInjection/MediatorServiceCollectionExtensions.cs`

**Checkpoint**: All four user stories functional and independently tested. Full test suite (`dotnet test`) passes.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: DI lifetime contract tests, benchmarks, Roslyn source generator, sample project, and
NuGet package metadata. These concerns span all user stories.

### Lifetime Contract & Integration

- [X] T062 [P] Implement contract test REQ-07 — handler is resolved per scope (not reused across `IServiceScope` boundaries); `IMediator` registered as `Singleton` resolves handlers from correct scope — `tests/Mediatore.ContractTests/Contracts/ScopedLifetimeTests.cs`
- [X] T063 [P] Implement integration tests for DI lifetime scenarios — `Scoped` handler not shared between requests, `Singleton` `IMediator` safe across scopes, `MediatorOptions.Lifetime` respected — `tests/Mediatore.IntegrationTests/LifetimeScenarios/LifetimeScenarioTests.cs`
- [X] T076 [P] Implement concurrent dispatch integration test (FR-016) — create a `Singleton` `IMediator`; launch N concurrent `Task.Run` blocks (N = `Environment.ProcessorCount * 4`) each calling `Send` with a distinct request type; assert no exceptions, no corrupted responses, no cross-request state leakage — `tests/Mediatore.IntegrationTests/LifetimeScenarios/ConcurrentDispatchTests.cs`

### Benchmarks

- [X] T064 [P] Implement `MediatorBenchmarks` — `[SimpleJob(RuntimeMoniker.Net10_0)]`, `[MemoryDiagnoser]`, 1 million sequential `Send` calls (no-op handler, no behaviors); three baselines: (1) direct `Func<Task>` invocation (SC-003 comparison target), (2) reflection-mode mediator, (3) source-gen-mode mediator — `tests/Mediatore.Benchmarks/MediatorBenchmarks.cs`
- [X] T065 [P] Implement `PipelineBenchmarks` — dispatch overhead with 0, 1, and 3 behaviors; `[MemoryDiagnoser]` to count allocations per `Send` in reflection mode — `tests/Mediatore.Benchmarks/PipelineBenchmarks.cs`

### Source Generator

- [X] T066 [P] Implement `Diagnostics` — `MED0001` (`DiagnosticSeverity.Error`, duplicate handler message with both handler type names), `MED0002` (`DiagnosticSeverity.Warning`, non-sealed handler) — `src/Mediatore.SourceGenerator/Diagnostics.cs`
- [X] T067 Implement `MediatorHandlerGenerator : IIncrementalGenerator` — discovers all `IRequestHandler<,>` (including `ICommandHandler<>` implementations, registered as `IRequestHandler<TCommand, Unit>`), `INotificationHandler<>`, and `IStreamRequestHandler<,>` implementations via `SyntaxProvider`/`CompilationProvider`; emits `MediatorRegistrations.g.cs` with `RegisterGeneratedHandlers` extension method in `Mediatore.Generated` namespace; emits `MED0001` on duplicate registrations for same closed `TRequest`; emits `MED0002` for non-sealed handlers — `src/Mediatore.SourceGenerator/MediatorHandlerGenerator.cs`
### Source Generator Tests *(required per Constitution Art. VII — A3)*

> Contract tests MUST NEVER be deleted.

- [X] T073 [P] Implement analyzer test for MED0001 — compile a source fixture with two `IRequestHandler<GetProductQuery, Product>` implementations; assert one `Error` diagnostic with ID `MED0001` containing both handler type names and the request type — `tests/Mediatore.SourceGenerator.Tests/Diagnostics/DuplicateHandlerDiagnosticTests.cs`
- [X] T074 [P] Implement analyzer test for MED0002 — compile a source fixture with a non-`sealed` handler class; assert one `Warning` diagnostic with ID `MED0002` identifying the handler type — `tests/Mediatore.SourceGenerator.Tests/Diagnostics/NonSealedHandlerDiagnosticTests.cs`
- [X] T075 Implement source-output test for `RegisterGeneratedHandlers` — compile a source fixture with valid handlers; assert `MediatorRegistrations.g.cs` is emitted with `public static IServiceCollection RegisterGeneratedHandlers(this IServiceCollection services)` signature in `Mediatore.Generated` namespace; assert zero diagnostics — `tests/Mediatore.SourceGenerator.Tests/Output/GeneratedRegistrationsOutputTests.cs`
### Sample Project

- [X] T068 Implement `CQRS.Sample` demonstrating `GetProductQuery : IRequest<Product>`, `CreateOrderCommand : ICommand`, `OrderPlaced : INotification` with two handlers, `AddMediator` registration, and all four dispatch paths — `samples/CQRS.Sample/`

### NuGet Package Metadata

- [X] T069 [P] Configure NuGet package metadata in `src/Mediatore/Mediatore.csproj` — `<PackageId>Mediatore</PackageId>`, version `1.0.0`, MIT license, description, tags (`mediator cqrs dotnet pipeline`)
- [X] T070 [P] Configure NuGet package metadata in `src/Mediatore.Extensions.DependencyInjection/Mediatore.Extensions.DependencyInjection.csproj` — `<PackageId>Mediatore.Extensions.DependencyInjection</PackageId>`, version `1.0.0`, MIT license
- [X] T071 [P] Configure NuGet package metadata in `src/Mediatore.SourceGenerator/Mediatore.SourceGenerator.csproj` — `<PackageId>Mediatore.SourceGenerator</PackageId>`, version `1.0.0`, `<DevelopmentDependency>true</DevelopmentDependency>`, MIT license

### Acceptance Criterion Verification

- [X] T077 [P] Verify FR-013 zero-dependency constraint (SC-005) — after packing, run `dotnet list package --include-transitive` against the produced `Mediatore` nupkg; assert zero `<PackageReference>` entries in `Mediatore.csproj` and zero transitive dependencies in the pack output — document result in CI pipeline notes
- [X] T078 [P] Verify SC-002 — in the pipeline integration test suite (T045), assert that adding `LoggingBehavior` to `MediatorOptions` requires zero edits to any existing handler file; validate by reviewing `git diff --name-only` after behavior registration, confirming no handler files appear
- [X] T079 [P] Validate quickstart.md accuracy (SC-006) — follow every step in `quickstart.md` verbatim in a fresh `dotnet new console` project outside the repo; confirm all 8 sections produce working, compilable code; update quickstart.md if any step is stale — `specs/00001-mediator-core/quickstart.md`

**Final Checkpoint**: `dotnet test` passes for all projects (including `Mediatore.SourceGenerator.Tests`). `dotnet pack` succeeds for all three `src/` packages. `dotnet list package --include-transitive` on `Mediatore` nupkg shows zero dependencies. Quickstart.md validated end-to-end.

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
    │
    ▼
Phase 2 (Foundational) ◄── BLOCKS everything below
    │
    ▼
Phase 3 (US1 — P1) ────────────────────────────── MVP: stop here and validate
    │
    ▼
Phase 4 (US2 — P2)   Phase 5 (US3 — P3)   Phase 6 (US4 — P4)
    │                      │                      │
    └──────────────────────┴──────────────────────┘
                           │
                           ▼
                   Phase 7 (Polish)
```

### User Story Dependencies

| Story | Depends On | Notes |
|---|---|---|
| US1 (P1) | Foundational only | No other story dependency — true MVP |
| US2 (P2) | US1 complete | Extends `PipelineBuilder`; US1 dispatch must work first |
| US3 (P3) | Foundational only | Independent of US1/US2 — can start after Foundational |
| US4 (P4) | Foundational only | Independent of US1/US2/US3 — can start after Foundational |
| Polish | US1–US4 complete | Source generator validates all dispatch paths |

### Within Each User Story

1. Tests MUST be written and FAIL before implementation (Spec-First Law, Art. II)
2. Contract tests MUST cover the spec invariants REQ-NN referenced in plan.md
3. Implementation tasks depend on tests existing (even if failing)
4. Integration test depends on at minimum: unit tests written + core implementation wired into DI

---

## Parallel Execution Examples

### Parallel Example: User Story 1

```text
# Write all US1 tests simultaneously (different files, no intra-phase deps):
T034 — RequestDispatchTests.cs
T035 — SingleHandlerResolutionTests.cs
T036 — UnregisteredRequestTests.cs
T037 — CancellationPropagationRequestTests.cs (REQ-05, request path)

# Then implement in order:
T039 — PipelineBuilder.cs
T040 — Mediator.cs (depends on T039)
T041 — AddMediator (depends on T040)

# Run integration test after wiring:
T038 integration test (depends on T039–T041 complete)
```

### Parallel Example: User Story 2

```text
# All US2 tests in parallel:
T042 — PipelineBehaviorOrderTests.cs
T043 — PipelineShortCircuitTests.cs
T044 — PipelineOrderTests.cs (REQ-03 contract)

# Then extend implementation:
T046 — PipelineBuilder (behavior chain)
T047 — AddMediator (behavior registration)
```

### Parallel Example: User Stories 3 & 4 (parallel streams)

```text
# After Foundational complete, US3 and US4 can proceed simultaneously:

Developer A (US3):                           Developer B (US4):
T048 SequentialPublisher                     T055 StreamRequestTests
T049 ParallelPublisher                       T056 StreamCancellationTests
T050 NotificationFanOut REQ04                T057 StreamingResponse REQ06
T051 CancellationPropagationNotification     T058 CancellationPropagationStream
     (separate file — no write conflict)          (separate file — no write conflict)
T052 Mediator.Publish         T059 Integration
T053 AddMediator update       T060 Mediator.CreateStream
T054 Integration              T061 AddMediator update
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup — `dotnet build` succeeds
2. Complete Phase 2: Foundational — interfaces, types, registry, DI config
3. Complete Phase 3: User Story 1 — request/response dispatch via reflection
4. **STOP AND VALIDATE**: Run `dotnet test` for US1 test projects only
5. Confirm `IMediator.Send` works end-to-end with DI and returns typed response
6. **This is a shippable MVP** — `Mediatore` + `Mediatore.Extensions.DependencyInjection` 1.0.0-alpha

### Incremental Delivery

| Milestone | Stories Complete | Deliverable |
|---|---|---|
| Foundation | Foundational | Project skeleton compiles |
| MVP | US1 | `Send<TResponse>` works; core package shippable |
| +Pipeline | US1 + US2 | Cross-cutting behaviors (logging, validation) |
| +Events | US1–US3 | Full CQRS: commands + queries + events |
| +Streaming | US1–US4 | All dispatch paths; publish all 3 packages |
| +Source Gen | Polish | Zero-allocation path; analyzer diagnostics |

### Parallel Team Strategy

With two or more developers after Foundational is complete:

- **Developer A** → US1 then US2 (request path)
- **Developer B** → US3 and US4 in parallel (notification + streaming paths)
- **Merge** when all four US pass independently
- **Both** → Polish / source generator (sequential; source gen depends on all dispatch paths)
