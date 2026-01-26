using HellLetLooseSeedingClient;
using HellLetLooseSeedingClient.Game;
using HellLetLooseSeedingClient.InputListeners;
using HellLetLooseSeedingClient.Notifications;
using HellLetLooseSeedingClient.Tray;
using HellLetLooseSeedingClient.Websockets;
//using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("This application only runs on Windows.");
    return 1;
}

if (args.Contains(StartupProcessHelper.RegisterTaskArgument))
{
    StartupProcessHelper.SetupAutostart();
    return 0;
}
else if (args.Contains(StartupProcessHelper.RemoveTaskArgument))
{
    StartupProcessHelper.RemoveAutostart();
    return 0;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddCommandLine(args)
    .AddEnvironmentVariables();


var logger = new LoggerConfiguration()
    .WriteTo.File(
        "logs/log.log", 
        Serilog.Events.LogEventLevel.Information, 
        rollingInterval: RollingInterval.Day, 
        fileSizeLimitBytes: 5 * 1024 * 1024, 
        retainedFileCountLimit: 7, 
        rollOnFileSizeLimit: true
    )
    .CreateLogger();

builder.Logging.AddSerilog(logger);
builder.Logging.AddDebug();
builder.Services.AddLogging();

builder.Services.AddWindowsService(x =>
{
    x.ServiceName = "HellLetLooseSeedingClient";
});

builder.Services
    .Configure<LaunchOptions>(builder.Configuration.GetSection("launch"))
    .AddSingleton<GameLauncher>();

builder.Services
    .Configure<NotificationOptions>(builder.Configuration.GetSection("notifications"))
    .AddSingleton<AppNotificationService>();

builder.Services
    .Configure<SeedingOptions>(builder.Configuration.GetSection("seeding"))
    .AddSingleton<SeedingWebsocketClient>();

builder.Services
    .Configure<WebsocketOptions>(builder.Configuration.GetSection("Websocket"))
    .AddHostedService<WebsocketHostedService>();

builder.Services
    .AddSingleton<BackgroundInputListener>();

builder.Services.AddSingleton<SystemTrayService>();

var host = builder.Build();

try
{
    await host.RunAsync();
} catch (Exception e)
{
    var exceptionLogger = host.Services.GetRequiredService<ILogger<Program>>();
    exceptionLogger.LogCritical(e, "{Message}\n{StackTrace}", e.Message, e.StackTrace);
}

return 0;
