namespace Mediatore.Internal;

/// <summary>
/// Bridges <see cref="ICommandHandler{TCommand}"/> to
/// <see cref="IRequestHandler{TRequest,TResponse}"/> with <see cref="Unit"/> response.
/// The DI layer registers this adapter so the dispatch path is uniform.
/// </summary>
internal sealed class CommandHandlerAdapter<TCommand> : IRequestHandler<TCommand, Unit>
    where TCommand : class, ICommand
{
    private readonly ICommandHandler<TCommand> _inner;

    public CommandHandlerAdapter(ICommandHandler<TCommand> inner)
    {
        _inner = inner;
    }

    public async Task<Unit> Handle(TCommand request, CancellationToken cancellationToken)
    {
        await _inner.Handle(request, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
