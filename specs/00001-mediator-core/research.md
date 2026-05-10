# Research: In-Process Mediator Library for .NET (CQRS)

**Phase**: 0 — Pre-Design Research
**Date**: 2026-05-09
**Feature**: [spec.md](spec.md)

---

## Decision 1: Source Generator API — `IIncrementalGenerator`

**Decision**: Use `IIncrementalGenerator` exclusively.

**Rationale**: `ISourceGenerator` is marked `[Obsolete]` in all current Roslyn versions.
`IIncrementalGenerator` provides caching via `IncrementalValueProvider<T>` / `IncrementalValuesProvider<T>`,
meaning the generator only re-runs when its input data actually changes — critical for IDE
responsiveness. The single entry point `Initialize(IncrementalGeneratorInitializationContext)` is
the only supported non-deprecated path.

**Key APIs**:
- `context.SyntaxProvider.CreateSyntaxProvider()` — syntax-based node filtering
- `context.CompilationProvider` — semantic analysis (symbol resolution, attribute lookup)
- `context.RegisterSourceOutput()` — emit generated source
- `context.RegisterPostInitializationOutput()` — emit marker attributes visible to user code
- `.WithTrackingName()` — for incremental cache diagnostics

**Duplicate handler detection**: The generator compares `INamedTypeSymbol` implementations of
`IRequestHandler<TRequest,TResponse>`. When two symbols map to the same closed `TRequest` type, it
emits a `Diagnostic` with `DiagnosticSeverity.Error` and a clear message that includes both
handler type names and the conflicting request type.

**Alternatives considered**:
- `ISourceGenerator` — rejected; marked obsolete, no caching, poor IDE performance.

---

## Decision 2: Solution File Format — SLNX

**Decision**: Use `.slnx` (SLNX format) as specified.

**Rationale**: SLNX is the modern XML-based solution format supported from .NET 6 SDK onwards,
now the default in Visual Studio 2026+. It is human-readable, merge-friendly, and tool-optimised.
BenchmarkDotNet itself has already migrated to `.slnx`.

**Creation command**:
```
dotnet new sln --name Mediatore --format slnx
```

**Project add command** (unchanged from `.sln`):
```
dotnet sln Mediatore.slnx add src/Mediatore/Mediatore.csproj
```

**Alternatives considered**:
- Legacy `.sln` — rejected; XML format preferred by the user, aligned with modern .NET tooling.

---

## Decision 3: Assertion Library — Assertivo 0.3.0

**Decision**: Use `Assertivo` (latest: `0.3.0`) as the primary assertion library; fall back to
xUnit assertions only when Assertivo cannot express the assertion.

**Rationale**: Assertivo provides a fluent `.Should()` API compatible with .NET 10. It is:
- AOT-compatible and trim-safe
- Zero-allocation on happy-path assertions
- Owned by the same organization (`danieltes`) — aligned with project ownership
- Already targets `net10.0`

**API surface used in this project**:
```csharp
// Value equality
result.Should().Be(expected);
result.Should().NotBe(null);

// Collections
handlers.Should().HaveCount(2);
handlers.Should().Contain(h);

// Exceptions (sync)
act.Should().Throw<HandlerNotFoundException>();

// Exceptions (async)
await act.Should().ThrowAsync<HandlerNotFoundException>();

// Chained on .Which
await act.Should().ThrowAsync<DuplicateHandlerException>()
    .Which.Message.Should().Contain("GetProductQuery");

// Booleans
wasCalled.Should().BeTrue();
```

**Fallback to xUnit** (only when Assertivo cannot be used):
- Checking exact `Assert.Equal` for struct value types where `.Should().Be()` is ambiguous
- Any scenario where Assertivo throws unexpectedly and the assertion must not fail the test itself

**Alternatives considered**:
- FluentAssertions — rejected; v7+ requires a commercial licence for non-OSS projects.
- Shouldly — rejected; not specified by the user.
- xUnit only — rejected; user explicitly requested Assertivo as primary.

---

## Decision 4: Test Framework — xUnit v3

**Decision**: Use xUnit v3 (latest stable for .NET 10) as the test runner.

**Rationale**: xUnit v3 is the standard .NET test framework with first-class .NET 10 / `net10.0`
support, native `async` test method support, and `IAsyncLifetime` for setup/teardown. It is
compatible with Assertivo (Assertivo does not depend on any specific runner).

**NuGet packages**:
- `xunit` — test attributes and runner abstractions
- `xunit.runner.visualstudio` — VS Test Explorer integration
- `Microsoft.NET.Test.Sdk` — SDK integration

**Alternatives considered**:
- NUnit — not requested, no advantage here.
- MSTest — not requested.

---

## Decision 5: Benchmark Framework — BenchmarkDotNet

**Decision**: Use BenchmarkDotNet (latest) with `RuntimeMoniker.Net10_0`.

**Rationale**: Specified in the original feature description and already supports .NET 10 with
`RuntimeMoniker.Net10_0` and `CsProjCoreToolchain.NetCoreApp10_0`. No known compatibility issues.

**Configuration**:
```csharp
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
public class MediatorBenchmarks { ... }
```

**Alternatives considered**: None — BenchmarkDotNet is the de-facto standard for .NET microbenchmarks.

---

## Decision 6: Package Structure — Multi-Project NuGet

**Decision**: Three independently publishable NuGet packages.

| Package | NuGet ID | Contents |
|---|---|---|
| Core | `Mediatore` | Abstractions + pipeline + publishing + dispatch internals |
| DI Integration | `Mediatore.Extensions.DependencyInjection` | `IServiceCollection` extensions, `AddMediator()` |
| Source Generator | `Mediatore.SourceGenerator` | Roslyn `IIncrementalGenerator` for compile-time registration |

**Rationale**:
- FR-013 requires zero external deps on the core; the DI package takes the MS.Extensions dep.
- Constitution Art. IX.3 mandates DI integrations are separate packages.
- The source generator is an SDK-style generator package, delivered as a build-time tool via
  `<IncludeAssets>analyzers</IncludeAssets>` — it adds zero runtime weight to consumers.

**Alternatives considered**:
- Single package with optional DI — rejected; would impose `Microsoft.Extensions.DependencyInjection.Abstractions` on all consumers.

---

## Decision 7: `Unit` Type Representation

**Decision**: `Unit` is a `readonly record struct` with a static `Value` singleton.

**Rationale**: A `record struct` provides structural equality, zero allocation, and JSON
serialisation support for free. The static singleton avoids repeated `new Unit()` calls on hot
paths. `ICommand` is defined as `IRequest<Unit>`.

```csharp
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
```

**Alternatives considered**:
- `class` — rejected; allocates on every command response.
- Using `void` directly — rejected; makes the generic pipeline impossible to unify.

---

## Decision 8: Notification Dispatch Error Policy

**Decision**: `SequentialPublisher` propagates on first exception (stops execution);
`ParallelPublisher` wraps all exceptions in `AggregateException`.

**Rationale**: Consistent with FR-012 and the clarified spec. No fire-and-forget publisher is
included (excluded in clarifications Q2). The `INotificationPublisher` interface is public,
allowing consumers to implement custom strategies.

**Alternatives considered**:
- Collect-and-aggregate for sequential — rejected; spec clarification explicitly chose halt-on-first.

---

## Decision 9: Minimum Viable Concurrency Model

**Decision**: The mediator's handler registry is an immutable dictionary built at startup.
All dispatch operations are lock-free reads followed by handler invocation. Thread safety
derives from immutability, not from synchronisation primitives.

**Rationale**: Confirmed safe for `Singleton` registration (clarification Q5). No `lock`, no
`ConcurrentDictionary` on the hot path — just a `FrozenDictionary<Type, ...>` (available in
`System.Collections.Frozen`, part of .NET 8+ BCL, no external dep).

**Alternatives considered**:
- `ConcurrentDictionary` — rejected; unnecessary complexity for an immutable registry.
- `ImmutableDictionary` — rejected; slightly slower lookup than `FrozenDictionary`.
