using System.Reflection;
using Guilds.Infrastructure.EventDispatcher;
using Guilds.Infrastructure.RabbitMq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shared.Core.Commands;
using Shared.Core.MessageBroker;
using Shared.Core.Queries;

namespace Guilds.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IHostBuilder AddSerilog(this IHostBuilder host, string serviceName, string? seqUrl = null)
    {
        seqUrl ??= Environment.GetEnvironmentVariable("SEQ_URL");
        host.UseSerilog((context, c) =>
        {
            c.MinimumLevel.Verbose()
             .Destructure.ToMaximumDepth(10)
             .Enrich.FromLogContext()
             .Enrich.WithProperty("ServiceName", serviceName)
             .WriteTo.Console();
            if (seqUrl is not null or "")
                c.WriteTo.Seq(seqUrl);
        });

        return host;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RabbitMqSettings>(config.GetSection("RabbitMQ"));
        services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();
        services.AddSingleton<ICommandHandlerFactory, CommandHandlerFactory>();
        services.AddSingleton<IQueryHandlerFactory, QueryHandlerFactory>();
        return services;
    }

    // Add event handlers
    public static IServiceCollection AddEventHandlers(this IServiceCollection services, Assembly assembly) =>
        services.AddHostedService<EventDispatcherHostedService>()
                .AddCommandHandlers(assembly)
                .AddQueryHandlers(assembly);

    public static IServiceCollection AddCommandHandlers(this IServiceCollection services, Assembly assembly)
    {
        var commandHandlers = assembly.GetExportedTypes().Where(t => t.IsClass && t.GetInterfaces()
                                                                        .Any(i => i.IsGenericType &&
                                                                                 i.GetGenericTypeDefinition() ==
                                                                                 typeof(ICommandHandler<>)))
                                      .ToList();

        foreach (var commandHandler in commandHandlers)
        {
            var interfaceType = commandHandler.GetInterfaces()
                                              .First(i => i.IsGenericType && i.GetGenericTypeDefinition() ==
                                                          typeof(ICommandHandler<>));
            services.AddTransient(interfaceType, commandHandler);
        }

        return services;
    }

    public static IServiceCollection AddQueryHandlers(this IServiceCollection services, Assembly assembly)
    {
        var queryHandlers = assembly.GetExportedTypes().Where(t => t.IsClass && t.GetInterfaces()
                                                                      .Any(i => i.IsGenericType &&
                                                                               i.GetGenericTypeDefinition() ==
                                                                               typeof(IQueryHandler<>)))
                                    .ToList();

        foreach (var queryHandler in queryHandlers)
        {
            var interfaceType = queryHandler.GetInterfaces()
                                            .First(i => i.IsGenericType &&
                                                        i.GetGenericTypeDefinition() == typeof(IQueryHandler<>));
            services.AddTransient(interfaceType, queryHandler);
        }

        return services;
    }
}