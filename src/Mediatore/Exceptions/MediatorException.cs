namespace Mediatore;

/// <summary>Base class for all Mediatore library exceptions.</summary>
public abstract class MediatorException : InvalidOperationException
{
    protected MediatorException(string message) : base(message) { }

    protected MediatorException(string message, Exception innerException)
        : base(message, innerException) { }
}
