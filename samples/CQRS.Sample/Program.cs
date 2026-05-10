// CQRS Sample — demonstrates all four Mediatore dispatch paths.
using Mediatore;
using Mediatore.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

using var sp = new ServiceCollection()
    .AddMediator(o => o.RegisterServicesFromAssemblyContaining<GetProductHandler>())
    .BuildServiceProvider();

using var scope = sp.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

Console.WriteLine("=== 1. Request/Response: GetProductQuery ===");
var product = await mediator.Send(new GetProductQuery(42));
Console.WriteLine($"  Result: {product}");

Console.WriteLine("\n=== 2. Command: CreateOrderCommand ===");
await mediator.Send(new CreateOrderCommand(42));

Console.WriteLine("\n=== 3. Streaming: NumberStreamRequest ===");
await foreach (var number in mediator.CreateStream(new NumberStreamRequest(3)))
    Console.WriteLine($"  Received: {number}");

Console.WriteLine("\nDone.");

