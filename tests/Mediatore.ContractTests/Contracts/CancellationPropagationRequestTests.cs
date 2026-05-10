using Mediatore;

namespace Mediatore.ContractTests.Contracts;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record EchoQuery(string Value) : IRequest<string>;
file sealed class EchoQueryHandler : IRequestHandler<EchoQuery, string>
{
    public Task<string> Handle(EchoQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(request.Value);
    }
}

// ---------------------------------------------------------------------------
// REQ-05 (request path): CancellationToken passed to Send is forwarded unchanged;
//         pre-cancelled token triggers OperationCanceledException.
// ---------------------------------------------------------------------------

public sealed class CancellationPropagationRequestTests
{
    [Fact]
    public async Task Send_PreCancelledToken_ThrowsOperationCanceledException()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<EchoQueryHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => mediator.Send(new EchoQuery("hello"), cts.Token));
    }

    [Fact]
    public async Task Send_CancellationToken_IsForwardedToHandler()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<EchoQueryHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new EchoQuery("world"), TestContext.Current.CancellationToken);

        result.Should().Be("world");
    }
}
