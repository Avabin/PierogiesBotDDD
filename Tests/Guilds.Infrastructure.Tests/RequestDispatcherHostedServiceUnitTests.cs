using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Guilds.Infrastructure.EventDispatcher;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.Extensions;
using NUnit.Framework;
using Shared.Core.Commands;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.Queries;

namespace Guilds.Infrastructure.Tests;
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class RequestDispatcherHostedServiceUnitTests
{
    private readonly IMessageBroker         _messageBroker;
    private readonly ICommandHandlerFactory _commandHandlerFactory;
    private readonly IQueryHandlerFactory   _queryHandlerFactory;

    private EventDispatcherHostedService Create() =>
        new(_messageBroker, _commandHandlerFactory, _queryHandlerFactory,
            NullLogger<EventDispatcherHostedService>.Instance);

    public RequestDispatcherHostedServiceUnitTests()
    {
        
        _messageBroker         = Substitute.For<IMessageBroker>();
        _commandHandlerFactory = Substitute.For<ICommandHandlerFactory>();
        _queryHandlerFactory   = Substitute.For<IQueryHandlerFactory>();
    }

    [Test]
    public async Task When_StartAsync_And_CommandArrived_Then_CommandHandler_Is_Invoked()
    {
        // Arrange
        var command         = new TestCommand();
        var commandHandler  = Substitute.For<ICommandHandler>();
        var commandsSubject = new Subject<Delivery>();
        _commandHandlerFactory.GetHandler(Arg.Any<Type>()).Configure().Returns(commandHandler);
        _messageBroker.GetObservableForQueue<IEvent>(Arg.Any<string>()).Returns(commandsSubject.AsObservable());
        commandHandler.HandleAsync(Arg.Is<ICommand>(command)).Returns(Task.CompletedTask);

        var sut = Create();

        // Act
        await sut.StartAsync(new System.Threading.CancellationToken());
        commandsSubject.OnNext(Delivery.Of(command, Guid.NewGuid(), DateTimeOffset.Now));

        // Assert
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        await commandHandler.Received().HandleAsync(Arg.Is<ICommand>(command));
    }

    [Test]
    public async Task When_StartAsync_And_QueryArrived_Then_QueryHandler_Is_Invoked_And_Result_IsSent()
    {
        // Arrange
        var query          = new TestQuery();
        var queryHandler   = Substitute.For<IQueryHandler<IQuery>>();
        var queriesSubject = new Subject<Delivery>();
        var result         = new TestQueryResult();

        var correlationId = Guid.NewGuid();
        var replyTo       = "reply-callback";

        _queryHandlerFactory.GetHandler(Arg.Any<Type>()).Configure().Returns(queryHandler);
        _messageBroker.GetObservableForQueue<IEvent>(Arg.Any<string>()).Returns(queriesSubject.AsObservable());
        _messageBroker.SendToQueueAsync(Arg.Is(result), Arg.Is(replyTo), Arg.Is(correlationId))
                      .Returns(ValueTask.CompletedTask);

        queryHandler.HandleAsync(Arg.Is(query)).Returns(Task.FromResult((IEvent)result));

        var sut = Create();

        // Act
        await sut.StartAsync(CancellationToken.None);
        queriesSubject.OnNext(Delivery.Of(query, correlationId, DateTimeOffset.Now, replyTo));

        // Assert
        await sut.StopAsync(CancellationToken.None);
        await queryHandler.Received(1).HandleAsync(Arg.Is(query));
        await _messageBroker.Received(1).SendToQueueAsync(Arg.Is(result), Arg.Is(replyTo), Arg.Is(correlationId));
    }
}

public record TestQueryResult : Event
{
}

public record TestQuery : Query
{
}

public record TestCommand : Command
{
}