using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace HellLetLooseSeedingClient.Tray;

public class SystemTrayService(ILogger<SystemTrayService> logger)
{
    private const string trayIconText = "Hell Let Loose Seeding Client";
    private const string disableAutostartText = "Disable autostart";
    private const string enableAutostartText = "Enable autostart";
    private const string exitText = "Exit";
    private const string settingsText = "Settings";

    private const string connectedText = "🟢 Connected";
    private const string disconnectedText = "❌ Disconnected";

    private const string iconEmbeddedResourcePath = "HellLetLooseSeedingClient.Assets.Icon.ico";

    private NotifyIcon? trayIcon;
    private ToolStripItem? statusitem;
    private ToolStripMenuItem? enableItem;
    private ToolStripMenuItem? disableItem;
    private ToolStripMenuItem? exitItem;
    private ToolStripMenuItem? settingsItem;

    private Thread? applicationThread;
    private ApplicationContext? context;
    private SynchronizationContext? uiContext;

    private TaskCompletionSource? createSystemTrayIconCompletionSource;

    public Task CreateSystemTrayIcon()
    {
        if (trayIcon != null)
            return Task.CompletedTask;

        createSystemTrayIconCompletionSource = new();

        applicationThread = new Thread(new ThreadStart(CreateAndRunTrayIcon));
        applicationThread.SetApartmentState(ApartmentState.STA);
        applicationThread.Start();

        return createSystemTrayIconCompletionSource.Task;
    }

    [STAThread]
    private void CreateAndRunTrayIcon()
    {
        logger.LogInformation("Creating tray icon");

        var menu = new ContextMenuStrip();

        statusitem = new ToolStripLabel(disconnectedText);
        menu.Items.Add(statusitem);

        menu.Items.Add(new ToolStripSeparator());

        enableItem = new ToolStripMenuItem(enableAutostartText);
        enableItem.Click += HandleEnableClick;
        menu.Items.Add(enableItem);

        disableItem = new ToolStripMenuItem(disableAutostartText);
        disableItem.Click += HandleDisableClick;
        menu.Items.Add(disableItem);

        settingsItem = new ToolStripMenuItem(settingsText);
        settingsItem.Click += HandleSettingsClick;
        menu.Items.Add(settingsItem);

        menu.Items.Add(new ToolStripSeparator());

        exitItem = new ToolStripMenuItem(exitText);
        exitItem.Click += HandleExitClick;
        menu.Items.Add(exitItem);


        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(iconEmbeddedResourcePath);
        var icon = stream != null ? new Icon(stream) : SystemIcons.Application;

        trayIcon = new NotifyIcon
        {
            Icon = icon,
            ContextMenuStrip = menu,
            Text = trayIconText,
            Visible = true,
        };


        uiContext = SynchronizationContext.Current;
        context = new ApplicationContext();

        createSystemTrayIconCompletionSource?.SetResult();

        Application.Run(context);

        logger.LogInformation("Tray icon created");
    }

    private void HandleSettingsClick(object? sender, EventArgs e)
    {
        var path = "appsettings.json";
        var current = Assembly.GetEntryAssembly()?.Location;
        var directory = Path.GetDirectoryName(current ?? string.Empty) ?? ".";
        var fullPath = Path.Combine(directory, path);

        logger.LogInformation("Attempting to open {path} in {directory}. Full path: {fullPath}", path, directory, fullPath);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            });
        } catch (Exception exception)
        {
            logger.LogError(exception, "Failed to open settings file at {fullPath}", fullPath);
        }
    }

    public void SetAutostartEnabled(bool enabled) => RunOnUiThread(() =>
    {
        disableItem?.Enabled = enabled;
        enableItem?.Enabled = !enabled;
    });

    public void DestroySystemTrayIcon() => RunOnUiThread(InnerDestroyTrayIcon);

    public void SetConnectedStatus(bool connected) => RunOnUiThread(() =>
    {
        if (statusitem == null)
            return;

        statusitem.Text = connected ? connectedText : disconnectedText;

        statusitem.ForeColor = connected ? Color.Green : Color.Red;
    });

    private void InnerDestroyTrayIcon()
    {
        context?.ExitThread();
        context?.Dispose();

        trayIcon?.Dispose();

        context = null;

        trayIcon = null;
        exitItem = null;
        disableItem = null;
        enableItem = null;

        applicationThread = null;
    }

    public void RunOnUiThread(Action action)
    {
        if (this.applicationThread == null)
            throw new Exception("Cannot run on UI thread when there is no UI thread.");

        if (Thread.CurrentThread == applicationThread)
        {
            action();
            return;
        }

        uiContext?.Post(_ => action(), null);
    }

    private void HandleEnableClick(object? sender, EventArgs e)
    {
        EnableRequested?.Invoke(sender, EventArgs.Empty);
    }

    private void HandleDisableClick(object? sender, EventArgs e)
    {
        DisableRequested?.Invoke(sender, EventArgs.Empty);
    }

    private void HandleExitClick(object? sender, EventArgs e)
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    public delegate void EnableRequestHandler(object? sender, EventArgs e);
    public delegate void DisableRequestHandler(object? sender, EventArgs e);
    public delegate void ExitRequestHandler(object? sender, EventArgs e);

    public event EnableRequestHandler? EnableRequested;
    public event DisableRequestHandler? DisableRequested;
    public event ExitRequestHandler? ExitRequested;
}
