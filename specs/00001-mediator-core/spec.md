# Feature Specification: In-Process Mediator Library for .NET (CQRS)

**Feature Branch**: `00001-mediator-core`
**Created**: 2026-05-09
**Status**: Draft

## Clarifications

### Session 2026-05-09

- Q: What is the target framework (TFM) for the library? → A: net10.0 only
- Q: Should `FireAndForgetPublisher` be included in the library core? → A: No — excluded; consumers implement `INotificationPublisher` for fire-and-forget use cases
- Q: Where does the `IServiceCollection` registration code live? → A: Separate package `Mediatore.Extensions.DependencyInjection`; core has zero external dependencies
- Q: What diagnostic severity does the source generator emit for duplicate handler registrations? → A: Compiler error (severity: Error) — blocks the build
- Q: Is the mediator instance thread-safe for concurrent use? → A: Yes — thread-safe; safe to register as Singleton and call from multiple concurrent contexts

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Send a Request and Receive a Response (Priority: P1)

A developer defines a query or command that implements `IRequest<TResponse>`, registers exactly one
handler for it, and calls `mediator.Send(request)`. The mediator routes the message to the correct
handler, executes the registered pipeline behaviors in order, and returns the handler's response to
the caller.

**Why this priority**: This is the core value proposition of the library. Without request/response
dispatch, no other feature has meaning. Every other story depends on this working first.

**Independent Test**: A handler can be registered and invoked in complete isolation — no other
message types, no pipeline, no notification publishers — and the correct `TResponse` is returned.
This constitutes a functional library MVP.

**Acceptance Scenarios**:

1. **Given** a request type `GetProductQuery` registered with one handler,
   **When** `mediator.Send(new GetProductQuery(42))` is called,
   **Then** the handler's `Handle` method is invoked exactly once and its result is returned to the
   caller.

2. **Given** a command type `CreateOrderCommand` (returns `Unit`) registered with one handler,
   **When** `mediator.Send(new CreateOrderCommand(...))` is called,
   **Then** the handler executes and the call completes without error.

3. **Given** a request type with no registered handler,
   **When** `mediator.Send(request)` is called,
   **Then** a `HandlerNotFoundException` is thrown before any handler code runs.

4. **Given** the same request type registered with two handlers,
   **When** the mediator is built (not when dispatch is called),
   **Then** a `DuplicateHandlerException` is raised.

---

### User Story 2 — Execute Cross-Cutting Concerns via Pipeline Behaviors (Priority: P2)

A developer registers one or more `IPipelineBehavior<TRequest, TResponse>` implementations in a
specific order. When any matching request is dispatched, each behavior wraps the next in the chain,
allowing pre- and post-processing (e.g., logging duration, validating input, auditing) without
modifying the handler.

**Why this priority**: Pipeline behaviors are the primary extension mechanism for non-functional
concerns. They unlock real-world use — validation, logging, authorization — without coupling those
concerns to handlers. This is the second most critical deliverable.

**Independent Test**: A single behavior that mutates a response or short-circuits dispatch can be
registered, a request can be sent, and the behavior's effect on the response (or prevention of
handler execution) can be verified independently of notifications or streaming.

**Acceptance Scenarios**:

1. **Given** three behaviors B1, B2, B3 registered in that order for a request type,
   **When** the request is sent,
   **Then** execution order is B1-entry → B2-entry → B3-entry → Handler → B3-exit → B2-exit →
   B1-exit.

2. **Given** a validation behavior registered before the handler,
   **When** an invalid request is sent,
   **Then** the behavior short-circuits (does not call `next`) and returns an error result before
   the handler is invoked.

3. **Given** a logging behavior,
   **When** a request completes or throws,
   **Then** the behavior's after-execution code runs regardless of whether the handler succeeded or
   threw.

4. **Given** no behaviors registered,
   **When** a request is sent,
   **Then** the handler executes directly with no measurable additional overhead from pipeline
   infrastructure.

5. **Given** an open-generic `IPipelineBehavior<,>` registered for all request types,
   **When** both a query request and a command request are dispatched,
   **Then** the behavior executes for both request types without any additional registration per
   type.

---

### User Story 3 — Publish a Notification to Multiple Handlers (Priority: P3)

A developer defines a notification that implements `INotification`, registers zero or more
handlers for it, and calls `mediator.Publish(notification)`. The mediator dispatches the event to
all registered handlers using the configured publishing strategy.

**Why this priority**: Notification fan-out enables domain events and side-effect patterns. It is
critical for CQRS completeness but is independent of request/response dispatch and pipeline
behavior.

**Independent Test**: A notification with two handlers can be published and both handlers verified
as invoked, using the default sequential publisher, completely independently of request dispatch.

**Acceptance Scenarios**:

1. **Given** a notification type `OrderPlaced` with two handlers (send email, update inventory),
   **When** `mediator.Publish(new OrderPlaced(...))` is called with the default sequential
   publisher,
   **Then** both handlers are invoked in registration order.

2. **Given** a notification type with zero registered handlers,
   **When** `mediator.Publish(notification)` is called,
   **Then** the call completes successfully with no error.

3. **Given** a notification with two handlers and the `ParallelPublisher` configured,
   **When** the notification is published,
   **Then** both handlers run concurrently and any exceptions are wrapped in `AggregateException`.

4. **Given** a notification handler that throws,
   **When** the notification is published with the default sequential publisher,
   **Then** the exception propagates immediately to the caller and subsequent handlers are not
   invoked.

---

### User Story 4 — Consume a Streaming Response (Priority: P4)

A developer defines a stream request implementing `IStreamRequest<TResponse>`, registers a handler
returning `IAsyncEnumerable<TResponse>`, and calls `mediator.CreateStream(request)`. The mediator
returns the stream immediately; items are produced lazily as the caller iterates.

**Why this priority**: Streaming is important for large result sets and real-time feeds but is an
additive concern. Request/response, pipeline, and notifications must be solid first.

**Independent Test**: A stream request handler that yields a fixed sequence of items can be
invoked, and the caller can verify exactly those items are received in order, without any
pipeline or notification infrastructure present.

**Acceptance Scenarios**:

1. **Given** a stream request handler that yields items `[1, 2, 3]`,
   **When** `mediator.CreateStream(request)` is iterated,
   **Then** items 1, 2, 3 are received in order and the stream completes.

2. **Given** a stream request with a `CancellationToken` that is cancelled mid-stream,
   **When** the caller iterates,
   **Then** an `OperationCanceledException` is thrown and no further items are yielded.

---

### Edge Cases

- A `CancellationToken` is cancelled before `Send` is called — `OperationCanceledException`
  propagates before any handler or behavior executes.
- A pipeline behavior calls `next` more than once — behavior contract violation; behavior is
  responsible for calling `next` exactly once. The library does not enforce this constraint
  internally (it is a behavior author responsibility, per Constitution Art. V.3).
- A handler throws a non-cancellation exception — the exception propagates to the caller
  unwrapped; the library does not catch or wrap it.
- Two different request types with the same handler class registered for each — this is valid
  only if two separate handler interfaces are implemented; the library registers them independently.
- A `Notification` is published from within a `RequestHandler` (re-entrancy) — this is permitted
  as long as the handler resolves the `IMediator` through dependency injection (not a static
  accessor).
- A `null` value passed as the `request` argument to `Send` or `CreateStream` raises
  `ArgumentNullException` before any handler lookup or pipeline construction.
- A `null` value passed as the `notification` argument to `Publish` raises `ArgumentNullException`
  before the `INotificationPublisher` is invoked.
- A `null` value assigned to `MediatorOptions.NotificationPublisher` raises `ArgumentNullException`
  during `AddMediator()` configuration, before `BuildServiceProvider()` is called.
- A `CancellationToken` passed to `CreateStream` is forwarded to `IStreamRequestHandler.Handle`
  when `MoveNextAsync()` is first called. If the token is already cancelled at that point,
  `OperationCanceledException` propagates from within the handler's iteration; the mediator does
  not pre-check the token before the first item is requested.
- A `CancellationToken` is cancelled while a pipeline behavior is executing before it calls
  `next` — the behavior is responsible for observing the token; `OperationCanceledException`
  propagates to the caller like any other exception; the library does not intercept it.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The library MUST provide an `IMediator` interface with `Send`, `Publish`, and
  `CreateStream` methods.
- **FR-002**: The library MUST route `IRequest<TResponse>` messages to exactly one registered
  `IRequestHandler<TRequest, TResponse>`.
- **FR-003**: The library MUST raise `HandlerNotFoundException` when `Send` or `CreateStream` is
  called for a request type with no registered handler. The exception MUST expose the request's
  `System.Type` as a `RequestType` property and MUST include the type's fully qualified name in
  the exception message.
- **FR-004**: The library MUST raise `DuplicateHandlerException` at mediator build time when two or
  more handlers are registered for the same `IRequest<TResponse>` type. "Mediator build time"
  means the moment the DI container is built (i.e., `BuildServiceProvider()` / `IHost.Build()`)
  — not lazily deferred to the first `Send` call. The exception MUST expose `RequestType` (the
  conflicting request type as `System.Type`) and `HandlerTypes` (all competing handler types as
  `IReadOnlyList<Type>`), and MUST include both in the exception message.
- **FR-005**: The library MUST invoke all registered `INotificationHandler<TNotification>`
  implementations when `Publish` is called, in the order determined by the configured
  `INotificationPublisher`.
- **FR-006**: The library MUST support zero notification handlers without error.
- **FR-007**: The library MUST execute registered `IPipelineBehavior<TRequest, TResponse>`
  implementations in registration order, wrapping the handler. "Registration order" is defined as
  the order in which `IPipelineBehavior<,>` services are added to the `IServiceCollection`.
  Open-generic registration (registering `IPipelineBehavior<,>` without closed type arguments,
  applying the behavior to all `IRequest<TResponse>` types) is supported, including for
  `ICommand`-derived requests.
- **FR-008**: The library MUST propagate `CancellationToken` to all handlers and behaviors.
- **FR-009**: The library MUST NOT swallow exceptions; all exceptions from handlers or behaviors
  propagate to the caller. `ParallelPublisher` wrapping multiple handler exceptions into an
  `AggregateException` is not considered swallowing — all exceptions are preserved and reach the
  caller; only the container type changes.
- **FR-010**: The library MUST support `IAsyncEnumerable<TResponse>` via `IStreamRequestHandler`
  with lazy enumeration. "Lazy enumeration" means `IStreamRequestHandler.Handle` is not invoked
  until the consumer calls `MoveNextAsync()` for the first time; no items are buffered upfront by
  the mediator.
- **FR-011**: An `IServiceCollection` extension method for handler registration MUST be provided by
  a dedicated integration package (`Mediatore.Extensions.DependencyInjection`). This package
  depends on the core library and on `Microsoft.Extensions.DependencyInjection.Abstractions`; the
  core library does not.
- **FR-012**: The library MUST offer exactly two built-in `INotificationPublisher` strategies:
  `SequentialPublisher` (default, awaits each handler in registration order) and
  `ParallelPublisher` (runs all handlers concurrently via `Task.WhenAll`, wraps exceptions in
  `AggregateException`). Fire-and-forget dispatch is not provided by the library; consumers
  implement `INotificationPublisher` directly for that use case.
- **FR-013**: The core library package (`Mediatore`) MUST have zero external runtime dependencies.
  The `Mediatore.Extensions.DependencyInjection` integration package MAY depend on
  `Microsoft.Extensions.DependencyInjection.Abstractions`; this dependency is isolated to that
  package and is not imposed on consumers of the core.
- **FR-014**: The library MUST provide a `Unit` value type (`readonly record struct` with a
  `Value` singleton) for void-returning commands. The library MUST also provide `ICommand :
  IRequest<Unit>` and `ICommandHandler<TCommand>` as syntactic conveniences;
  `ICommandHandler<TCommand>` implementations MUST be registered and resolved as
  `IRequestHandler<TCommand, Unit>`, participating in the same pipeline and exception semantics as
  any other request handler.
- **FR-015**: The library MUST expose an optional source-generator package
  (`Mediatore.SourceGenerator`) that eliminates runtime assembly scanning and produces zero heap
  allocations on the hot dispatch path. The "hot dispatch path" is defined as: zero-behavior
  dispatch in source-gen mode — handler invocation without pipeline traversal or DI resolution
  overhead. Zero heap allocations MUST be verified using BenchmarkDotNet `[MemoryDiagnoser]`;
  the `Allocated` column MUST report 0 bytes per operation for the hot-path benchmark. The source
  generator MUST emit a **compiler error** (diagnostic severity `Error`) when duplicate handler
  registrations are detected for the same request type, blocking the build before any executable
  is produced.
- **FR-016**: The mediator implementation MUST be thread-safe. A single `IMediator` instance MUST
  be safely callable from multiple concurrent async contexts without external synchronisation.
  Consumers MAY register the mediator with `Singleton` lifetime.

### Invariants *(mandatory — per Constitution Art. II.2)*

- The mediator implementation is thread-safe; concurrent calls from multiple async contexts
  produce no data races. The handler registry's immutability after build is the basis for this
  guarantee.
- The mediator's handler registry is immutable after the mediator instance is built; no handler
  may be added or removed at runtime.
- Exactly one `IRequestHandler` exists per `IRequest<TResponse>` type in a valid mediator
  configuration; any deviation is a configuration error detectable at build time.
- Pipeline behaviors are applied in the same order for every invocation of a given request type;
  order is determined at registration time and does not vary per-call.
- An `INotificationPublisher` is always present; the mediator MUST NOT operate without one (the
  default `SequentialPublisher` is used if none is explicitly configured).
- `CancellationToken` is always forwarded; the mediator MUST NOT discard or replace a
  caller-provided token.
- Exceptions are never silently discarded by the mediator core; all unhandled exceptions propagate
  to the caller.
- Exactly one `IStreamRequestHandler<TRequest, TResponse>` is permitted per closed `TRequest`
  type; duplicate registrations raise `DuplicateHandlerException` at build time (reflection path)
  or a compiler error (source-gen path); zero registrations raise `HandlerNotFoundException` at
  dispatch time.
- Dispatch is type-exact: `IMediator.Send` and `IMediator.CreateStream` resolve handlers by the
  exact runtime `System.Type` of the request argument. A handler registered for a base type is
  not matched by a derived type. Polymorphic dispatch is not supported in v1.
- Pipeline behaviors registered for `IPipelineBehavior<TRequest, TResponse>` apply to
  `ICommand`-derived requests (`ICommand : IRequest<Unit>`) — there is no special-cased or
  excluded pipeline for commands.

### Key Entities

- **`IMediator`**: The primary entry point; exposes `Send`, `Publish`, and `CreateStream`.
- **`IRequest<TResponse>`**: Marker interface for typed request messages with a single expected
  response.
- **`ICommand`**: Specialization of `IRequest<Unit>` for void-returning commands.
- **`INotification`**: Marker interface for fan-out event messages.
- **`IStreamRequest<TResponse>`**: Marker interface for streaming queries.
- **`IPipelineBehavior<TRequest, TResponse>`**: Cross-cutting behavior that wraps handler
  execution.
- **`INotificationPublisher`**: Strategy for dispatching notifications to multiple handlers.
- **`Unit`**: Value type representing the absence of a meaningful return value.
- **`HandlerNotFoundException`**: Thrown when no handler is registered for a dispatched request.
- **`DuplicateHandlerException`**: Thrown at build time when multiple handlers exist for one
  request type.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can send a request and receive a typed response in fewer than 5 lines of
  application code after registration, with no boilerplate beyond the handler class.
  "Boilerplate" means: registration attributes on the handler, per-handler DI registration calls,
  or generated partial classes. A handler implementing only `IRequestHandler<TRequest, TResponse>`
  (or `ICommandHandler<TCommand>`) with its `Handle` method is sufficient; no other artefacts are
  required on the handler.
- **SC-002**: Adding or removing a pipeline behavior requires no changes to handler code —
  verified by confirming that registering a behavior in `MediatorOptions` requires modifications
  only to the composition root, not to any existing handler class. The integration test suite
  (T045, T078) demonstrates this property in code.
- **SC-003**: Dispatch overhead does not noticeably degrade a hot-loop benchmark processing 1
  million sequential in-process calls compared to a direct method invocation baseline (target:
  under 1 microsecond additional latency per call with no behaviors and source-gen mode enabled).
  The "< 1 µs" target is the **median** additional latency measured by BenchmarkDotNet over 1 000 000
  iterations. A "no-op handler" is one that returns `Task.FromResult(Unit.Value)` immediately.
  The baseline is direct invocation of an equivalent `Func<Task<Unit>>` no-op delegate.
- **SC-004**: A misconfigured mediator (missing handler, duplicate handler) produces a clear,
  actionable error message at startup, not at the first production dispatch — verified by
  integration tests that assert the exception type and message content at build time.
- **SC-005**: The core library package can be added to a project that has no other runtime
  dependencies without introducing any transitive dependency conflicts.
- **SC-006**: A developer new to the library can configure, register handlers, and successfully
  dispatch their first request by following only the library's quickstart documentation, without
  reading source code. Verified by T079: following each step of `quickstart.md` verbatim in a
  fresh `dotnet new` project and confirming all dispatch paths produce working, compilable output.

---

## Assumptions

- The sole target framework moniker (TFM) is `net10.0`. No multi-targeting or `netstandard2.x` compatibility layer is provided in v1.
- Consumers are expected to use `Microsoft.Extensions.DependencyInjection` for handler
  registration via the `Mediatore.Extensions.DependencyInjection` package; a manual (DI-free)
  registration path is a future concern.
- The `ValidationBehavior` built-in does not bundle a validation library; it defines an adapter
  interface that consumers wire to their preferred validator (e.g., FluentValidation).
- Handler lifetime defaults to `Scoped`, matching the typical ASP.NET Core request scope.
- The source generator is an optional add-on; the reflection-based registration path is the
  baseline and must be fully functional on its own.
- Benchmark baselines are measured on a single-core, fixed-frequency environment to ensure
  reproducibility across CI runs. The `< 5 ms` and `< 100 ms` startup registration targets
  (source-gen and reflection respectively) are wall-clock elapsed time on this same environment.
- BCL (Base Class Library) types — including `System.Collections.Frozen.FrozenDictionary<,>` —
  are not considered external runtime dependencies for purposes of FR-013. They are part of the
  .NET 10 runtime and require no package reference.
- The \u2264 1 heap allocation per `Send` call in reflection mode (the `Task<TResponse>` return) is
  an aspirational performance target in v1, not a hard regression gate.
- BenchmarkDotNet runs are executed manually, not as CI merge gates, in v1; results are reviewed
  manually against the targets in SC-003.
- Handler types discovered by `RegisterServicesFromAssembly` must be concrete, non-abstract,
  non-open-generic classes; abstract base handler classes and open-generic handler definitions
  are silently skipped during assembly scanning.
- `DEPENDENCIES.md` content follows Constitution Art. IX.1: one entry per dependency, recording
  the package name, version range, and justification for why it cannot be implemented inline or
  eliminated.
- NuGet package metadata (package IDs, MIT licence, description, tags) are defined in the
  respective `.csproj` files per plan.md; this spec does not govern packaging details.

---

## Out of Scope *(mandatory — per Constitution Art. II.2)*

- Fire-and-forget notification dispatch with exception suppression — consumers implement
  `INotificationPublisher` directly for this use case.
- Distributed or out-of-process messaging (message queues, brokers, transports).
- Persistent or durable message processing (no outbox pattern, no retry infrastructure).
- Actor model or concurrent mailbox semantics.
- Automatic handler discovery without any opt-in registration call — consumers MUST explicitly
  call `RegisterServicesFromAssembly(...)` (reflection path) or `RegisterGeneratedHandlers()`
  (source-gen path) to register handlers; fully implicit scanning with no API call is not
  provided.
- Pipeline behaviors for `INotificationHandler` chains (notification pipeline is a future spec).
- Request deduplication or idempotency key management.
- Distributed tracing integration (OpenTelemetry adapters are a consumer concern or future spec).
- Generic covariant or contravariant handler resolution.
- DI container integrations beyond `Microsoft.Extensions.DependencyInjection` (e.g., Autofac,
  Lamar) — these ship as separate packages and are not part of this spec.
- Any UI, web, or transport layer concerns.
- Performance requirements for streaming dispatch (`CreateStream`) overhead — addressed in a
  future spec.
- Performance requirements for notification dispatch (`Publish`) overhead with N handlers —
  addressed in a future spec.
- AOT (Ahead-of-Time) compilation compatibility and ILLink/trim safety — no formal guarantee or
  test coverage is committed to in v1. The source-gen path may be AOT-friendly by design, but
  this is not spec-required.
