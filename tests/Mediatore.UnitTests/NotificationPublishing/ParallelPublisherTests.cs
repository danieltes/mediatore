using Mediatore;
using Mediatore.Publishing;

namespace Mediatore.UnitTests.NotificationPublishing;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record StockUpdated(int ProductId) : INotification;

file sealed class StockHandler(int multiplier) : INotificationHandler<StockUpdated>
{
    public List<int> Invocations { get; } = [];
    public async Task Handle(StockUpdated notification, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        Invocations.Add(notification.ProductId * multiplier);
    }
}

file sealed class ThrowingStockHandler(string message) : INotificationHandler<StockUpdated>
{
    public Task Handle(StockUpdated notification, CancellationToken cancellationToken)
        => throw new InvalidOperationException(message);
}

// ---------------------------------------------------------------------------
// Tests for ParallelPublisher
// ---------------------------------------------------------------------------

public sealed class ParallelPublisherTests
{
    [Fact]
    public async Task Publish_AllHandlers_EventuallyInvoked()
    {
        var h1 = new StockHandler(1);
        var h2 = new StockHandler(2);
        var notification = new StockUpdated(7);

        var executors = new[]
        {
            new NotificationHandlerExecutor
            {
                HandlerType = typeof(StockHandler),
                HandlerCallback = (n, ct) => h1.Handle((StockUpdated)n, ct)
            },
            new NotificationHandlerExecutor
            {
                HandlerType = typeof(StockHandler),
                HandlerCallback = (n, ct) => h2.Handle((StockUpdated)n, ct)
            }
        };

        var publisher = new ParallelPublisher();
        await publisher.Publish(executors, notification, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { 7 }, h1.Invocations);
        Assert.Equal(new[] { 14 }, h2.Invocations);
    }

    [Fact]
    public async Task Publish_ZeroHandlers_CompletesWithNoException()
    {
        var publisher = new ParallelPublisher();
        await publisher.Publish([], new StockUpdated(1), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_MultipleHandlersThrow_WrapsInAggregateException()
    {
        var notification = new StockUpdated(1);

        var executors = new[]
        {
            new NotificationHandlerExecutor
            {
                HandlerType = typeof(ThrowingStockHandler),
                HandlerCallback = (n, ct) => new ThrowingStockHandler("Error1").Handle((StockUpdated)n, ct)
            },
            new NotificationHandlerExecutor
            {
                HandlerType = typeof(ThrowingStockHandler),
                HandlerCallback = (n, ct) => new ThrowingStockHandler("Error2").Handle((StockUpdated)n, ct)
            }
        };

        var publisher = new ParallelPublisher();
        Func<Task> act = () => publisher.Publish(executors, notification, TestContext.Current.CancellationToken);
        var result = await act.Should().ThrowAsync<AggregateException>();
        result.Which.InnerExceptions.Count.Should().Be(2);
    }
}
