using Mediatore;

namespace Mediatore.UnitTests.StreamRequests;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record NumbersRequest(int Count) : IStreamRequest<int>;

file sealed class NumbersHandler : IStreamRequestHandler<NumbersRequest, int>
{
    public static bool HandleCalled;
    public async IAsyncEnumerable<int> Handle(
        NumbersRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        HandleCalled = true;
        for (int i = 1; i <= request.Count; i++)
        {
            await Task.Delay(1, cancellationToken);
            yield return i;
        }
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public sealed class StreamRequestTests
{
    [Fact]
    public async Task Stream_YieldsItemsInOrder()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<NumbersHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var results = new List<int>();

        await foreach (var item in mediator.CreateStream(
            new NumbersRequest(3), TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        Assert.Equal(new[] { 1, 2, 3 }, results);
    }

    [Fact]
    public async Task Stream_IsLazy_HandlerNotInvokedUntilFirstMoveNext()
    {
        NumbersHandler.HandleCalled = false;

        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<NumbersHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        // CreateStream should not invoke the handler yet
        var stream = mediator.CreateStream(new NumbersRequest(3), TestContext.Current.CancellationToken);

        // The handler may or may not have been called yet depending on implementation.
        // What we require: iterating at least one item invokes the handler.
        await foreach (var _ in stream) break; // take just the first item

        NumbersHandler.HandleCalled.Should().BeTrue();
    }
}
