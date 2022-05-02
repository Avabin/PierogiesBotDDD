// See https://aka.ms/new-console-template for more information

using Guilds.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
          .ConfigureAppConfiguration(c => c.AddEnvironmentVariables("PB_"))
    .ConfigureServices((hostContext, services) =>
           {
               services.AddInfrastructure(hostContext.Configuration);
           })
    .Build()
    .RunAsync();