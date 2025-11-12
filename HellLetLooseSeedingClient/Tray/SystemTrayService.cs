using System.Reflection;

namespace HellLetLooseSeedingClient.Tray;

public class SystemTrayService
{
    private const string trayIconText = "Hell Let Loose Seeding Client";
    private const string disableAutostartText = "Disable autostart";
    private const string enableAutostartText = "Enable autostart";
    private const string exitText = "Exit";

    private const string iconEmbeddedResourcePath = "HellLetLooseSeedingClient.Assets.Icon.ico";

    private NotifyIcon? trayIcon;
    private ToolStripMenuItem? enableItem;
    private ToolStripMenuItem? disableItem;
    private ToolStripMenuItem? exitItem;

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
        var menu = new ContextMenuStrip();

        enableItem = new ToolStripMenuItem(enableAutostartText);
        enableItem.Click += HandleEnableClick;
        menu.Items.Add(enableItem);

        disableItem = new ToolStripMenuItem(disableAutostartText);
        disableItem.Click += HandleDisableClick;
        menu.Items.Add(disableItem);

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
    }

    public void SetAutostartEnabled(bool enabled) => RunOnUiThread(() =>
    {
        disableItem?.Enabled = enabled;
        enableItem?.Enabled = !enabled;
    });

    public void DestroySystemTrayIcon() => RunOnUiThread(InnerDestroyTrayIcon);

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
