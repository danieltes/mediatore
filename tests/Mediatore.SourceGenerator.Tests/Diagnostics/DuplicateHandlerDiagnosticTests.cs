using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Mediatore.SourceGenerator.Tests.Diagnostics;

public sealed class DuplicateHandlerDiagnosticTests
{
    [Fact]
    public void TwoHandlersForSameRequest_EmitsMED0001()
    {
        const string source = """
            using Mediatore;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record GetProductQuery(int Id) : IRequest<string>;

            public sealed class Handler1 : IRequestHandler<GetProductQuery, string>
            {
                public Task<string> Handle(GetProductQuery request, CancellationToken ct)
                    => Task.FromResult("handler1");
            }

            public sealed class Handler2 : IRequestHandler<GetProductQuery, string>
            {
                public Task<string> Handle(GetProductQuery request, CancellationToken ct)
                    => Task.FromResult("handler2");
            }
            """;

        var diagnostics = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "MED0001");
    }

    private static IReadOnlyList<Diagnostic> RunGenerator(string source)
        => SourceGeneratorTestHelper.RunGenerator(source);
}
