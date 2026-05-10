namespace Mediatore;

/// <summary>Marker for a void command (returns <see cref="Unit"/>).</summary>
public interface ICommand : IRequest<Unit> { }
