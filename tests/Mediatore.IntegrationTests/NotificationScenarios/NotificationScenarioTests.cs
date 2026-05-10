using Mediatore;
using Mediatore.Publishing;

namespace Mediatore.IntegrationTests.NotificationScenarios;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record SequentialEvent(string Email) : INotification;
file sealed record ParallelEvent(string Email) : INotification;
file sealed record FailingEvent(string Email) : INotification;

file sealed class WelcomeEmailHandler : INotificationHandler<SequentialEvent>
{
    public static readonly List<string> Invocations = [];
    public Task Handle(SequentialEvent notification, CancellationToken cancellationToken)
    {
        Invocations.Add($"email:{notification.Email}");
        return Task.CompletedTask;
    }
}

file sealed class AuditHandler : INotificationHandler<SequentialEvent>
{
    public static readonly List<string> Invocations = [];
    public Task Handle(SequentialEvent notification, CancellationToken cancellationToken)
    {
        Invocations.Add($"audit:{notification.Email}");
        return Task.CompletedTask;
    }
}

file sealed class ParallelWelcomeHandler : INotificationHandler<ParallelEvent>
{
    public static readonly List<string> Invocations = [];
    public Task Handle(ParallelEvent notification, CancellationToken cancellationToken)
    {
        Invocations.Add($"email:{notification.Email}");
        return Task.CompletedTask;
    }
}

file sealed class ParallelAuditHandler : INotificationHandler<ParallelEvent>
{
    public static readonly List<string> Invocations = [];
    public Task Handle(ParallelEvent notification, CancellationToken cancellationToken)
    {
        Invocations.Add($"audit:{notification.Email}");
        return Task.CompletedTask;
    }
}

file sealed class FailingHandler : INotificationHandler<FailingEvent>
{
    public Task Handle(FailingEvent notification, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Failed to send");
}

file sealed record NoOpNotification : INotification;

// ---------------------------------------------------------------------------
// Notification scenario tests
// ---------------------------------------------------------------------------

public sealed class NotificationScenarioTests
{
    [Fact]
    public async Task SequentialPublisher_TwoHandlers_BothInvokedInOrder()
    {
        WelcomeEmailHandler.Invocations.Clear();
        AuditHandler.Invocations.Clear();

        using var sp = new ServiceCollection()
            .AddMediator(o =>
            {
                o.NotificationPublisher = new SequentialPublisher();
                o.RegisterServicesFromAssemblyContaining<WelcomeEmailHandler>();
            })
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Publish(new SequentialEvent("test@example.com"), TestContext.Current.CancellationToken);

        Assert.Contains("email:test@example.com", WelcomeEmailHandler.Invocations);
        Assert.Contains("audit:test@example.com", AuditHandler.Invocations);
    }

    [Fact]
    public async Task ParallelPublisher_TwoHandlers_BothInvoked()
    {
        ParallelWelcomeHandler.Invocations.Clear();
        ParallelAuditHandler.Invocations.Clear();

        using var sp = new ServiceCollection()
            .AddMediator(o =>
            {
                o.NotificationPublisher = new ParallelPublisher();
                o.RegisterServicesFromAssemblyContaining<ParallelWelcomeHandler>();
            })
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Publish(new ParallelEvent("parallel@example.com"), TestContext.Current.CancellationToken);

        Assert.Contains("email:parallel@example.com", ParallelWelcomeHandler.Invocations);
        Assert.Contains("audit:parallel@example.com", ParallelAuditHandler.Invocations);
    }

    [Fact]
    public async Task SequentialPublisher_OneHandlerThrows_ExceptionPropagates()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o =>
            {
                o.NotificationPublisher = new SequentialPublisher();
                o.RegisterServicesFromAssemblyContaining<FailingHandler>();
            })
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Publish(new FailingEvent("fail@example.com"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Publish_ZeroHandlers_NoException()
    {
        using var sp = new ServiceCollection()
            .AddMediator()
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Publish(new NoOpNotification(), TestContext.Current.CancellationToken);
    }
}
