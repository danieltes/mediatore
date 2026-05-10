using Mediatore;

namespace Mediatore.ContractTests.Contracts;

// ---------------------------------------------------------------------------
// Stubs — scoped handler to verify per-scope resolution
// ---------------------------------------------------------------------------

file sealed record ScopedRequest : IRequest<Guid>;

file sealed class ScopedHandler : IRequestHandler<ScopedRequest, Guid>
{
    // A unique ID per instance, verifying each scope gets a new instance.
    private readonly Guid _id = Guid.NewGuid();
    public Task<Guid> Handle(ScopedRequest request, CancellationToken cancellationToken)
        => Task.FromResult(_id);
}

// ---------------------------------------------------------------------------
// REQ-07: Handler resolved per scope; IMediator registered as Singleton
// ---------------------------------------------------------------------------

public sealed class ScopedLifetimeTests
{
    [Fact]
    public async Task ScopedHandler_DifferentScopes_GetDifferentInstances()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o =>
            {
                o.Lifetime = ServiceLifetime.Scoped;
                o.RegisterServicesFromAssemblyContaining<ScopedHandler>();
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

        // Different scopes should produce different handler instances (different GUIDs)
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public async Task IMediator_ScopedPerScope_GetsDifferentInstances()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<ScopedHandler>())
            .BuildServiceProvider();

        IMediator mediator1, mediator2;

        using (var scope1 = sp.CreateScope())
            mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();

        using (var scope2 = sp.CreateScope())
            mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();

        // IMediator is scoped — different scopes get different instances
        Assert.NotSame(mediator1, mediator2);
    }
}
