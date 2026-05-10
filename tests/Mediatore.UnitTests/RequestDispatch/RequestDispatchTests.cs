using Mediatore;

namespace Mediatore.UnitTests.RequestDispatch;

// ---------------------------------------------------------------------------
// Minimal stubs for unit testing (no DI, no real mediator)
// ---------------------------------------------------------------------------

file sealed record GetProductQuery(int Id) : IRequest<Product>;
file sealed record Product(int Id, string Name);

file sealed class GetProductQueryHandler : IRequestHandler<GetProductQuery, Product>
{
    public int CallCount { get; private set; }
    public Task<Product> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(new Product(request.Id, $"Product-{request.Id}"));
    }
}

file sealed record CreateProductCommand(string Name) : ICommand;

file sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand>
{
    public int CallCount { get; private set; }
    public Task Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.CompletedTask;
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public sealed class RequestDispatchTests
{
    [Fact]
    public async Task RequestHandler_Handle_InvokedExactlyOnce()
    {
        var handler = new GetProductQueryHandler();
        var result = await handler.Handle(
            new GetProductQuery(42), TestContext.Current.CancellationToken);

        handler.CallCount.Should().Be(1);
        result.Id.Should().Be(42);
    }

    [Fact]
    public async Task CommandHandler_Handle_InvokedExactlyOnce()
    {
        var handler = new CreateProductCommandHandler();
        await handler.Handle(
            new CreateProductCommand("Widget"), TestContext.Current.CancellationToken);

        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Unit_Value_IsSingleton()
    {
        await Task.CompletedTask;
        var a = Unit.Value;
        var b = Unit.Value;
        a.Should().Be(b);
    }
}

