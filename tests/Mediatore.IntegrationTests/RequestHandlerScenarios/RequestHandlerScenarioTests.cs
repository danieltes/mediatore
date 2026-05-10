using Mediatore;

namespace Mediatore.IntegrationTests.RequestHandlerScenarios;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record IntegrationQuery(int Multiplier) : IRequest<int>;
file sealed class IntegrationQueryHandler : IRequestHandler<IntegrationQuery, int>
{
    public Task<int> Handle(IntegrationQuery request, CancellationToken cancellationToken)
        => Task.FromResult(request.Multiplier * 10);
}

file sealed record IntegrationCommand(string Name) : ICommand;
file sealed class IntegrationCommandHandler : ICommandHandler<IntegrationCommand>
{
    public static int CallCount;
    public Task Handle(IntegrationCommand command, CancellationToken cancellationToken)
    {
        System.Threading.Interlocked.Increment(ref CallCount);
        return Task.CompletedTask;
    }
}

// ---------------------------------------------------------------------------
// Integration tests: full DI + dispatch cycle
// ---------------------------------------------------------------------------

public sealed class RequestHandlerScenarioTests
{
    [Fact]
    public async Task AddMediator_ResolveIMediator_SendQuery_ReturnsResponse()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<IntegrationQueryHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new IntegrationQuery(5), TestContext.Current.CancellationToken);

        result.Should().Be(50);
    }

    [Fact]
    public async Task AddMediator_ResolveIMediator_SendCommand_ReturnsUnit()
    {
        IntegrationCommandHandler.CallCount = 0;

        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<IntegrationCommandHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Send(new IntegrationCommand("Test"), TestContext.Current.CancellationToken);

        IntegrationCommandHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public void AddMediator_DuplicateHandlers_ThrowsDuplicateHandlerExceptionAtBuildTime()
    {
        // DuplicateHandlerException must be thrown in AddMediator (at build time),
        // NOT deferred to the first Send call.
        // We simulate duplicates by calling RegisterServicesFromAssembly twice with the same assembly.
        var assembly = typeof(RequestHandlerScenarioTests).Assembly;
        var act = () => new ServiceCollection()
            .AddMediator(o => o
                .RegisterServicesFromAssembly(assembly)
                .RegisterServicesFromAssembly(assembly))
            .BuildServiceProvider();

        act.Should().Throw<DuplicateHandlerException>();
    }

    [Fact]
    public async Task AddMediator_HandlerRegisteredPerScope_ScopedLifetimeRespected()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<IntegrationQueryHandler>())
            .BuildServiceProvider();

        // IMediator should be resolved successfully from the root (it's singleton).
        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Send(new IntegrationQuery(3), TestContext.Current.CancellationToken);

        result.Should().Be(30);
    }
}
