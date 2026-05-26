namespace Skoleoverblikket.Api.Services;

public sealed class NullNotificationService : INotificationService
{
	public Task CreateAsync(
		Guid recipientId,
		RecipientType recipientType,
		NotificationType type,
		Guid? referenceId,
		string body,
		CancellationToken ct) => Task.CompletedTask;
}
