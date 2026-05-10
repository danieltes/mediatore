using Mediatore;

namespace Mediatore.ContractTests.Contracts;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record InventoryEvent(int ProductId) : INotification;

file sealed class InventoryHandler : INotificationHandler<InventoryEvent>
{
    public static CancellationToken LastToken;
    public Task Handle(InventoryEvent notification, CancellationToken cancellationToken)
    {
        LastToken = cancellationToken;
        return Task.CompletedTask;
    }
}

// ---------------------------------------------------------------------------
// REQ-05 (notification path): CancellationToken is forwarded to all handlers
// ---------------------------------------------------------------------------

public sealed class CancellationPropagationNotificationTests
{
    [Fact]
    public async Task Publish_CancellationToken_IsPropagatedToHandler()
    {
        using var cts = new CancellationTokenSource();
        InventoryHandler.LastToken = default;

        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<InventoryHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Publish(new InventoryEvent(42), cts.Token);

        InventoryHandler.LastToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Publish_PreCancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<InventoryHandler>())
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mediator.Publish(new InventoryEvent(1), cts.Token));
    }
}
