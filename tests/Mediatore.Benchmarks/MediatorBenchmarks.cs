using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Mediatore;
using Mediatore.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Mediatore.Benchmarks;

// ---------------------------------------------------------------------------
// No-op handler for baseline measurements
// ---------------------------------------------------------------------------

internal sealed record PingRequest : IRequest<int>;

internal sealed class PingHandler : IRequestHandler<PingRequest, int>
{
    public Task<int> Handle(PingRequest request, CancellationToken cancellationToken)
        => Task.FromResult(0);
}

// ---------------------------------------------------------------------------
// MediatorBenchmarks — 1 million sequential Send calls, no behaviors
// ---------------------------------------------------------------------------

[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
public class MediatorBenchmarks
{
    private ServiceProvider _sp = null!;
    private IServiceScope _scope = null!;
    private IMediator _mediator = null!;
    private readonly PingRequest _request = new();

    [GlobalSetup]
    public void Setup()
    {
        _sp = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<PingHandler>())
            .BuildServiceProvider();
        _scope = _sp.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
        _sp.Dispose();
    }

    [Benchmark(Baseline = true)]
    public Task<int> DirectDelegate()
    {
        Func<Task<int>> fn = () => Task.FromResult(0);
        return fn();
    }

    [Benchmark]
    public Task<int> MediatorSend()
        => _mediator.Send(_request, CancellationToken.None);
}
