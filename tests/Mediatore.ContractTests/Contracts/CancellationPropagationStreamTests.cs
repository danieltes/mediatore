using Mediatore;

namespace Mediatore.ContractTests.Contracts;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record SlowDataRequest(int Count) : IStreamRequest<int>;

file sealed class SlowDataHandler : IStreamRequestHandler<SlowDataRequest, int>
{
    public async IAsyncEnumerable<int> Handle(
        SlowDataRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken);
            yield return i;
        }
    }
}

// ---------------------------------------------------------------------------
// REQ-05 (stream path): CancellationToken propagation for IStreamRequestHandler.Handle
// ---------------------------------------------------------------------------

public sealed class CancellationPropagationStreamTests
{
    [Fact]
    public async Task CreateStream_PreCancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<SlowDataHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in mediator.CreateStream(
                new SlowDataRequest(10), cts.Token))
            { }
        });
    }

    [Fact]
    public async Task CreateStream_TokenCancelledMidStream_StopsIteration()
    {
        using var cts = new CancellationTokenSource();

        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<SlowDataHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var items = new List<int>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in mediator.CreateStream(new SlowDataRequest(10), cts.Token))
            {
                items.Add(item);
                if (items.Count == 2)
                    await cts.CancelAsync();
            }
        });

        Assert.True(items.Count < 10);
    }
}
