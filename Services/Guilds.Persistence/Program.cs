// See https://aka.ms/new-console-template for more information

using Guilds.Api.Extensions;
using Guilds.Infrastructure;
using Guilds.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
          .ConfigureAppConfiguration(c => c.AddEnvironmentVariables("PB_"))
          .ConfigureServices((hostContext, services) =>
           {
               services.AddInfrastructure(hostContext.Configuration)
                       .AddGuildsMongo(hostContext.Configuration)
                       .AddEventHandlers(typeof(ViewMapExtensions).Assembly);
           })
          .AddSerilog("Guilds.Mongo")
          .Build()
          .RunAsync();