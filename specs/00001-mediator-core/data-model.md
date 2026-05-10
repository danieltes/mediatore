# Data Model: In-Process Mediator Library for .NET (CQRS)

**Phase**: 1 — Design
**Date**: 2026-05-09
**Feature**: [spec.md](spec.md) | [research.md](research.md)

---

## Core Message Types

### `Unit`

| Field | Type | Notes |
|---|---|---|
| *(none)* | — | Empty value type; static singleton `Unit.Value` |

- **Kind**: `readonly record struct`
- **Purpose**: Uniform return type for void-returning commands; enables a single generic pipeline
  without special-casing `void`.
- **State transitions**: None — stateless value.
- **Validation**: None.

---

### `IRequest<TResponse>` *(marker interface)*

| Aspect | Detail |
|---|---|
| Type parameter | `TResponse` — the expected response type |
| Constraint | `TResponse` is unconstrained; any type is valid |
| Relationship | Base for all request messages; `ICommand` is a specialisation |
| Cardinality | Exactly one `IRequestHandler<TRequest, TResponse>` per closed generic type |

---

### `ICommand` *(marker interface)*

| Aspect | Detail |
|---|---|
| Extends | `IRequest<Unit>` |
| Purpose | Syntactic shortcut for void-returning commands |
| Handler interface | `ICommandHandler<TCommand>` (adapter over `IRequestHandler<TCommand, Unit>`) |

---

### `INotification` *(marker interface)*

| Aspect | Detail |
|---|---|
| Type parameter | None — notification carries its own payload as fields |
| Cardinality | Zero or more `INotificationHandler<TNotification>` per closed type |
| Dispatch | Delegated to configured `INotificationPublisher` |

---

### `IStreamRequest<TResponse>` *(marker interface)*

| Aspect | Detail |
|---|---|
| Type parameter | `TResponse` — type of each item in the stream |
| Cardinality | Exactly one `IStreamRequestHandler<TRequest, TResponse>` per closed type |
| Return | `IAsyncEnumerable<TResponse>` — lazy, cancellable |

---

## Handler Types

### `IRequestHandler<TRequest, TResponse>`

| Aspect | Detail |
|---|---|
| Generic constraints | `TRequest : IRequest<TResponse>` |
| Method | `Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)` |
| Lifetime | Configurable (default `Scoped`) |
| Registration rule | Exactly one per closed `TRequest` type; duplicates → `DuplicateHandlerException` at build |

---

### `ICommandHandler<TCommand>`

| Aspect | Detail |
|---|---|
| Generic constraints | `TCommand : ICommand` |
| Method | `Task Handle(TCommand command, CancellationToken cancellationToken)` |
| Implementation | Adapter; internally registered as `IRequestHandler<TCommand, Unit>` |

---

### `INotificationHandler<TNotification>`

| Aspect | Detail |
|---|---|
| Generic constraints | `TNotification : INotification` |
| Method | `Task Handle(TNotification notification, CancellationToken cancellationToken)` |
| Cardinality | Zero or more per closed type; all are invoked on `Publish` |

---

### `IStreamRequestHandler<TRequest, TResponse>`

| Aspect | Detail |
|---|---|
| Generic constraints | `TRequest : IStreamRequest<TResponse>` |
| Method | `IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken)` |
| Lifetime | Configurable (default `Scoped`) |

---

## Pipeline Types

### `IPipelineBehavior<TRequest, TResponse>`

| Aspect | Detail |
|---|---|
| Generic constraints | `TRequest : IRequest<TResponse>` |
| Method | `Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)` |
| Invariant | MUST call `next` exactly once on the happy path; MAY short-circuit intentionally |
| Order | Applied in registration order; consistent across all calls for the same request type |
| Scope | Typed — does NOT apply to `INotification` dispatch |

---

### `RequestHandlerDelegate<TResponse>`

| Aspect | Detail |
|---|---|
| Kind | `delegate Task<TResponse>()` |
| Purpose | Represents the next stage in the pipeline (next behavior or the handler itself) |

---

## Publishing Types

### `INotificationPublisher`

| Aspect | Detail |
|---|---|
| Method | `Task Publish(IEnumerable<NotificationHandlerExecutor> handlers, INotification notification, CancellationToken cancellationToken)` |
| Built-in implementations | `SequentialPublisher`, `ParallelPublisher` |
| Default | `SequentialPublisher` |
| Invariant | Always present; mediator does not operate without one |

---

### `NotificationHandlerExecutor`

| Field | Type | Notes |
|---|---|---|
| `HandlerCallback` | `Func<INotification, CancellationToken, Task>` | Wrapped handler invocation |
| `HandlerType` | `Type` | Concrete handler type, for diagnostics |

---

### `SequentialPublisher`

| Aspect | Detail |
|---|---|
| Strategy | Awaits each `HandlerCallback` in registration order |
| On first exception | Propagates immediately; subsequent handlers are NOT invoked |

---

### `ParallelPublisher`

| Aspect | Detail |
|---|---|
| Strategy | `Task.WhenAll` across all `HandlerCallback` invocations |
| On exception | All exceptions aggregated into `AggregateException` |

---

## Mediator Type

### `IMediator`

| Method | Signature | Notes |
|---|---|---|
| `Send` | `Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)` | Routes to handler through pipeline |
| `Send` (command) | `Task Send(ICommand command, CancellationToken ct = default)` | Convenience overload; returns `Task` (wraps `Unit`) |
| `Publish` | `Task Publish<TNotification>(TNotification notification, CancellationToken ct = default) where TNotification : INotification` | Fan-out via `INotificationPublisher` |
| `CreateStream` | `IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default)` | Returns lazy enumerable |

**Thread safety**: Thread-safe; registry is a `FrozenDictionary` (immutable after build).
**Lifetime**: Safe as `Singleton`.

---

## Exception Hierarchy

### `MediatorException` *(abstract base)*

| Aspect | Detail |
|---|---|
| Base | `InvalidOperationException` |
| Purpose | Root of library exception hierarchy; allows consumers to catch all library exceptions with one catch clause |

---

### `HandlerNotFoundException`

| Field | Type | Notes |
|---|---|---|
| `RequestType` | `Type` | The request type for which no handler was found |

- **Thrown**: At dispatch time (`Send`/`CreateStream`) when registry lookup returns nothing.
- **Invariant**: Never thrown for `Publish` (zero handlers is valid for notifications).

---

### `DuplicateHandlerException`

| Field | Type | Notes |
|---|---|---|
| `RequestType` | `Type` | The request type with duplicate registrations |
| `HandlerTypes` | `IReadOnlyList<Type>` | All handler types competing for the same request type |

- **Thrown**: At mediator build time (registration phase), not at dispatch time.
- **Source-gen path**: Emitted as a compiler `Error` diagnostic before build succeeds.

---

## Configuration / Registration

### `MediatorOptions`

| Property | Type | Default | Notes |
|---|---|---|---|
| `Lifetime` | `ServiceLifetime` | `Scoped` | Lifetime applied to all registered handlers |
| `NotificationPublisher` | `INotificationPublisher` | `new SequentialPublisher()` | Publisher strategy instance |

---

## State Transitions

```
MediatorOptions configured
        │
        ▼
  [Build time] ──► DuplicateHandlerException (if duplicate IRequestHandler registrations)
        │
        ▼
  IMediator instance (registry frozen: FrozenDictionary)
        │
   ┌────┴─────────────────────────────────┐
   ▼                                      ▼
Send(IRequest<TResponse>)          Publish(INotification)
   │                                      │
Pipeline behaviors (ordered)      INotificationPublisher
   │                                      │
   ▼                                      ▼
IRequestHandler.Handle()        INotificationHandler[].Handle()
   │                                      │
   ▼                                      ▼
TResponse returned               Task completed (or exception propagated)
```

---

## Validation Rules

| Rule | Where Enforced |
|---|---|
| Exactly one `IRequestHandler` per request type | At build time (reflection) / compiler error (source-gen) |
| Zero or more `INotificationHandler` per notification type | Informational only; no error |
| `CancellationToken` must be forwarded | Invariant — enforced by implementation |
| Pipeline `next` called exactly once on happy path | Behavior author responsibility; not enforced by framework |
| `INotificationPublisher` must not be null | Validated during options configuration; defaults to `SequentialPublisher` |
