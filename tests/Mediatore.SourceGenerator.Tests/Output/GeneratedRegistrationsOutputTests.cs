using Microsoft.CodeAnalysis;
using Xunit;

namespace Mediatore.SourceGenerator.Tests.Output;

public sealed class GeneratedRegistrationsOutputTests
{
    [Fact]
    public void ValidHandler_EmitsRegisterGeneratedHandlers()
    {
        const string source = """
            using Mediatore;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record PingQuery : IRequest<string>;

            public sealed class PingHandler : IRequestHandler<PingQuery, string>
            {
                public Task<string> Handle(PingQuery request, CancellationToken ct)
                    => Task.FromResult("pong");
            }
            """;

        var (_, generatedSources) = SourceGeneratorTestHelper.RunGeneratorWithOutput(source);

        var generatedFile = generatedSources.FirstOrDefault(
            s => s.HintName == "MediatorRegistrations.g.cs");

        generatedFile.SourceText.Should().NotBeNull();

        var content = generatedFile.SourceText.ToString();
        content.Should().Contain("RegisterGeneratedHandlers");
        content.Should().Contain("Mediatore.Generated");
        content.Should().Contain("IServiceCollection");
    }
}
