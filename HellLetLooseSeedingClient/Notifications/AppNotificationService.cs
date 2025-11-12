using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Collections.Concurrent;
using System.Runtime.Versioning;

namespace HellLetLooseSeedingClient.Notifications;

[SupportedOSPlatform("windows")]
public class AppNotificationService
{
    private const string idArgumentName = "HellletLoose.Seeding.id";
    private const string actionArgumentName = "HellletLoose.Seeding.action";

    private readonly IOptions<NotificationOptions> options;

    private readonly ConcurrentDictionary<Guid, ApprovalNotification> pendingNotifications = new();

    public AppNotificationService(IOptions<NotificationOptions> options)
    {
        this.options = options;

        ToastNotificationManagerCompat.OnActivated += HandleToastActivation;
    }

    public static void ShowErrorToast(string title, string message)
    {
        new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .AddAttributionText("Hell Let Loose Seeding Client")
            .SetToastScenario(ToastScenario.Default)
            .SetToastDuration(ToastDuration.Short)
            .Show();
    }

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

    public Task<ApprovalResult> RequestApprovalAsync(string title, string message, string yesText, string noText, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var notification = new ApprovalNotification();
        pendingNotifications.TryAdd(notification.Id, notification);

        new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .AddArgument(idArgumentName, $"{notification.Id}")
            .AddButton(yesText, ToastActivationType.Background, arguments: $"{idArgumentName}={notification.Id};{actionArgumentName}={notification.ConfirmId}")
            .AddButton(noText, ToastActivationType.Background, arguments: $"{idArgumentName}={notification.Id};{actionArgumentName}={notification.DeclineId}")
            .AddAttributionText("Hell Let Loose Seeding Client")
            .SetToastScenario(ToastScenario.Default)
            .Show();

        if (timeout.HasValue)
        {
            Task.Delay(timeout.Value, cancellationToken)
                .ContinueWith(t =>
                {
                    if (!notification.ApprovalTask.IsCompleted)
                    {
                        notification.TimeOut();

                        pendingNotifications.TryRemove(notification.Id, out var _);
                    }
                }, cancellationToken);
        }

        return notification.ApprovalTask;
    }

    private void HandleToastActivation(ToastNotificationActivatedEventArgsCompat e)
    {
        var args = ToastArguments.Parse(e.Argument);

        if (
            args.TryGetValue(idArgumentName, out var id) && Guid.TryParse(id, out var idGuid) &&
            args.TryGetValue(actionArgumentName, out var action) && Guid.TryParse(action, out var actionGuid) &&
            pendingNotifications.TryGetValue(idGuid, out var notification)
        )
        {
            if (actionGuid == notification.ConfirmId)
                notification.Complete(true);
            else if (actionGuid == notification.DeclineId)
                notification.Complete(false);
            else
                notification.Complete(false);

            pendingNotifications.TryRemove(idGuid, out var _);
        }
    }
}
