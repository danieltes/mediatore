using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mediatore.SourceGenerator.Tests;

internal static class SourceGeneratorTestHelper
{
    /// <summary>
    /// Runs the <see cref="MediatorHandlerGenerator"/> against <paramref name="source"/> and
    /// returns all diagnostics emitted by the generator.
    /// </summary>
    public static IReadOnlyList<Diagnostic> RunGenerator(string source)
        => RunGeneratorWithOutput(source).Diagnostics;

    /// <summary>
    /// Runs the <see cref="MediatorHandlerGenerator"/> and returns both diagnostics and
    /// generated source texts.
    /// </summary>
    public static (IReadOnlyList<Diagnostic> Diagnostics,
                   IReadOnlyList<GeneratedSourceResult> GeneratedSources)
        RunGeneratorWithOutput(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Reference the Mediatore core assembly so the interfaces resolve in the generator's
        // semantic model.
        var mediatoreRef = MetadataReference.CreateFromFile(typeof(IRequest<>).Assembly.Location);
        var runtimeRef = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        // System.Threading.Tasks
        var tasksRef = MetadataReference.CreateFromFile(
            typeof(System.Threading.Tasks.Task).Assembly.Location);

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: [runtimeRef, mediatoreRef, tasksRef],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new MediatorHandlerGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .ToList();

        return (diagnostics, generatedSources);
    }
}
