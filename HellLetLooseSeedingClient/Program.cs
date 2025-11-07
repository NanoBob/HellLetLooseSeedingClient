using HellLetLooseSeedingClient;
using HellLetLooseSeedingClient.Game;
using HellLetLooseSeedingClient.Notifications;
using HellLetLooseSeedingClient.Websockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("This application only runs on Windows.");
    return 1;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddCommandLine(args)
    .AddEnvironmentVariables();

#if RELEASE
if (!StartupProcessHelper.IsAutostartSetUp())
{
    StartupProcessHelper.SetupAutostart();
}
#endif

builder.Logging.AddDebug();
builder.Services.AddLogging();

builder.Services.AddWindowsService(x =>
{
    x.ServiceName = "HellLetLooseSeedingClient";
});

builder.Services
    .Configure<LaunchOptions>(builder.Configuration.GetSection("launch"))
    .AddSingleton<GameLauncher>();

builder.Services.AddSingleton<AppNotificationService>();

builder.Services
    .Configure<SeedingOptions>(builder.Configuration.GetSection("seeding"))
    .AddSingleton<SeedingWebsocketClient>();

builder.Services
    .Configure<WebsocketOptions>(builder.Configuration.GetSection("Websocket"))
    .AddHostedService<WebsocketService>();

var host = builder.Build();

await host.RunAsync();

return 0;
