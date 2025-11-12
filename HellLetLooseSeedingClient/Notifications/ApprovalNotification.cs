using System.Runtime.Versioning;

namespace HellLetLooseSeedingClient.Notifications;

[SupportedOSPlatform("windows")]
public class ApprovalNotification
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid ConfirmId { get; } = Guid.NewGuid();
    public Guid DeclineId { get; } = Guid.NewGuid();

    public Task<ApprovalResult> ApprovalTask => completionSource.Task;


    private readonly TaskCompletionSource<ApprovalResult> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Complete(bool approved)
    {
        completionSource.TrySetResult(approved ? ApprovalResult.Approved : ApprovalResult.Declined);
    }

    public void TimeOut()
    {
        completionSource.TrySetResult(ApprovalResult.TimedOut);
    }
}
