using Microsoft.CodeAnalysis;

namespace Mediatore.SourceGenerator;

internal static class Diagnostics
{
    private const string Category = "Mediatore";

    /// <summary>
    /// MED0001 — Duplicate handler registered for the same closed request type.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateHandler = new(
        id: "MED0001",
        title: "Duplicate request handler",
        messageFormat: "Multiple handlers found for '{0}': '{1}' and '{2}'. Only one handler per request type is allowed.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// MED0002 — Handler class is not sealed.
    /// </summary>
    public static readonly DiagnosticDescriptor NonSealedHandler = new(
        id: "MED0002",
        title: "Non-sealed handler",
        messageFormat: "Handler '{0}' should be sealed to prevent accidental inheritance and improve performance",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
