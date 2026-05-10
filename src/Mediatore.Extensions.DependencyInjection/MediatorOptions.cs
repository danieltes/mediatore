using System.Reflection;
using Mediatore;
using Mediatore.Publishing;
using Microsoft.Extensions.DependencyInjection;

namespace Mediatore.Extensions.DependencyInjection;

/// <summary>
/// Configuration for the mediator and handler registration.
/// </summary>
public sealed class MediatorOptions
{
    private readonly List<Assembly> _assemblies = [];

    /// <summary>
    /// Service lifetime applied to all registered handlers.
    /// Default: <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// The notification dispatch strategy.
    /// Default: <see cref="SequentialPublisher"/>.
    /// </summary>
    public INotificationPublisher NotificationPublisher { get; set; } = new SequentialPublisher();

    /// <summary>The assemblies registered for handler scanning.</summary>
    internal IReadOnlyList<Assembly> Assemblies => _assemblies;

    /// <summary>
    /// Registers all handlers from the assembly containing <typeparamref name="T"/>.
    /// </summary>
    public MediatorOptions RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Registers all handlers from the specified assembly.
    /// </summary>
    public MediatorOptions RegisterServicesFromAssembly(Assembly assembly)
    {
        _assemblies.Add(assembly);
        return this;
    }
}
