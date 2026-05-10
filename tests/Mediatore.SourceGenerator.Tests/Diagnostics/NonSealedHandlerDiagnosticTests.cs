using Microsoft.CodeAnalysis;
using Xunit;

namespace Mediatore.SourceGenerator.Tests.Diagnostics;

public sealed class NonSealedHandlerDiagnosticTests
{
    [Fact]
    public void NonSealedHandler_EmitsMED0002()
    {
        const string source = """
            using Mediatore;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record MyQuery(int Id) : IRequest<string>;

            public class NonSealedHandler : IRequestHandler<MyQuery, string>
            {
                public Task<string> Handle(MyQuery request, CancellationToken ct)
                    => Task.FromResult("ok");
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "MED0002");
    }
}
