using Mediatore;
using Mediatore.Publishing;

namespace Mediatore.UnitTests.NotificationPublishing;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record OrderPlaced(int OrderId) : INotification;

file sealed class Handler1 : INotificationHandler<OrderPlaced>
{
    public List<int> Invocations { get; } = [];
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        Invocations.Add(notification.OrderId);
        return Task.CompletedTask;
    }
}

file sealed class Handler2 : INotificationHandler<OrderPlaced>
{
    public List<int> Invocations { get; } = [];
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        Invocations.Add(notification.OrderId * 10);
        return Task.CompletedTask;
    }
}

file sealed class ThrowingNotificationHandler : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Handler threw");
}

// ---------------------------------------------------------------------------
// Tests for SequentialPublisher
// ---------------------------------------------------------------------------

public sealed class SequentialPublisherTests
{
    [Fact]
    public async Task Publish_AllHandlers_InvokedInOrder()
    {
        var h1 = new Handler1();
        var h2 = new Handler2();
        var notification = new OrderPlaced(5);

        var executors = new NotificationHandlerExecutor[]
        {
            new() { HandlerType = h1.GetType(), HandlerCallback = (n, ct) => h1.Handle((OrderPlaced)n, ct) },
            new() { HandlerType = h2.GetType(), HandlerCallback = (n, ct) => h2.Handle((OrderPlaced)n, ct) }
        };

        var publisher = new SequentialPublisher();
        await publisher.Publish(executors, notification, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { 5 }, h1.Invocations);
        Assert.Equal(new[] { 50 }, h2.Invocations);
    }

    [Fact]
    public async Task Publish_ZeroHandlers_CompletesWithNoException()
    {
        var publisher = new SequentialPublisher();
        await publisher.Publish([], new OrderPlaced(1), TestContext.Current.CancellationToken);
        // No assertion needed — should not throw
    }

    [Fact]
    public async Task Publish_FirstHandlerThrows_SecondHandlerNotCalled()
    {
        var h1 = new ThrowingNotificationHandler();
        var h2 = new Handler2();
        var notification = new OrderPlaced(1);

        var executors = new[]
        {
            new NotificationHandlerExecutor
            {
                HandlerType = typeof(ThrowingNotificationHandler),
                HandlerCallback = (n, ct) => h1.Handle((OrderPlaced)n, ct)
            },
            new NotificationHandlerExecutor
            {
                HandlerType = typeof(Handler2),
                HandlerCallback = (n, ct) => h2.Handle((OrderPlaced)n, ct)
            }
        };

        var publisher = new SequentialPublisher();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.Publish(executors, notification, TestContext.Current.CancellationToken));

        Assert.Empty(h2.Invocations);
    }
}
