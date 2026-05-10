using Mediatore;

namespace Mediatore.IntegrationTests.LifetimeScenarios;

// ---------------------------------------------------------------------------
// Stubs — tracks instance identity for lifetime verification
// ---------------------------------------------------------------------------

file sealed record ScopedRequest : IRequest<Guid>;
file sealed record SingletonRequest : IRequest<Guid>;

file sealed class ScopedInstanceHandler : IRequestHandler<ScopedRequest, Guid>
{
    private readonly Guid _id = Guid.NewGuid();
    public Task<Guid> Handle(ScopedRequest request, CancellationToken cancellationToken)
        => Task.FromResult(_id);
}

file sealed class SingletonInstanceHandler : IRequestHandler<SingletonRequest, Guid>
{
    private readonly Guid _id = Guid.NewGuid();
    public Task<Guid> Handle(SingletonRequest request, CancellationToken cancellationToken)
        => Task.FromResult(_id);
}

// ---------------------------------------------------------------------------
// Lifetime scenario tests
// ---------------------------------------------------------------------------

public sealed class LifetimeScenarioTests
{
    [Fact]
    public async Task ScopedHandler_TwoScopes_GetDifferentInstances()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o =>
            {
                o.Lifetime = ServiceLifetime.Scoped;
                o.RegisterServicesFromAssemblyContaining<ScopedInstanceHandler>();
            })
            .BuildServiceProvider();

        Guid id1, id2;

        using (var scope1 = sp.CreateScope())
        {
            var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
            id1 = await mediator1.Send(new ScopedRequest(), TestContext.Current.CancellationToken);
        }

        using (var scope2 = sp.CreateScope())
        {
            var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();
            id2 = await mediator2.Send(new ScopedRequest(), TestContext.Current.CancellationToken);
        }

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public async Task SingletonHandler_AcrossScopes_GetsTheSameInstance()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o =>
            {
                o.Lifetime = ServiceLifetime.Singleton;
                o.RegisterServicesFromAssemblyContaining<ScopedInstanceHandler>();
            })
            .BuildServiceProvider();

        Guid id1, id2;

        using (var scope1 = sp.CreateScope())
        {
            var mediator = scope1.ServiceProvider.GetRequiredService<IMediator>();
            id1 = await mediator.Send(new SingletonRequest(), TestContext.Current.CancellationToken);
        }

        using (var scope2 = sp.CreateScope())
        {
            var mediator = scope2.ServiceProvider.GetRequiredService<IMediator>();
            id2 = await mediator.Send(new SingletonRequest(), TestContext.Current.CancellationToken);
        }

        Assert.Equal(id1, id2);
    }
}
