namespace Mediatore;

/// <summary>Reacts to a <typeparamref name="TNotification"/> as part of a fan-out dispatch.</summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
