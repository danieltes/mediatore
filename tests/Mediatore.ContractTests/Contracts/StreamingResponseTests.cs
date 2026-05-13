using Mediatore;

namespace Mediatore.ContractTests.Contracts;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record DataStreamRequest(int Count) : IStreamRequest<string>;

file sealed class DataStreamHandler : IStreamRequestHandler<DataStreamRequest, string>
{
    public static CancellationToken LastToken;
    public async IAsyncEnumerable<string> Handle(
        DataStreamRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        LastToken = cancellationToken;
        for (int i = 0; i < request.Count; i++)
        {
            await Task.Yield();
            yield return $"item-{i}";
        }
    }
}

file sealed record UnregisteredStreamRequest : IStreamRequest<int>;

// ---------------------------------------------------------------------------
// REQ-06: CreateStream is lazy, CancellationToken is forwarded,
//         HandlerNotFoundException thrown for unregistered type.
// ---------------------------------------------------------------------------

public sealed class StreamingResponseTests
{
    [Fact]
    public async Task CreateStream_YieldsItemsAndForwardsToken()
    {
        using var cts = new CancellationTokenSource();
        DataStreamHandler.LastToken = default;

        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<DataStreamHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var results = new List<string>();

        await foreach (var item in mediator.CreateStream(new DataStreamRequest(3), cts.Token))
            results.Add(item);

        Assert.Equal(new[] { "item-0", "item-1", "item-2" }, results);
        DataStreamHandler.LastToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task CreateStream_UnregisteredType_ThrowsHandlerNotFoundException()
    {
        using var sp = new ServiceCollection()
            .AddMediator()
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        Func<Task> act = async () =>
        {
            await foreach (var _ in mediator.CreateStream(
                new UnregisteredStreamRequest(), TestContext.Current.CancellationToken))
            { }
        };
        await act.Should().ThrowAsync<HandlerNotFoundException>();
    }
}
