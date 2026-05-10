using Mediatore;

// ---------------------------------------------------------------------------
// Domain types
// ---------------------------------------------------------------------------

record Product(int Id, string Name);

record GetProductQuery(int Id) : IRequest<Product>;

record CreateOrderCommand(int ProductId) : ICommand;

record OrderPlaced(int ProductId) : INotification;

record NumberStreamRequest(int Count) : IStreamRequest<int>;

// ---------------------------------------------------------------------------
// Handlers
// ---------------------------------------------------------------------------

sealed class GetProductHandler : IRequestHandler<GetProductQuery, Product>
{
    public Task<Product> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [GetProductHandler] Fetching product {request.Id}");
        return Task.FromResult(new Product(request.Id, $"Product-{request.Id}"));
    }
}

sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand>
{
    private readonly IMediator _mediator;
    public CreateOrderHandler(IMediator mediator) => _mediator = mediator;

    public async Task Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [CreateOrderHandler] Creating order for product {command.ProductId}");
        await _mediator.Publish(new OrderPlaced(command.ProductId), cancellationToken);
    }
}

sealed class EmailNotificationHandler : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [EmailNotificationHandler] Sending email for product {notification.ProductId}");
        return Task.CompletedTask;
    }
}

sealed class AuditNotificationHandler : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [AuditNotificationHandler] Auditing order for product {notification.ProductId}");
        return Task.CompletedTask;
    }
}

sealed class NumberStreamHandler : IStreamRequestHandler<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> Handle(
        NumberStreamRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 1; i <= request.Count; i++)
        {
            await Task.Delay(10, cancellationToken);
            yield return i;
        }
    }
}
