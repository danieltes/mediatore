using Mediatore;

namespace Mediatore.IntegrationTests.StreamScenarios;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record RangeRequest(int From, int To) : IStreamRequest<int>;

file sealed class RangeHandler : IStreamRequestHandler<RangeRequest, int>
{
    public async IAsyncEnumerable<int> Handle(
        RangeRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = request.From; i <= request.To; i++)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
        }
    }
}

file sealed record UnknownStreamRequest : IStreamRequest<int>;

// ---------------------------------------------------------------------------
// Stream scenario integration tests
// ---------------------------------------------------------------------------

public sealed class StreamScenarioTests
{
    [Fact]
    public async Task CreateStream_FullDispatch_YieldsAllItems()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<RangeHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var results = new List<int>();

        await foreach (var item in mediator.CreateStream(
            new RangeRequest(1, 5), TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, results);
    }

    [Fact]
    public async Task CreateStream_CancelledMidStream_StopsIteration()
    {
        using var cts = new CancellationTokenSource();

        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<RangeHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var items = new List<int>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in mediator.CreateStream(new RangeRequest(1, 100), cts.Token))
            {
                items.Add(item);
                if (items.Count == 3)
                    await cts.CancelAsync();
            }
        });

        Assert.True(items.Count < 100);
    }

    [Fact]
    public async Task CreateStream_UnregisteredHandler_ThrowsHandlerNotFoundException()
    {
        using var sp = new ServiceCollection()
            .AddMediator()
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
        {
            await foreach (var _ in mediator.CreateStream(
                new UnknownStreamRequest(), TestContext.Current.CancellationToken))
            { }
        });
    }
}
