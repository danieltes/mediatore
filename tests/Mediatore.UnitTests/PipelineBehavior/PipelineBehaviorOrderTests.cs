using Mediatore;

namespace Mediatore.UnitTests.PipelineBehavior;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record OrderRequest : IRequest<List<string>>;
file sealed class OrderRequestHandler : IRequestHandler<OrderRequest, List<string>>
{
    public Task<List<string>> Handle(OrderRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new List<string> { "handler" });
}

file sealed class TracingBehavior(string name) : IPipelineBehavior<OrderRequest, List<string>>
{
    public async Task<List<string>> Handle(
        OrderRequest request,
        RequestHandlerDelegate<List<string>> next,
        CancellationToken cancellationToken)
    {
        var result = await next();
        result.Insert(0, $"{name}-enter");
        result.Add($"{name}-exit");
        return result;
    }
}

// ---------------------------------------------------------------------------
// REQ-03: Behaviors execute in B1→B2→B3→Handler→B3-exit→B2-exit→B1-exit order
// ---------------------------------------------------------------------------

public sealed class PipelineBehaviorOrderTests
{
    [Fact]
    public async Task ThreeBehaviors_ExecuteInCorrectOrder()
    {
        using var sp = new ServiceCollection()
            .AddSingleton<IRequestHandler<OrderRequest, List<string>>, OrderRequestHandler>()
            .AddSingleton<IPipelineBehavior<OrderRequest, List<string>>>(new TracingBehavior("B1"))
            .AddSingleton<IPipelineBehavior<OrderRequest, List<string>>>(new TracingBehavior("B2"))
            .AddSingleton<IPipelineBehavior<OrderRequest, List<string>>>(new TracingBehavior("B3"))
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<OrderRequestHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Send(new OrderRequest(), TestContext.Current.CancellationToken);

        // Expected: B1-enter, B2-enter, B3-enter, handler, B3-exit, B2-exit, B1-exit
        Assert.Equal(
            new[] { "B1-enter", "B2-enter", "B3-enter", "handler", "B3-exit", "B2-exit", "B1-exit" },
            result);
    }

    [Fact]
    public async Task TwoBehaviors_OrderIsConsistentAcrossMultipleCalls()
    {
        using var sp = new ServiceCollection()
            .AddSingleton<IRequestHandler<OrderRequest, List<string>>, OrderRequestHandler>()
            .AddSingleton<IPipelineBehavior<OrderRequest, List<string>>>(new TracingBehavior("B1"))
            .AddSingleton<IPipelineBehavior<OrderRequest, List<string>>>(new TracingBehavior("B2"))
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<OrderRequestHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        var r1 = await mediator.Send(new OrderRequest(), TestContext.Current.CancellationToken);
        var r2 = await mediator.Send(new OrderRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "B1-enter", "B2-enter", "handler", "B2-exit", "B1-exit" }, r1);
        Assert.Equal(new[] { "B1-enter", "B2-enter", "handler", "B2-exit", "B1-exit" }, r2);
    }
}
