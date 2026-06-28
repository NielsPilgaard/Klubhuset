using Skoleoverblikket.Api.Services;

namespace Skoleoverblikket.Api.IntegrationTests.Infrastructure;

public sealed class NoOpNotificationService : INotificationService
{
	public Task CreateAsync(Guid recipientId, RecipientType recipientType, NotificationType type,
		Guid? referenceId, string body, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	public Task CreateBatchAsync(IEnumerable<NotificationRequest> requests, CancellationToken cancellationToken)
		=> Task.CompletedTask;
}
