using Mediatore;

namespace Mediatore.ContractTests.Contracts;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record PipelineRequest(int Value) : IRequest<int>;
file sealed class PipelineRequestHandler : IRequestHandler<PipelineRequest, int>
{
    public Task<int> Handle(PipelineRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value * 2);
}

file sealed class MultiplyBehavior(int factor) : IPipelineBehavior<PipelineRequest, int>
{
    public async Task<int> Handle(
        PipelineRequest request,
        RequestHandlerDelegate<int> next,
        CancellationToken cancellationToken)
    {
        var result = await next();
        return result * factor;
    }
}

// ---------------------------------------------------------------------------
// REQ-03: Behaviors execute in registration order; order is consistent.
// ---------------------------------------------------------------------------

public sealed class PipelineOrderTests
{
    [Fact]
    public async Task Behaviors_ExecuteInRegistrationOrder()
    {
        // B1 multiplies by 3, B2 multiplies by 5, handler multiplies input by 2.
        // Execution: handler(5) = 10, B2: 10*5=50, B1: 50*3=150
        using var sp = new ServiceCollection()
            .AddSingleton<IRequestHandler<PipelineRequest, int>, PipelineRequestHandler>()
            .AddSingleton<IPipelineBehavior<PipelineRequest, int>>(new MultiplyBehavior(3))
            .AddSingleton<IPipelineBehavior<PipelineRequest, int>>(new MultiplyBehavior(5))
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<PipelineRequestHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Send(new PipelineRequest(5), TestContext.Current.CancellationToken);

        result.Should().Be(150); // 5*2=10, *5=50, *3=150
    }

    [Fact]
    public async Task Behaviors_OrderIsConsistentAcrossMultipleInvocations()
    {
        using var sp = new ServiceCollection()
            .AddSingleton<IRequestHandler<PipelineRequest, int>, PipelineRequestHandler>()
            .AddSingleton<IPipelineBehavior<PipelineRequest, int>>(new MultiplyBehavior(3))
            .AddSingleton<IPipelineBehavior<PipelineRequest, int>>(new MultiplyBehavior(5))
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<PipelineRequestHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        for (int i = 0; i < 5; i++)
        {
            var result = await mediator.Send(new PipelineRequest(1), TestContext.Current.CancellationToken);
            result.Should().Be(30); // 1*2=2, *5=10, *3=30
        }
    }
}
