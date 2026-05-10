namespace Mediatore;

/// <summary>
/// Represents the absence of a meaningful return value.
/// Used as the response type for <see cref="ICommand"/> implementations.
/// </summary>
public readonly record struct Unit
{
    /// <summary>The singleton value. Use instead of <c>new Unit()</c>.</summary>
    public static readonly Unit Value = new();
}
