# Specification Review Checklist: API Contract & Implementation Readiness

**Purpose**: Thorough pre-implementation audit — tests requirement quality, not implementation
**Created**: 2026-05-09
**Depth**: Thorough (~50 items)
**Focus**: API Contract & Stability (deepest), Performance Requirements Quality (deepest)
**Audience**: Spec author self-review
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md)

---

## ⚠️ Blocking Defects ✅ Resolved

- [x] CHK001 — FR-015 restored with full text (source-gen requirement, hot-path definition,
  zero-alloc measurability via `[MemoryDiagnoser]`). Fixed in spec §FR-015.

- [x] CHK002 — FR-016 deduplicated; appears exactly once. Fixed in spec §FR-016.

---

## Requirement Completeness

- [x] CHK003 — FR-003 updated: `HandlerNotFoundException` MUST expose `RequestType` property and
  include type's fully qualified name in message; extended to cover `CreateStream`. [Spec §FR-003]

- [x] CHK004 — FR-004 updated: `DuplicateHandlerException` MUST expose `RequestType` and
  `HandlerTypes` (`IReadOnlyList<Type>`) and include both in message. [Spec §FR-004]

- [x] CHK005 — FR-014 updated: `ICommand`→`ICommandHandler<TCommand>`→`IRequestHandler<TCommand,
  Unit>` adapter relationship now explicitly required as a functional requirement. [Spec §FR-014]

- [x] CHK006 — Added to Assumptions: `DEPENDENCIES.md` format follows Constitution Art. IX.1
  (package name, version range, justification). [Spec §Assumptions]

- [x] CHK007 — Added to Assumptions: NuGet metadata defined in `.csproj` files per plan.md;
  spec does not govern packaging details. [Spec §Assumptions]

- [x] CHK008 — FR-003 extended: `HandlerNotFoundException` is raised for `CreateStream` with no
  registered `IStreamRequestHandler`. [Spec §FR-003]

- [x] CHK009 — Added to Invariants: exactly one `IStreamRequestHandler<TRequest, TResponse>` per
  closed `TRequest`; duplicates → `DuplicateHandlerException`; zero → `HandlerNotFoundException`.
  [Spec §Invariants]

- [x] CHK010 — FR-007 updated: open-generic `IPipelineBehavior<,>` registration explicitly
  supported and applies to all `IRequest<TResponse>` types including `ICommand`-derived requests.
  [Spec §FR-007]

---

## Requirement Clarity

- [x] CHK011 — FR-007 updated: "registration order" defined as order of `IPipelineBehavior<,>`
  service additions to the `IServiceCollection`. [Spec §FR-007]

- [x] CHK012 — FR-004 updated: "mediator build time" = `BuildServiceProvider()` / `IHost.Build()`,
  not lazily deferred to first `Send`. [Spec §FR-004]

- [x] CHK013 — FR-010 updated: "lazy enumeration" defined as `IStreamRequestHandler.Handle` not
  invoked until first `MoveNextAsync()`; no items buffered upfront. [Spec §FR-010]

- [x] CHK014 — FR-015 updated: "hot dispatch path" defined as zero-behavior dispatch in source-gen
  mode — handler invocation without pipeline traversal or DI resolution overhead. [Spec §FR-015]

- [x] CHK015 — SC-001 updated: "boilerplate" defined as registration attributes, per-handler DI
  calls, or generated partial classes; handler implementing only its interface is sufficient.
  [Spec §SC-001]

---

## API Contract & Stability *(deepest focus)*

- [x] CHK016 — Variance annotations (`in`/`out`) specified with exact C# signatures in
  contracts/api-contract.md. Stable public API per Constitution Art. VIII.
  [api-contract.md §Message Marker Interfaces, §Handler Interfaces]

- [x] CHK017 — `ICommand` stability: api-contract.md §Stability states "All interfaces in
  Mediatore namespace = Stable"; `ICommand` is in the `Mediatore` namespace. [api-contract.md]

- [x] CHK018 — `Send(ICommand)` is specified as an `IMediator` **interface member** (not an
  extension method) in api-contract.md §The Mediator. [api-contract.md §IMediator]

- [x] CHK019 — `NotificationHandlerExecutor` explicitly listed as Stable in api-contract.md
  §Stability Guarantees. [api-contract.md §Stability Guarantees]

- [x] CHK020 — Exception properties (`RequestType`, `HandlerTypes`, message format) listed as
  Stable in api-contract.md and now also required by spec §FR-003, §FR-004.
  [api-contract.md §Stability Guarantees, Spec §FR-003, §FR-004]

- [x] CHK021 — Added to Invariants: `IPipelineBehavior` applies to `ICommand`-derived requests;
  no special-cased pipeline for commands. Also explicit in FR-007 (open-generic support).
  [Spec §FR-007, §Invariants]

- [x] CHK022 — `INotificationPublisher` stability: api-contract.md §Stability states "All
  interfaces in Mediatore namespace = Stable". [api-contract.md §Stability Guarantees]

- [x] CHK023 — `MediatorOptions.NotificationPublisher` is `public INotificationPublisher
  NotificationPublisher { get; set; }` (mutable) per api-contract.md. [api-contract.md §MediatorOptions]

- [x] CHK024 — Added to Edge Cases: `null` `NotificationPublisher` in `MediatorOptions` raises
  `ArgumentNullException` during `AddMediator()`, before `BuildServiceProvider()`. [Spec §Edge Cases]

- [x] CHK025 — Tension documented in api-contract.md §Stability: `Mediatore.Generated.*` namespace
  is Unstable but `RegisterGeneratedHandlers` method signature is stable. Consumers call the
  method, not the namespace. [api-contract.md §Stability Guarantees]

- [x] CHK026 — Added to Assumptions: handler types discovered by `RegisterServicesFromAssembly`
  must be concrete, non-abstract, non-open-generic; abstract/open-generic types silently skipped.
  [Spec §Assumptions]

- [x] CHK027 — Added to Edge Cases: `CancellationToken` forwarded to `Handle` on first
  `MoveNextAsync()`; pre-cancelled token propagates `OperationCanceledException` from within
  handler iteration. [Spec §Edge Cases]

- [x] CHK028 — `SequentialPublisher` and `ParallelPublisher` are `sealed class` per
  api-contract.md §Built-in Publishers. [api-contract.md §Built-in Publishers]

- [x] CHK029 — Added to Invariants: dispatch is type-exact; handler for base type does not match
  derived type; polymorphic dispatch not supported in v1. [Spec §Invariants]

- [x] CHK030 — `RegisterServicesFromAssembly` and `RegisterServicesFromAssemblyContaining<T>()`
  are listed as public API in api-contract.md §MediatorOptions. All public API covered by SemVer
  per Constitution Art. VIII. [api-contract.md §MediatorOptions]

---

## Performance Requirements Quality *(deepest focus)*

- [x] CHK031 — SC-003 updated: "< 1 µs" is the **median** additional latency over 1 000 000
  BenchmarkDotNet iterations. [Spec §SC-003]

- [x] CHK032 — SC-003 updated: "no-op handler" = returning `Task.FromResult(Unit.Value)`
  immediately; baseline = direct `Func<Task<Unit>>` no-op invocation. [Spec §SC-003]

- [x] CHK033 — FR-015 updated: zero heap allocations verified by BenchmarkDotNet
  `[MemoryDiagnoser]`; `Allocated` column MUST report 0 bytes per operation. [Spec §FR-015]

- [x] CHK034 — Added to Assumptions: ≤ 1 allocation per `Send` in reflection mode is aspirational
  in v1, not a hard regression gate. [Spec §Assumptions]

- [x] CHK035 — Added to Assumptions: startup targets (< 5 ms / < 100 ms) are wall-clock time on
  single-core, fixed-frequency environment (same as benchmark assumption). [Spec §Assumptions]

- [x] CHK036 — Added to Assumptions: BenchmarkDotNet runs are manual, not CI-gated, in v1.
  [Spec §Assumptions]

- [x] CHK037 — Added to Out of Scope: `CreateStream` dispatch performance requirements deferred
  to a future spec. [Spec §Out of Scope]

- [x] CHK038 — Added to Out of Scope: `Publish` dispatch performance with N handlers deferred to
  a future spec. [Spec §Out of Scope]

---

## Requirement Consistency

- [x] CHK039 — Consistent: FR-004 defines "build time" = `BuildServiceProvider()`; invariant says
  "immutable after build" (after that same moment). No conflict. [Spec §FR-004, §Invariants]

- [x] CHK040 — FR-009 updated: `ParallelPublisher` wrapping into `AggregateException` explicitly
  stated as not swallowing — all exceptions preserved and reach the caller. [Spec §FR-009]

- [x] CHK041 — Added to Assumptions: BCL types (e.g., `FrozenDictionary<,>`) are not external
  runtime dependencies for FR-013 — part of .NET 10 runtime; no package reference required.
  [Spec §Assumptions]

- [x] CHK042 — Consistent: SC-004 "startup" = FR-004 `BuildServiceProvider()` / `IHost.Build()`.
  Same moment in the application lifecycle. [Spec §FR-004, §SC-004]

- [x] CHK043 — Out of Scope entry reworded: "Automatic handler discovery without any opt-in
  registration call" — makes clear that `RegisterServicesFromAssembly` (FR-011) IS provided;
  fully implicit scanning with no API call is what's excluded. [Spec §Out of Scope]

---

## Acceptance Criteria Quality

- [x] CHK044 — SC-002 updated: verified by integration test suite (T045, T078) confirming
  behavior registration requires only composition root changes, not handler file modifications.
  [Spec §SC-002]

- [x] CHK045 — SC-006 updated: verified by T079 — following `quickstart.md` verbatim in a fresh
  `dotnet new` project and confirming all dispatch paths produce working output. [Spec §SC-006]

---

## Scenario Coverage

- [x] CHK046 — US2 Acceptance Scenario 5 added: open-generic `IPipelineBehavior<,>` executes for
  both query and command dispatch. [Spec §US2 Acceptance Scenarios]

- [x] CHK047 — Added to Edge Cases: `CancellationToken` cancelled during behavior execution before
  `next` is called — `OperationCanceledException` propagates to caller; library does not
  intercept. [Spec §Edge Cases]

---

## Edge Case Coverage

- [x] CHK048 — Added to Edge Cases: `null` request to `Send`/`CreateStream` raises
  `ArgumentNullException` before handler lookup. [Spec §Edge Cases]

- [x] CHK049 — Added to Edge Cases: `null` notification to `Publish` raises `ArgumentNullException`
  before `INotificationPublisher` is invoked. [Spec §Edge Cases]

---

## Non-Functional Requirements

- [x] CHK050 — Added to Out of Scope: AOT compatibility and ILLink/trim safety not guaranteed in
  v1; source-gen path may be AOT-friendly by design but this is not spec-required. Formally
  deferred to a future spec. [Spec §Out of Scope]

- [x] CHK051 — Covered by CHK050 resolution: trim/ILLink safety explicitly deferred in Out of
  Scope. [Spec §Out of Scope]
