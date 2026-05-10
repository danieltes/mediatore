using Mediatore;

namespace Mediatore.ContractTests.Contracts;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record OrderShipped(int OrderId) : INotification;

file sealed class ShipHandler1 : INotificationHandler<OrderShipped>
{
    public static int CallCount;
    public Task Handle(OrderShipped notification, CancellationToken cancellationToken)
    {
        System.Threading.Interlocked.Increment(ref CallCount);
        return Task.CompletedTask;
    }
}

file sealed class ShipHandler2 : INotificationHandler<OrderShipped>
{
    public static int CallCount;
    public Task Handle(OrderShipped notification, CancellationToken cancellationToken)
    {
        System.Threading.Interlocked.Increment(ref CallCount);
        return Task.CompletedTask;
    }
}

file sealed record UnhandledNotification : INotification;

// ---------------------------------------------------------------------------
// REQ-04: Publish invokes all registered handlers;
//         zero-handler Publish completes without exception.
// ---------------------------------------------------------------------------

public sealed class NotificationFanOutTests
{
    [Fact]
    public async Task Publish_TwoHandlers_BothInvoked()
    {
        ShipHandler1.CallCount = 0;
        ShipHandler2.CallCount = 0;

        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<ShipHandler1>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Publish(new OrderShipped(1), TestContext.Current.CancellationToken);

        ShipHandler1.CallCount.Should().Be(1);
        ShipHandler2.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Publish_NoHandlers_CompletesWithoutException()
    {
        using var sp = new ServiceCollection()
            .AddMediator()
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        // Should not throw — UnhandledNotification has no registered handlers
        await mediator.Publish(new UnhandledNotification(), TestContext.Current.CancellationToken);
    }
}
