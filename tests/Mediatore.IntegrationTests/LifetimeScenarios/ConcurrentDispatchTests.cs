using Mediatore;

namespace Mediatore.IntegrationTests.LifetimeScenarios;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

file sealed record EchoRequest(int Value) : IRequest<int>;

file sealed class EchoHandler : IRequestHandler<EchoRequest, int>
{
    public Task<int> Handle(EchoRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}

// ---------------------------------------------------------------------------
// FR-016: Concurrent dispatch — N = ProcessorCount * 4
// ---------------------------------------------------------------------------

public sealed class ConcurrentDispatchTests
{
    [Fact]
    public async Task ConcurrentSend_AllTasksComplete_NoExceptionsOrCorruption()
    {
        using var sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<EchoHandler>())
            .BuildServiceProvider();

        int n = Environment.ProcessorCount * 4;
        var tasks = Enumerable.Range(0, n).Select(i =>
            Task.Run(async () =>
            {
                using var scope = sp.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(
                    new EchoRequest(i), TestContext.Current.CancellationToken);
                return (i, result);
            })).ToArray();

        var results = await Task.WhenAll(tasks);

        // Each echo returns the same value it was given — no corruption
        foreach (var (input, output) in results)
            Assert.Equal(input, output);

        Assert.Equal(n, results.Length);
    }
}
