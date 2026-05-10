<!--
SYNC IMPACT REPORT
==================
Version change: (none) → 1.0.0 (initial ratification)
Modified principles: n/a — initial constitution creation
Added sections: Articles I–X, Quick-Reference Checklist
Removed sections: n/a
Templates requiring updates:
  ✅ .specify/memory/constitution.md (this file)
  ✅ .specify/templates/plan-template.md — Constitution Check gates updated
  ⚠ .specify/templates/spec-template.md — alignment verified; no structural change needed
  ⚠ .specify/templates/tasks-template.md — alignment verified; no structural change needed
Deferred items: none
-->

# Mediatore Constitution

## Core Principles

### I. Purpose & Scope

This library exists to provide a correct, minimal, and composable implementation of the Mediator
behavioral pattern. The library's public API is the product; implementation details are private
concerns. The library MUST be usable without modification for the majority of real-world mediator
use cases. Extension points exist where variation is genuinely needed, not speculatively.

### II. Spec-First Law (NON-NEGOTIABLE)

No production code may be written without a corresponding specification that preceded it.

A **specification** is a written document (not a test file) that defines: the behavior being
modeled; preconditions, postconditions, and invariants; edge cases and error states; and what the
spec explicitly does *not* cover.

Specs live in `specs/` at the project root. Each spec is a Markdown file named after the
capability it describes (e.g., `specs/request-handling.md`).

A spec is **ratified** when it has been reviewed and no open objections remain. Only ratified specs
may be implemented.

Changing a ratified spec requires a formal revision. Code changes that contradict a ratified spec
without a spec revision are **invalid and MUST be reverted**.

The spec, not the code, is the source of truth. When code and spec conflict, **the spec wins**.

### III. The Mediator Contract

The core abstraction is the **Mediator** — an object that brokers communication between
**Colleagues** without them referencing each other directly.

The library MUST define and enforce these roles:

| Role | Responsibility |
|---|---|
| `Mediator` | Receives messages; routes and coordinates responses |
| `Request` | A typed message with a single expected response type |
| `Notification` | A typed message with no expected response (fan-out) |
| `Handler<TRequest>` | Produces the response to a specific `Request` type |
| `NotificationHandler<TNotification>` | Reacts to a specific `Notification` type |
| `Pipeline` | Ordered middleware applied around handler execution |

Every message type MUST be a distinct, closed type. Untyped or `object`-based dispatch is not
permitted in the public API.

A `Request` MUST have exactly one registered `Handler`. Multiple registrations for the same
request type is a configuration error, surfaced at startup, not at dispatch time.

A `Notification` MAY have zero or more registered `NotificationHandler`s. Zero handlers is not an
error.

### IV. Design Principles

**IV.1 Explicit over implicit.** The library MUST never rely on magic (reflection-heavy
auto-discovery, ambient global state, thread-local tricks) for core behavior. Any auto-discovery
feature is opt-in and behind a clearly named API.

**IV.2 Composition over inheritance.** Behavior extension is done through middleware pipelines, not
subclassing core types.

**IV.3 Fail loudly at configuration time.** Misconfiguration (missing handlers, duplicate
registrations, circular dependencies) MUST be detected and raised when the mediator is built, not
when a message is dispatched.

**IV.4 Zero hidden coupling.** Handlers MUST NOT reference the `Mediator` directly unless
implementing a deliberate chaining pattern that is explicitly specified.

**IV.5 No ambient state.** The mediator instance is an explicit dependency. No static/singleton
access patterns in the library itself.

**IV.6 Single responsibility per handler.** One handler class handles one request type. A handler
MUST NOT register itself for multiple request types.

### V. Pipeline & Middleware

Pipelines are ordered sequences of behaviors that wrap handler execution. They are the only
approved extension point for cross-cutting concerns (logging, validation, authorization, caching).

Middleware MUST be explicitly registered and ordered by the consumer. The library MUST NOT silently
inject any middleware of its own.

A pipeline behavior MUST:
- Accept the request and a `next` delegate
- Call `next` exactly once in the happy path
- Short-circuit (not call `next`) ONLY when explicitly designed to do so and documented in its spec

Pipeline behaviors are typed. A behavior registered for `Request<TResponse>` does NOT automatically
apply to `Notification`.

## Quality & Stability Requirements

### VI. Error Handling

The library MUST NOT swallow exceptions. All exceptions propagate to the caller unless a pipeline
behavior explicitly handles them.

Library-specific exceptions use a dedicated exception hierarchy. They MUST carry enough context to
diagnose the problem without requiring a debugger.

Notification dispatch failure policy (halt-on-first-error vs. continue-and-aggregate) is
configurable and MUST be specified before it is implemented.

The library MUST NOT log. Observability is a consumer concern, addressed through pipeline
behaviors.

### VII. Testing Requirements

Every ratified spec produces at least one corresponding test file. The test file name mirrors the
spec name.

Tests MUST be written against the **public API only**. Tests that depend on internal
implementation types are disallowed.

Test categories:

| Category | Purpose |
|---|---|
| **Unit** | Single handler/behavior in isolation; no real mediator instance |
| **Integration** | Full mediator with a real registration and dispatch cycle |
| **Contract** | Verifies spec invariants hold; MUST remain green across refactors |

Contract tests are NEVER deleted. They MAY be updated only when the underlying spec is formally
revised.

Code coverage is a floor, not a goal. 100% coverage of dead or trivial code is worthless. Every
branch that represents a spec-defined behavior MUST be covered.

### VIII. API Stability & Versioning

The library uses **Semantic Versioning** (MAJOR.MINOR.PATCH) strictly:
- **PATCH** — Bug fixes with no API change
- **MINOR** — Additive, backward-compatible API changes
- **MAJOR** — Breaking changes to any public type, method, or behavior contract

Any type, method, or behavior that is part of the public API is covered by the stability guarantee
the moment it ships in a non-prerelease version.

Deprecation MUST precede removal by at least one MINOR version. Deprecated items are annotated
and documented with a migration path.

Experimental APIs are explicitly marked (e.g., `@experimental`, `[Experimental]`) and carry no
stability guarantee. They may change in any release.

### IX. Dependency Policy

The library's production dependency count MUST be justified in writing. Each dependency added to
the library requires an entry in `DEPENDENCIES.md` explaining why it cannot be implemented inline
or eliminated.

The library MUST NOT transitively impose framework dependencies on consumers (e.g., it MUST NOT
require a specific DI container, web framework, or logging library).

DI container integrations are separate packages. They depend on the library; the library does NOT
depend on them.

## Development Workflow

### Quick-Reference Checklist (PR Gate)

Before any PR may be merged:

- [ ] A ratified spec exists for the behavior being added or changed
- [ ] Spec and implementation are consistent
- [ ] Contract tests pass
- [ ] No new public API ships without documentation
- [ ] No new dependency added without a `DEPENDENCIES.md` entry
- [ ] If a breaking change, MAJOR version bump is noted and migration guide drafted

## Governance

**X.1 Spec authorship.** Any contributor may draft a spec. A spec becomes a candidate for
ratification through a pull request.

**X.2 Ratification quorum.** A spec is ratified when at least two project maintainers approve it
with no unresolved blocking comments.

**X.3 Implementation ownership.** The person who implements a ratified spec is responsible for
the correctness of that implementation against the spec, not the spec author.

**X.4 Amendment process.** Changes to this constitution require a formal proposal, a ratification
quorum of all active maintainers, and a changelog entry.

**X.5 Conflict resolution.** When implementation choices are contested and no spec exists to
resolve the dispute, the dispute drives the creation of a spec, not a vote on the
implementation.

This constitution supersedes all other project practices. All PRs and reviews MUST verify
compliance with the principles above before merging.

**Version**: 1.0.0 | **Ratified**: 2026-05-09 | **Last Amended**: 2026-05-09
