using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mediatore.SourceGenerator;

[Generator]
public sealed class MediatorHandlerGenerator : IIncrementalGenerator
{
    // Handler interface names (short, unqualified)
    private const string IRequestHandler = "IRequestHandler";
    private const string INotificationHandler = "INotificationHandler";
    private const string IStreamRequestHandler = "IStreamRequestHandler";
    private const string ICommandHandler = "ICommandHandler";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Collect all class declarations that might be handlers
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cls &&
                    cls.BaseList is not null,
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Collect();

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations);

        context.RegisterSourceOutput(compilationAndClasses, (spc, source) =>
        {
            var (compilation, classes) = source;
            Execute(compilation, classes, spc);
        });
    }

    private static void Execute(
        Compilation compilation,
        System.Collections.Immutable.ImmutableArray<ClassDeclarationSyntax> classes,
        SourceProductionContext context)
    {
        // Resolve key handler interface symbols
        var requestHandlerSymbol = compilation.GetTypeByMetadataName("Mediatore.IRequestHandler`2");
        var notificationHandlerSymbol = compilation.GetTypeByMetadataName("Mediatore.INotificationHandler`1");
        var streamHandlerSymbol = compilation.GetTypeByMetadataName("Mediatore.IStreamRequestHandler`2");
        var commandHandlerSymbol = compilation.GetTypeByMetadataName("Mediatore.ICommandHandler`1");

        if (requestHandlerSymbol is null && notificationHandlerSymbol is null &&
            streamHandlerSymbol is null && commandHandlerSymbol is null)
            return; // Mediatore not referenced

        var requestHandlers = new List<(INamedTypeSymbol HandlerType, INamedTypeSymbol RequestType, INamedTypeSymbol ResponseType)>();
        var notificationHandlers = new List<(INamedTypeSymbol HandlerType, INamedTypeSymbol NotificationType)>();
        var streamHandlers = new List<(INamedTypeSymbol HandlerType, INamedTypeSymbol RequestType, INamedTypeSymbol ResponseType)>();

        foreach (var classDecl in classes.Distinct())
        {
            var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
                continue;

            if (classSymbol.IsAbstract) continue;

            bool isHandler = false;

            foreach (var iface in classSymbol.AllInterfaces)
            {
                if (!iface.IsGenericType) continue;

                var def = iface.ConstructedFrom;

                if (requestHandlerSymbol is not null &&
                    SymbolEqualityComparer.Default.Equals(def, requestHandlerSymbol))
                {
                    isHandler = true;
                    var requestType = (INamedTypeSymbol)iface.TypeArguments[0];
                    var responseType = (INamedTypeSymbol)iface.TypeArguments[1];
                    requestHandlers.Add((classSymbol, requestType, responseType));
                }
                else if (notificationHandlerSymbol is not null &&
                    SymbolEqualityComparer.Default.Equals(def, notificationHandlerSymbol))
                {
                    isHandler = true;
                    var notificationType = (INamedTypeSymbol)iface.TypeArguments[0];
                    notificationHandlers.Add((classSymbol, notificationType));
                }
                else if (streamHandlerSymbol is not null &&
                    SymbolEqualityComparer.Default.Equals(def, streamHandlerSymbol))
                {
                    isHandler = true;
                    var requestType = (INamedTypeSymbol)iface.TypeArguments[0];
                    var responseType = (INamedTypeSymbol)iface.TypeArguments[1];
                    streamHandlers.Add((classSymbol, requestType, responseType));
                }
                else if (commandHandlerSymbol is not null &&
                    SymbolEqualityComparer.Default.Equals(def, commandHandlerSymbol))
                {
                    isHandler = true;
                    // ICommandHandler<TCommand> is treated as IRequestHandler<TCommand, Unit>
                    // (just register as notification handler — actual wrapping is done at runtime)
                }
            }

            // MED0002: warn if handler class is not sealed
            if (isHandler && !classSymbol.IsSealed)
            {
                var location = classDecl.Identifier.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.NonSealedHandler,
                    location,
                    classSymbol.Name));
            }
        }

        // MED0001: duplicate request handlers
        var requestHandlerGroups = requestHandlers
            .GroupBy(r => r.RequestType, SymbolEqualityComparer.Default)
            .Where(g => g.Count() > 1);

        foreach (var group in requestHandlerGroups)
        {
            var handlers = group.ToList();
            var location = handlers[0].HandlerType.Locations.FirstOrDefault() ?? Location.None;
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.DuplicateHandler,
                location,
                group.Key?.Name ?? "?",
                handlers[0].HandlerType.Name,
                handlers[1].HandlerType.Name));
        }

        // Emit registration code if no duplicate errors
        if (!requestHandlerGroups.Any())
        {
            EmitRegistrations(context, requestHandlers, notificationHandlers, streamHandlers);
        }
    }

    private static void EmitRegistrations(
        SourceProductionContext context,
        List<(INamedTypeSymbol HandlerType, INamedTypeSymbol RequestType, INamedTypeSymbol ResponseType)> requestHandlers,
        List<(INamedTypeSymbol HandlerType, INamedTypeSymbol NotificationType)> notificationHandlers,
        List<(INamedTypeSymbol HandlerType, INamedTypeSymbol RequestType, INamedTypeSymbol ResponseType)> streamHandlers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace Mediatore.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    public static class MediatorRegistrations");
        sb.AppendLine("    {");
        sb.AppendLine("        public static IServiceCollection RegisterGeneratedHandlers(this IServiceCollection services)");
        sb.AppendLine("        {");

        foreach (var (handlerType, requestType, responseType) in requestHandlers)
        {
            var ifaceType = $"global::Mediatore.IRequestHandler<global::{requestType.ToDisplayString()}, global::{responseType.ToDisplayString()}>";
            var concreteType = $"global::{handlerType.ToDisplayString()}";
            sb.AppendLine($"            services.AddTransient<{ifaceType}, {concreteType}>();");
        }

        foreach (var (handlerType, notificationType) in notificationHandlers)
        {
            var ifaceType = $"global::Mediatore.INotificationHandler<global::{notificationType.ToDisplayString()}>";
            var concreteType = $"global::{handlerType.ToDisplayString()}";
            sb.AppendLine($"            services.AddTransient<{ifaceType}, {concreteType}>();");
        }

        foreach (var (handlerType, requestType, responseType) in streamHandlers)
        {
            var ifaceType = $"global::Mediatore.IStreamRequestHandler<global::{requestType.ToDisplayString()}, global::{responseType.ToDisplayString()}>";
            var concreteType = $"global::{handlerType.ToDisplayString()}";
            sb.AppendLine($"            services.AddTransient<{ifaceType}, {concreteType}>();");
        }

        sb.AppendLine("            return services;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("MediatorRegistrations.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}
