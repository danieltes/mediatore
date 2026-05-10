using Mediatore;

namespace Mediatore.UnitTests.PipelineBehavior;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record ShortCircuitRequest(bool ShouldShortCircuit) : IRequest<string>;
file sealed class ShortCircuitHandler : IRequestHandler<ShortCircuitRequest, string>
{
    public static int CallCount;
    public Task<string> Handle(ShortCircuitRequest request, CancellationToken cancellationToken)
    {
        System.Threading.Interlocked.Increment(ref CallCount);
        return Task.FromResult("handler-response");
    }
}

file sealed class ShortCircuitBehavior : IPipelineBehavior<ShortCircuitRequest, string>
{
    public Task<string> Handle(
        ShortCircuitRequest request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        if (request.ShouldShortCircuit)
            return Task.FromResult("short-circuit-response");

        return next();
    }
}

// ---------------------------------------------------------------------------
// Tests for short-circuit behavior
// ---------------------------------------------------------------------------

public sealed class PipelineShortCircuitTests
{
    [Fact]
    public async Task Behavior_WhenShortCircuiting_HandlerNotInvoked()
    {
        ShortCircuitHandler.CallCount = 0;

        using var sp = new ServiceCollection()
            .AddSingleton<IRequestHandler<ShortCircuitRequest, string>, ShortCircuitHandler>()
            .AddSingleton<IPipelineBehavior<ShortCircuitRequest, string>, ShortCircuitBehavior>()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<ShortCircuitHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Send(
            new ShortCircuitRequest(ShouldShortCircuit: true),
            TestContext.Current.CancellationToken);

        result.Should().Be("short-circuit-response");
        ShortCircuitHandler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Behavior_WhenNotShortCircuiting_HandlerInvoked()
    {
        ShortCircuitHandler.CallCount = 0;

        using var sp = new ServiceCollection()
            .AddSingleton<IRequestHandler<ShortCircuitRequest, string>, ShortCircuitHandler>()
            .AddSingleton<IPipelineBehavior<ShortCircuitRequest, string>, ShortCircuitBehavior>()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<ShortCircuitHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Send(
            new ShortCircuitRequest(ShouldShortCircuit: false),
            TestContext.Current.CancellationToken);

        result.Should().Be("handler-response");
        ShortCircuitHandler.CallCount.Should().Be(1);
    }
}
