using System.Reflection;
using Mediatore;
using Mediatore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mediatore.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to register Mediatore in an <see cref="IServiceCollection"/>.
/// </summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator and all handlers found in the assemblies configured via
    /// <paramref name="configure"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <see cref="MediatorOptions.NotificationPublisher"/> is <see langword="null"/> when
    /// <paramref name="configure"/> is evaluated.
    /// </exception>
    /// <exception cref="DuplicateHandlerException">
    /// Two or more handlers are registered for the same closed request type.
    /// </exception>
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        Action<MediatorOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MediatorOptions();
        configure?.Invoke(options);

        if (options.NotificationPublisher is null)
            throw new ArgumentNullException(
                nameof(options.NotificationPublisher),
                "MediatorOptions.NotificationPublisher must not be null.");

        var assemblies = options.Assemblies;

        // --- Scan handlers ---
        var requestHandlerRegistrations = AssemblyScanner.FindRequestHandlers(assemblies);
        var notificationHandlerRegistrations = AssemblyScanner.FindNotificationHandlers(assemblies);
        var streamHandlerRegistrations = AssemblyScanner.FindStreamHandlers(assemblies);

        // --- Register individual handler types in DI ---
        foreach (var reg in requestHandlerRegistrations)
        {
            RegisterHandlerService(services, reg.HandlerType, reg.RequestType, reg.ResponseType, options.Lifetime);
        }

        foreach (var reg in notificationHandlerRegistrations)
        {
            var serviceType = typeof(INotificationHandler<>).MakeGenericType(reg.NotificationType);
            services.Add(new ServiceDescriptor(serviceType, reg.HandlerType, options.Lifetime));
            // Also register by concrete type so Mediator can resolve specific handler instances.
            services.TryAdd(new ServiceDescriptor(reg.HandlerType, reg.HandlerType, options.Lifetime));
        }

        foreach (var reg in streamHandlerRegistrations)
        {
            var serviceType = typeof(IStreamRequestHandler<,>).MakeGenericType(reg.RequestType, reg.ResponseType);
            services.Add(new ServiceDescriptor(serviceType, reg.HandlerType, options.Lifetime));
        }

        // --- Build HandlerRegistry ---
        var requestEntries = BuildRequestRegistryEntries(requestHandlerRegistrations);
        var notificationEntries = notificationHandlerRegistrations
            .Select(r => (r.NotificationType, r.HandlerType))
            .ToList();
        var streamEntries = BuildStreamRegistryEntries(streamHandlerRegistrations);

        // This is where DuplicateHandlerException is raised (at build / AddMediator time).
        var registry = new HandlerRegistry(requestEntries, notificationEntries, streamEntries);

        services.TryAddSingleton(registry);

        // --- Register INotificationPublisher ---
        var publisher = options.NotificationPublisher;
        services.TryAddSingleton(publisher);
        services.TryAddSingleton<INotificationPublisher>(sp =>
            sp.GetRequiredService(publisher.GetType()) as INotificationPublisher
            ?? publisher);

        // --- Register IMediator ---
        // Registered as Scoped so it receives the scope's IServiceProvider,
        // allowing Scoped handlers to be resolved correctly per scope.
        services.TryAddScoped<IMediator, Mediator>();

        return services;
    }

    private static void RegisterHandlerService(
        IServiceCollection services,
        Type handlerType,
        Type requestType,
        Type responseType,
        ServiceLifetime lifetime)
    {
        var ifaces = handlerType.GetInterfaces();
        bool isCommandHandler = ifaces.Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));

        if (isCommandHandler)
        {
            // Register the concrete ICommandHandler<TCommand> itself so it can be resolved.
            var commandHandlerServiceType = typeof(ICommandHandler<>).MakeGenericType(requestType);
            services.Add(new ServiceDescriptor(commandHandlerServiceType, handlerType, lifetime));

            // Register the adapter as IRequestHandler<TCommand, Unit>.
            var adapterType = typeof(CommandHandlerAdapter<>).MakeGenericType(requestType);
            var requestHandlerServiceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
            services.Add(new ServiceDescriptor(requestHandlerServiceType, adapterType, lifetime));
        }
        else
        {
            var requestHandlerServiceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
            services.Add(new ServiceDescriptor(requestHandlerServiceType, handlerType, lifetime));
        }
    }

    private static IEnumerable<(Type RequestType, Type HandlerType, object Wrapper)>
        BuildRequestRegistryEntries(
            IEnumerable<AssemblyScanner.RequestHandlerRegistration> registrations)
    {
        foreach (var reg in registrations)
        {
            var wrapperType = typeof(RequestHandlerWrapper<,>)
                .MakeGenericType(reg.RequestType, reg.ResponseType);
            var wrapper = Activator.CreateInstance(wrapperType)!;
            yield return (reg.RequestType, reg.HandlerType, wrapper);
        }
    }

    private static IEnumerable<(Type RequestType, Type HandlerType, object Wrapper)>
        BuildStreamRegistryEntries(
            IEnumerable<AssemblyScanner.StreamHandlerRegistration> registrations)
    {
        foreach (var reg in registrations)
        {
            var wrapperType = typeof(StreamHandlerWrapper<,>)
                .MakeGenericType(reg.RequestType, reg.ResponseType);
            var wrapper = Activator.CreateInstance(wrapperType)!;
            yield return (reg.RequestType, reg.HandlerType, wrapper);
        }
    }
}
