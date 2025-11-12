using SharpHook;

namespace HellLetLooseSeedingClient.InputListeners;

public class BackgroundInputListener : IDisposable
{
    private EventLoopGlobalHook? globalInputHook;
    private Task? runTask;

    private readonly Lock subscribeLock = new();

    public void Subscribe()
    {
        lock (subscribeLock)
        {
            if (globalInputHook != null)
                return;

            globalInputHook = new EventLoopGlobalHook();

            globalInputHook.MousePressed += HandleMousePress;
            globalInputHook.KeyPressed += HandleKeyPress;

            runTask = globalInputHook.RunAsync();
        }
    }

    public async Task UnsubscribeAsync()
    {
        lock (subscribeLock)
        {
            if (globalInputHook == null)
                return;

            globalInputHook.MousePressed -= HandleMousePress;
            globalInputHook.KeyPressed -= HandleKeyPress;


            globalInputHook.Stop();
            globalInputHook.Dispose();

            globalInputHook = null;
        }

        await (runTask ?? Task.CompletedTask);
    }

    private void HandleKeyPress(object? sender, KeyboardHookEventArgs e)
    {
        this.InputReceived?.Invoke(this, EventArgs.Empty);
    }

    private void HandleMousePress(object? sender, MouseHookEventArgs e)
    {
        this.InputReceived?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        this.globalInputHook?.Dispose();
        GC.SuppressFinalize(this);
    }

    public delegate void InputReceivedHandler(object? sender, EventArgs e);

    public event InputReceivedHandler? InputReceived;
}
