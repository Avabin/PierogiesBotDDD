using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Guilds.Infrastructure.RabbitMq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shared.Core.Events;
using Shared.Core.Queries;

namespace Guilds.Infrastructure.Tests;
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[NonParallelizable]
[TestFixture]
[Category("Integration")]
public class RabbitMqMessageBrokerIntegrationTests
{
    private static readonly RabbitMqSettings RabbitMqSettings = new RabbitMqSettings
    {
        Enabled    = true,
        Host       = "localhost",
        Password   = "guest",
        UserName   = "guest",
        Port       = 5672,
        ClientName = "rpc_test"
    };

    [Test]
    public async Task When_MessageIsSentToQueue_ObservableIsNotified()
    {
        // Arrange
        var sut = new RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings),
                                            NullLoggerFactory.Instance);

        // Act
        var       resultObservable = sut.GetObservableForQueue<QueryInt>("test_queue");
        var       tcs              = new TaskCompletionSource<QueryInt?>();
        using var sub              = resultObservable.Subscribe(x => tcs.SetResult(x.Data as QueryInt));
        var       expected         = new QueryInt();
        await sut.SendToQueueAsync(expected, "test_queue");

        await Task.Delay(100);

        var actual = await tcs.Task;

        // Assert
        actual.Should().Be(expected);
    }

    [TestCase("*")]
    [TestCase("logs.*")]
    [TestCase("logs.debug")]
    public async Task When_MessageIsSentToTopic_ObservableIsNotified(string routingKey)
    {
        // Arrange

        var sut = new RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings),
                                            NullLoggerFactory.Instance);

        // Act
        var       resultObservable = sut.GetObservableForTopic<QueryInt>("queries", routingKey);
        var       tcs              = new TaskCompletionSource<QueryInt?>();
        using var sub              = resultObservable.Subscribe(x => tcs.SetResult(x.Data as QueryInt));
        var       expected         = new QueryInt();
        await sut.SendToTopicAsync(expected, "queries", routingKey);


        var actual = await tcs.Task;

        // Assert
        actual.Should().Be(expected);
    }

    [Test]
    public async Task RpcCallTest()
    {
        // Arrange
        // Create RPC server instance
        var server = new RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings),
                                               NullLoggerFactory.Instance);

        // Create RPC client instance
        var client =
            new
                RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings with { ClientName = "rpc_test2" }),
                                      NullLoggerFactory.Instance);


        // Server observe for RPC requests
        var queryObservable = server.GetObservableForQueue<QueryInt>("rpc_queue");

        var expected      = new QueryIntResult(42);
        var callbackQuery = client.RpcCallbackQueueName;

        // Handle query and send response
        using var sub = queryObservable
                       .Select((x) => (x.CorrelationId, result: expected))
                       .Subscribe(x => server.SendToQueueAsync(x.result, callbackQuery, x.CorrelationId));

        // Act
        var query  = new QueryInt();
        var actual = await client.SendAndReceiveAsync<QueryInt, QueryIntResult>(query);

        // Assert
        actual.Should().Be(expected);
    }

    [Test]
    public async Task When_NotificationIsSent_ThenClientsReceive()
    {
        // Arrange
        var notifier = new RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings),
                                                 NullLoggerFactory.Instance);
        var client1 =
            new
                RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings with { ClientName = "rpc_test3" }),
                                      NullLoggerFactory.Instance);
        var client2 =
            new
                RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings with { ClientName = "rpc_test4" }),
                                      NullLoggerFactory.Instance);
        var client3 =
            new
                RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings with { ClientName = "rpc_test5" }),
                                      NullLoggerFactory.Instance);

        var tcs1 = new TaskCompletionSource<TestNotification?>();
        var tcs2 = new TaskCompletionSource<TestNotification?>();
        var tcs3 = new TaskCompletionSource<TestNotification?>();

        using var sub1 = client1.GetNotificationsObservable<TestNotification>()
                                .Subscribe(x => tcs1.SetResult(x.Data as TestNotification));
        using var sub2 = client2.GetNotificationsObservable<TestNotification>()
                                .Subscribe(x => tcs2.SetResult(x.Data as TestNotification));
        using var sub3 = client3.GetNotificationsObservable<TestNotification>()
                                .Subscribe(x => tcs3.SetResult(x.Data as TestNotification));

        // Act
        var expected = new TestNotification("Hello there");
        await notifier.NotifyAsync(expected);

        TestNotification?[] results = await Task.WhenAll(tcs1.Task, tcs2.Task, tcs3.Task);

        // Assert
        results.Should().AllSatisfy(x => x.Should().Be(expected));
    }

    [Test]
    public async Task When_NotificationIsSentWithRoutingKey_ThenOnlyClientsWithCorrectKeysReceive()
    {
        // Arrange
        var notifier = new RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings),
                                                 NullLoggerFactory.Instance);
        var client1 =
            new
                RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings with { ClientName = "rpc_test3" }),
                                      NullLoggerFactory.Instance);
        var client2 =
            new
                RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings with { ClientName = "rpc_test4" }),
                                      NullLoggerFactory.Instance);
        var client3 =
            new
                RabbitMqMessageBroker(new OptionsWrapper<RabbitMqSettings>(RabbitMqSettings with { ClientName = "rpc_test5" }),
                                      NullLoggerFactory.Instance);

        var results1 = new List<TestNotification?>();
        var results2 = new List<TestNotification?>();
        var results3 = new List<TestNotification?>();


        using var sub1 = client1.GetNotificationsObservable<TestNotification>("logs")
                                .Subscribe(x => results1.Add(x.Data as TestNotification));
        using var sub2 = client2.GetNotificationsObservable<TestNotification>("users")
                                .Subscribe(x => results2.Add(x.Data as TestNotification));
        using var sub3 = client3.GetNotificationsObservable<TestNotification>()
                                .Subscribe(x => results3.Add(x.Data as TestNotification));

        // Act
        var expected = new TestNotification("Hello there");
        await notifier.NotifyAsync(expected, "logs");
        await notifier.NotifyAsync(expected, "logs");
        await notifier.NotifyAsync(expected, "users");
        await notifier.NotifyAsync(expected, "system");
        await notifier.NotifyAsync(expected);

        await Task.Delay(100);

        // Assert
        results1.Should().AllSatisfy(x => x.Should().Be(expected));
        results2.Should().AllSatisfy(x => x.Should().Be(expected));
        results3.Should().AllSatisfy(x => x.Should().Be(expected));

        results1.Should().HaveCount(2);
        results2.Should().HaveCount(1);
        results3.Should().HaveCount(1);
    }
}

public record QueryIntResult(int Value) : Event
{
}

public record QueryInt : Query
{
}