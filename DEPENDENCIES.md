# Dependencies

> Required by Constitution Art. IX.1. Each entry records the package name, version range,
> hosting project, and justification for why the dependency cannot be implemented inline or
> eliminated.

---

## `Mediatore.Extensions.DependencyInjection`

| Field | Value |
|---|---|
| Package | `Microsoft.Extensions.DependencyInjection.Abstractions` |
| Version | `>= 10.0.0` (exact: `10.0.0`) |
| PackageReference | `<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />` |
| Justification | `IServiceCollection`, `IServiceProvider`, and `ServiceLifetime` are defined in this package. Inlining these types would duplicate BCL-adjacent abstractions that consumers already depend on, and would break compatibility with the standard .NET DI ecosystem. The package itself has zero transitive runtime dependencies. |

---

## Core library (`Mediatore`)

Zero external runtime dependencies — by design (FR-013).

`FrozenDictionary<TKey, TValue>` is used for the handler registry; it is part of the
`System.Collections.Frozen` namespace in the .NET 10 BCL and requires no package reference.
