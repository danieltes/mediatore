using System.Reflection;
using Mediatore;

namespace Mediatore.Extensions.DependencyInjection;

/// <summary>
/// Scans assemblies for Mediatore handler implementations.
/// Discovers only concrete, non-abstract, non-open-generic classes.
/// </summary>
internal static class AssemblyScanner
{
    private static readonly Type RequestHandlerOpenType = typeof(IRequestHandler<,>);
    private static readonly Type CommandHandlerOpenType = typeof(ICommandHandler<>);
    private static readonly Type NotificationHandlerOpenType = typeof(INotificationHandler<>);
    private static readonly Type StreamHandlerOpenType = typeof(IStreamRequestHandler<,>);

    internal record struct RequestHandlerRegistration(
        Type HandlerType,
        Type RequestType,
        Type ResponseType);

    internal record struct NotificationHandlerRegistration(
        Type HandlerType,
        Type NotificationType);

    internal record struct StreamHandlerRegistration(
        Type HandlerType,
        Type RequestType,
        Type ResponseType);

    internal static IReadOnlyList<RequestHandlerRegistration> FindRequestHandlers(
        IEnumerable<Assembly> assemblies)
    {
        var results = new List<RequestHandlerRegistration>();

        foreach (var type in GetCandidateTypes(assemblies))
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                var def = iface.GetGenericTypeDefinition();

                if (def == RequestHandlerOpenType)
                {
                    var args = iface.GetGenericArguments();
                    results.Add(new(type, args[0], args[1]));
                }
                else if (def == CommandHandlerOpenType)
                {
                    // ICommandHandler<TCommand> is an adapter; register as
                    // IRequestHandler<TCommand, Unit> via CommandHandlerAdapter.
                    var commandType = iface.GetGenericArguments()[0];
                    results.Add(new(type, commandType, typeof(Unit)));
                }
            }
        }

        return results;
    }

    internal static IReadOnlyList<NotificationHandlerRegistration> FindNotificationHandlers(
        IEnumerable<Assembly> assemblies)
    {
        var results = new List<NotificationHandlerRegistration>();

        foreach (var type in GetCandidateTypes(assemblies))
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                if (iface.GetGenericTypeDefinition() != NotificationHandlerOpenType) continue;

                var notifType = iface.GetGenericArguments()[0];
                results.Add(new(type, notifType));
            }
        }

        return results;
    }

    internal static IReadOnlyList<StreamHandlerRegistration> FindStreamHandlers(
        IEnumerable<Assembly> assemblies)
    {
        var results = new List<StreamHandlerRegistration>();

        foreach (var type in GetCandidateTypes(assemblies))
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                if (iface.GetGenericTypeDefinition() != StreamHandlerOpenType) continue;

                var args = iface.GetGenericArguments();
                results.Add(new(type, args[0], args[1]));
            }
        }

        return results;
    }

    private static IEnumerable<Type> GetCandidateTypes(IEnumerable<Assembly> assemblies)
        => assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });
}
