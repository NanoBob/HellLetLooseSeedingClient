using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Runtime.Versioning;

namespace HellLetLooseSeedingClient.Notifications;

[SupportedOSPlatform("windows")]
public class AppNotificationService(IOptions<NotificationOptions> options)
{
    private const string actionArgumentName = "HellletLoose.Seeding.action";
    private const string approveActionArgumentValue = "HellletLoose.Seeding.approve";
    private const string rejectActionArgumentValue = "HellletLoose.Seeding.reject";

    private TaskCompletionSource<bool>? approvalCompletionSource;

    public void ShowInformationalToast(string title, string message)
    {
        if (!options.Value.ShowInformationalNotifications)
            return;

        new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .AddAttributionText("Hell Let Loose Seeding Client")
            .SetToastScenario(ToastScenario.Default)
            .SetToastDuration(ToastDuration.Short)
            .Show();
    }

    public Task<bool> RequestApprovalAsync(string title, string message, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .AddButton("Start seeding", ToastActivationType.Background, arguments: $"{actionArgumentName}={approveActionArgumentValue}")
            .AddButton("Decline", ToastActivationType.Background, arguments: $"{actionArgumentName}={rejectActionArgumentValue}")
            .AddAttributionText("Hell Let Loose Seeding Client")
            .SetToastScenario(ToastScenario.Default)
            .Show();

        ToastNotificationManagerCompat.OnActivated += HandleToastActivation;

        approvalCompletionSource = new TaskCompletionSource<bool>();

        if (timeout.HasValue)
        {
            Task.Delay(timeout.Value, cancellationToken)
                .ContinueWith(t =>
                {
                    if (!approvalCompletionSource!.Task.IsCompleted)
                    {
                        approvalCompletionSource.SetResult(true);
                        ToastNotificationManagerCompat.OnActivated -= HandleToastActivation;
                    }
                }, cancellationToken);
        }

        return approvalCompletionSource.Task;
    }

    private void HandleToastActivation(ToastNotificationActivatedEventArgsCompat e)
    {
        if (approvalCompletionSource?.Task.IsCompleted == true)
            return;

        var args = ToastArguments.Parse(e.Argument);
        var userInput = e.UserInput;

        if (args.TryGetValue(actionArgumentName, out var action))
            approvalCompletionSource?.SetResult(action == approveActionArgumentValue);
        else
            approvalCompletionSource?.SetResult(false);

        ToastNotificationManagerCompat.OnActivated -= HandleToastActivation;
    }
}
