# Mediatore

A lightweight, zero-dependency, in-process mediator library for .NET 10. Implements the Mediator pattern with support for request/response, commands, notifications (fan-out), and async streaming — with an ordered pipeline behavior mechanism for cross-cutting concerns.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Target Framework](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)

## Packages

| Package | Description |
|---|---|
| `Mediatore` | Core library — zero external runtime dependencies |
| `Mediatore.Extensions.DependencyInjection` | DI integration via `Microsoft.Extensions.DependencyInjection` |
| `Mediatore.SourceGenerator` | Optional Roslyn source generator — eliminates reflection and produces zero heap allocations on the hot dispatch path |

## Installation

```shell
# Core library (zero dependencies)
dotnet add package Mediatore

# DI integration
dotnet add package Mediatore.Extensions.DependencyInjection

# Optional: compile-time source generator (zero runtime weight)
dotnet add package Mediatore.SourceGenerator
```

## Configuration (Dependency Injection)

Call `AddMediator` on your `IServiceCollection` and point it at the assemblies containing your handlers:

```csharp
using Mediatore.Extensions.DependencyInjection;

builder.Services.AddMediator(options =>
{
    // Scan the assembly that contains MyHandler (and all other handlers in that assembly)
    options.RegisterServicesFromAssemblyContaining<MyHandler>();

    // Scan multiple assemblies if handlers are spread across projects
    // options.RegisterServicesFromAssembly(typeof(OtherHandler).Assembly);
});
```

### Options

| Option | Type | Default | Description |
|---|---|---|---|
| `Lifetime` | `ServiceLifetime` | `Scoped` | DI lifetime applied to all registered handlers |
| `NotificationPublisher` | `INotificationPublisher` | `SequentialPublisher` | Strategy for dispatching notifications to multiple handlers |

#### Handler lifetime

```csharp
builder.Services.AddMediator(options =>
{
    options.RegisterServicesFromAssemblyContaining<MyHandler>();
    options.Lifetime = ServiceLifetime.Transient;
});
```

#### Notification publisher

Two built-in publishers are provided:

- **`SequentialPublisher`** *(default)* — awaits each handler in registration order; propagates the first exception immediately.
- **`ParallelPublisher`** — runs all handlers concurrently via `Task.WhenAll`; aggregates all exceptions into an `AggregateException`.

```csharp
using Mediatore.Publishing;

builder.Services.AddMediator(options =>
{
    options.RegisterServicesFromAssemblyContaining<MyHandler>();
    options.NotificationPublisher = new ParallelPublisher();
});
```

`IMediator` is registered as `Scoped`; `INotificationPublisher` and the internal handler registry are registered as `Singleton`.

---

## Examples

### 1. Request / Response

Define a request implementing `IRequest<TResponse>` and a single handler:

```csharp
using Mediatore;

public record GetProductQuery(int ProductId) : IRequest<Product>;

public record Product(int Id, string Name, decimal Price);

public sealed class GetProductQueryHandler : IRequestHandler<GetProductQuery, Product>
{
    public Task<Product> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = new Product(request.ProductId, "Widget", 9.99m);
        return Task.FromResult(product);
    }
}
```

Dispatch:

```csharp
var product = await mediator.Send(new GetProductQuery(42), cancellationToken);
```

In ASP.NET Core minimal APIs:

```csharp
app.MapGet("/products/{id}", async (int id, IMediator mediator, CancellationToken ct) =>
{
    var product = await mediator.Send(new GetProductQuery(id), ct);
    return Results.Ok(product);
});
```

---

### 2. Command (void return)

`ICommand` is a convenience marker for requests that return no value (`Unit`):

```csharp
using Mediatore;

public record CreateOrderCommand(int ProductId, int Quantity) : ICommand;

public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand>
{
    public Task Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Order placed: {command.Quantity}x product {command.ProductId}");
        return Task.CompletedTask;
    }
}
```

Dispatch:

```csharp
await mediator.Send(new CreateOrderCommand(42, 3), cancellationToken);
```

---

### 3. Notification (fan-out)

Notifications are dispatched to **all** registered handlers. Zero handlers is not an error.

```csharp
using Mediatore;

public record OrderPlaced(int OrderId) : INotification;

public sealed class SendConfirmationEmailHandler : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Email sent for order {notification.OrderId}");
        return Task.CompletedTask;
    }
}

public sealed class UpdateInventoryHandler : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Inventory updated for order {notification.OrderId}");
        return Task.CompletedTask;
    }
}
```

Dispatch:

```csharp
await mediator.Publish(new OrderPlaced(orderId), cancellationToken);
// Both handlers are invoked sequentially (default strategy)
```

---

### 4. Streaming

Define a stream request implementing `IStreamRequest<TResponse>`:

```csharp
using Mediatore;

public record GetProductCatalogueStream : IStreamRequest<Product>;

public sealed class GetProductCatalogueStreamHandler
    : IStreamRequestHandler<GetProductCatalogueStream, Product>
{
    public async IAsyncEnumerable<Product> Handle(
        GetProductCatalogueStream request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var product in GetAllProducts())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return product;
        }
    }
}
```

Consume:

```csharp
await foreach (var product in mediator.CreateStream(new GetProductCatalogueStream(), cancellationToken))
{
    Console.WriteLine(product.Name);
}
```

---

### 5. Pipeline Behaviors

Pipeline behaviors wrap handler execution for cross-cutting concerns such as logging, validation, and authorization. Behaviors are applied in registration order.

```csharp
using Mediatore;

public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}
```

Register using open-generic DI registration (applies to all request types):

```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

Execution order for three behaviors B1, B2, B3 registered in that order:

```
B1 entry → B2 entry → B3 entry → Handler → B3 exit → B2 exit → B1 exit
```

---

### 6. Custom Notification Publisher

Implement `INotificationPublisher` to define a custom dispatch strategy (e.g., fire-and-forget):

```csharp
using Mediatore;

public sealed class FireAndForgetPublisher : INotificationPublisher
{
    public Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlers,
        INotification notification,
        CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
            _ = handler.HandlerCallback(notification, CancellationToken.None);

        return Task.CompletedTask;
    }
}
```

Register it via `MediatorOptions`:

```csharp
builder.Services.AddMediator(options =>
{
    options.RegisterServicesFromAssemblyContaining<MyHandler>();
    options.NotificationPublisher = new FireAndForgetPublisher();
});
```

---

## Error Handling

| Exception | When thrown |
|---|---|
| `HandlerNotFoundException` | `Send` or `CreateStream` is called for a type with no registered handler (dispatch time) |
| `DuplicateHandlerException` | Two or more handlers are registered for the same request type (at `AddMediator` build time) |
