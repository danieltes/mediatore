using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Mediatore;
using Mediatore.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Mediatore.Benchmarks;

// ---------------------------------------------------------------------------
// Stubs
// ---------------------------------------------------------------------------

internal sealed record BehaviorRequest(int Value) : IRequest<int>;

internal sealed class BehaviorPingHandler : IRequestHandler<BehaviorRequest, int>
{
    public Task<int> Handle(BehaviorRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}

internal sealed class NoOpBehavior : IPipelineBehavior<BehaviorRequest, int>
{
    public Task<int> Handle(BehaviorRequest request, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
        => next();
}

// ---------------------------------------------------------------------------
// PipelineBenchmarks — dispatch overhead with 0, 1, and 3 behaviors
// ---------------------------------------------------------------------------

[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
public class PipelineBenchmarks
{
    private IMediator _noBehaviors = null!;
    private IMediator _oneBehavior = null!;
    private IMediator _threeBehaviors = null!;
    private readonly List<ServiceProvider> _providers = [];
    private readonly List<IServiceScope> _scopes = [];
    private readonly BehaviorRequest _request = new(42);

    [GlobalSetup]
    public void Setup()
    {
        _noBehaviors = CreateMediator(0);
        _oneBehavior = CreateMediator(1);
        _threeBehaviors = CreateMediator(3);
    }

    private IMediator CreateMediator(int behaviorCount)
    {
        var services = new ServiceCollection()
            .AddMediator(o => o.RegisterServicesFromAssemblyContaining<BehaviorPingHandler>());

        for (int i = 0; i < behaviorCount; i++)
            services.AddSingleton<IPipelineBehavior<BehaviorRequest, int>, NoOpBehavior>();

        var sp = services.BuildServiceProvider();
        _providers.Add(sp);
        var scope = sp.CreateScope();
        _scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var s in _scopes) s.Dispose();
        foreach (var p in _providers) p.Dispose();
    }

    [Benchmark(Baseline = true)]
    public Task<int> NoBehaviors() => _noBehaviors.Send(_request, CancellationToken.None);

    [Benchmark]
    public Task<int> OneBehavior() => _oneBehavior.Send(_request, CancellationToken.None);

    [Benchmark]
    public Task<int> ThreeBehaviors() => _threeBehaviors.Send(_request, CancellationToken.None);
}
