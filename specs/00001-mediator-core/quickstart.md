# Quickstart: Mediatore

**Audience**: Developer new to the library (Success Criterion SC-006)
**Date**: 2026-05-09

---

## 1. Install Packages

```xml
<!-- Core library (zero dependencies) -->
<PackageReference Include="Mediatore" Version="*" />

<!-- DI integration (Microsoft.Extensions.DependencyInjection) -->
<PackageReference Include="Mediatore.Extensions.DependencyInjection" Version="*" />

<!-- Optional: compile-time source generator (zero runtime weight) -->
<PackageReference Include="Mediatore.SourceGenerator" Version="*"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

---

## 2. Define a Request and Handler

```csharp
using Mediatore;

// The request (a query)
public record GetProductQuery(int ProductId) : IRequest<Product>;

// The response
public record Product(int Id, string Name, decimal Price);

// The handler — one class, one request type (Constitution Art. IV.6)
public sealed class GetProductQueryHandler : IRequestHandler<GetProductQuery, Product>
{
    public Task<Product> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        // Replace with real data access
        var product = new Product(request.ProductId, "Widget", 9.99m);
        return Task.FromResult(product);
    }
}
```

---

## 3. Define a Command

```csharp
using Mediatore;

// A void command (no return value)
public record CreateOrderCommand(int ProductId, int Quantity) : ICommand;

public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand>
{
    public Task Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // Replace with real business logic
        Console.WriteLine($"Order placed: {command.Quantity}x product {command.ProductId}");
        return Task.CompletedTask;
    }
}
```

---

## 4. Register with Dependency Injection

```csharp
using Mediatore.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediator(options =>
{
    // Register handlers from the assembly containing GetProductQueryHandler
    options.RegisterServicesFromAssemblyContaining<GetProductQueryHandler>();

    // Optional: switch to parallel notification dispatch
    // options.NotificationPublisher = new ParallelPublisher();
});
```

---

## 5. Dispatch a Request

```csharp
// In a controller, minimal API handler, or service:
app.MapGet("/products/{id}", async (int id, IMediator mediator, CancellationToken ct) =>
{
    var product = await mediator.Send(new GetProductQuery(id), ct);
    return Results.Ok(product);
});

app.MapPost("/orders", async (CreateOrderCommand cmd, IMediator mediator, CancellationToken ct) =>
{
    await mediator.Send(cmd, ct);
    return Results.NoContent();
});
```

---

## 6. Add a Pipeline Behavior (Cross-Cutting Concern)

```csharp
using Mediatore;

// Logs every request with its duration
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

Register the behavior (open-generic registration):

```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

---

## 7. Publish a Notification

```csharp
using Mediatore;

// Notification
public record OrderPlaced(int OrderId) : INotification;

// Handler 1
public sealed class SendConfirmationEmailHandler : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Email sent for order {notification.OrderId}");
        return Task.CompletedTask;
    }
}

// Handler 2
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

## 8. Stream a Response

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
await foreach (var product in mediator.CreateStream(new GetProductCatalogueStream(), ct))
{
    Console.WriteLine(product.Name);
}
```

---

## Common Errors

| Error | Cause | Fix |
|---|---|---|
| `HandlerNotFoundException` at runtime | No handler registered for the request type | Ensure `RegisterServicesFromAssemblyContaining<T>()` covers the handler's assembly |
| `DuplicateHandlerException` at startup | Two classes implement `IRequestHandler<SameRequest, ...>` | Remove or rename one; each request type must have exactly one handler |
| `MED0001` compiler error (source-gen) | Same as above, detected at compile time | Remove the duplicate handler class |
| Pipeline not executing | Behavior not registered in DI | Add `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MyBehavior<,>))` |
