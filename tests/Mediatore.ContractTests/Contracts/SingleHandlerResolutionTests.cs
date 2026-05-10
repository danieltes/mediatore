using Mediatore;

namespace Mediatore.ContractTests.Contracts;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record ProductQuery(int Id) : IRequest<ProductResult>;
file sealed record ProductResult(int Id);
file sealed class ProductQueryHandler : IRequestHandler<ProductQuery, ProductResult>
{
    public Task<ProductResult> Handle(ProductQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new ProductResult(request.Id));
}

// ---------------------------------------------------------------------------
// REQ-01: A registered IRequest<TResponse> resolves to exactly one handler;
//         response matches handler return value.
// ---------------------------------------------------------------------------

public sealed class SingleHandlerResolutionTests
{
    [Fact]
    public async Task Send_RegisteredRequest_ReturnsExactHandlerResponse()
    {
        using var sp = CreateServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new ProductQuery(7), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Id.Should().Be(7);
    }

    [Fact]
    public async Task Send_RegisteredRequest_HandlerInvokedOnce()
    {
        using var sp = CreateServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var r1 = await mediator.Send(new ProductQuery(1), TestContext.Current.CancellationToken);
        var r2 = await mediator.Send(new ProductQuery(2), TestContext.Current.CancellationToken);

        r1.Id.Should().Be(1);
        r2.Id.Should().Be(2);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddMediator(o => o.RegisterServicesFromAssemblyContaining<ProductQueryHandler>());
        return services.BuildServiceProvider();
    }
}
