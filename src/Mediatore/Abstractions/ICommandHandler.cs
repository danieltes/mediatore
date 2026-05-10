namespace Mediatore;

/// <summary>
/// Handles a <typeparamref name="TCommand"/> that produces no meaningful return value.
/// Registered by the DI layer as <see cref="IRequestHandler{TCommand, Unit}"/>.
/// </summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task Handle(TCommand command, CancellationToken cancellationToken);
}
