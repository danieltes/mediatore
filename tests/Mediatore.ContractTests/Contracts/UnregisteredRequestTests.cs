using Mediatore;

namespace Mediatore.ContractTests.Contracts;

// ---------------------------------------------------------------------------
// Stubs — no handler registered for UnregisteredQuery
// ---------------------------------------------------------------------------

file sealed record UnregisteredQuery(int Id) : IRequest<int>;

// ---------------------------------------------------------------------------
// REQ-02: Send for an unregistered request type raises HandlerNotFoundException
//         with correct RequestType property and message.
// ---------------------------------------------------------------------------

public sealed class UnregisteredRequestTests
{
    [Fact]
    public async Task Send_UnregisteredRequestType_ThrowsHandlerNotFoundException()
    {
        using var sp = new ServiceCollection()
            .AddMediator()
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => mediator.Send(new UnregisteredQuery(1), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Send_UnregisteredRequest_ExceptionHasCorrectRequestType()
    {
        using var sp = new ServiceCollection()
            .AddMediator()
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => mediator.Send(new UnregisteredQuery(1), TestContext.Current.CancellationToken));

        ex.RequestType.Should().Be(typeof(UnregisteredQuery));
    }

    [Fact]
    public async Task Send_UnregisteredRequest_ExceptionMessageContainsTypeName()
    {
        using var sp = new ServiceCollection()
            .AddMediator()
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => mediator.Send(new UnregisteredQuery(1), TestContext.Current.CancellationToken));

        ex.Message.Should().Contain("UnregisteredQuery");
    }

    [Fact]
    public async Task Send_NullRequest_ThrowsArgumentNullException()
    {
        using var sp = new ServiceCollection()
            .AddMediator()
            .BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => mediator.Send<int>(null!, TestContext.Current.CancellationToken));
    }
}
