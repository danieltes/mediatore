using Mediatore;

namespace Mediatore.UnitTests.StreamRequests;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record SlowStreamRequest(int Count) : IStreamRequest<int>;

file sealed class SlowStreamHandler : IStreamRequestHandler<SlowStreamRequest, int>
{
    public async IAsyncEnumerable<int> Handle(
        SlowStreamRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 1; i <= request.Count; i++)
        {
            await Task.Delay(50, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
        }
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public sealed class StreamCancellationTests
{
    [Fact]
    public async Task Stream_CancelledMidStream_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();

        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<SlowStreamHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var items = new List<int>();

        Func<Task> act = async () =>
        {
            await foreach (var item in mediator.CreateStream(new SlowStreamRequest(10), cts.Token))
            {
                items.Add(item);
                if (items.Count == 1)
                    await cts.CancelAsync();
            }
        };
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Fewer items than requested — cancellation stopped iteration
        items.Count.Should().BeLessThan(10);
    }
}
