using Mediatore;

namespace Mediatore.IntegrationTests.PipelineScenarios;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record PipelineIntegrationRequest(int Value) : IRequest<int>;
file sealed class PipelineIntegrationHandler : IRequestHandler<PipelineIntegrationRequest, int>
{
    public Task<int> Handle(PipelineIntegrationRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value + 1);
}

file sealed class LoggingBehavior : IPipelineBehavior<PipelineIntegrationRequest, int>
{
    public static readonly List<string> Log = [];

    public async Task<int> Handle(
        PipelineIntegrationRequest request,
        RequestHandlerDelegate<int> next,
        CancellationToken cancellationToken)
    {
        Log.Add($"before-{request.Value}");
        var result = await next();
        Log.Add($"after-{result}");
        return result;
    }
}

file sealed class ValidationBehavior : IPipelineBehavior<PipelineIntegrationRequest, int>
{
    public Task<int> Handle(
        PipelineIntegrationRequest request,
        RequestHandlerDelegate<int> next,
        CancellationToken cancellationToken)
    {
        if (request.Value < 0)
            throw new ArgumentException("Value must be non-negative");

        return next();
    }
}

file sealed class AfterHandlerExceptionBehavior : IPipelineBehavior<AfterExceptionRequest, int>
{
    public static bool AfterCodeRan;
    public async Task<int> Handle(
        AfterExceptionRequest request,
        RequestHandlerDelegate<int> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        finally
        {
            AfterCodeRan = true;
        }
    }
}

file sealed record AfterExceptionRequest(int Value) : IRequest<int>;

file sealed class ThrowingHandler : IRequestHandler<AfterExceptionRequest, int>
{
    public Task<int> Handle(AfterExceptionRequest request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Handler threw");
}

// ---------------------------------------------------------------------------
// Integration tests: pipeline scenarios
// ---------------------------------------------------------------------------

public sealed class PipelineScenarioTests
{
    [Fact]
    public async Task LoggingBehavior_RecordsBeforeAndAfterExecution()
    {
        LoggingBehavior.Log.Clear();

        using var sp = new ServiceCollection()
            .AddSingleton<IRequestHandler<PipelineIntegrationRequest, int>, PipelineIntegrationHandler>()
            .AddSingleton<IPipelineBehavior<PipelineIntegrationRequest, int>, LoggingBehavior>()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<PipelineIntegrationHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Send(new PipelineIntegrationRequest(5), TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "before-5", "after-6" }, LoggingBehavior.Log);
    }

    [Fact]
    public async Task ValidationBehavior_NegativeValue_ShortCircuitsWithException()
    {
        using var sp = new ServiceCollection()
            .AddSingleton<IRequestHandler<PipelineIntegrationRequest, int>, PipelineIntegrationHandler>()
            .AddSingleton<IPipelineBehavior<PipelineIntegrationRequest, int>, ValidationBehavior>()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<PipelineIntegrationHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ArgumentException>(
            () => mediator.Send(new PipelineIntegrationRequest(-1), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BehaviorAfterCode_RunsEvenWhenHandlerThrows()
    {
        AfterHandlerExceptionBehavior.AfterCodeRan = false;

        using var sp = new ServiceCollection()
            .AddSingleton<IRequestHandler<PipelineIntegrationRequest, int>, PipelineIntegrationHandler>()
            .AddSingleton<IRequestHandler<AfterExceptionRequest, int>, ThrowingHandler>()
            .AddSingleton<IPipelineBehavior<AfterExceptionRequest, int>, AfterHandlerExceptionBehavior>()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<ThrowingHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new AfterExceptionRequest(1), TestContext.Current.CancellationToken));

        AfterHandlerExceptionBehavior.AfterCodeRan.Should().BeTrue();
    }
}
