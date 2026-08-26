using Skoleoverblikket.Api.Services;

namespace Skoleoverblikket.Api.IntegrationTests.Infrastructure;

/// <summary>
/// No-op by default so tests don't need an SMTP server. Tests that need to simulate a
/// notification/email failure can flip <see cref="ShouldThrow"/> for the duration of the call —
/// it's an AsyncLocal so it flows through the in-process TestServer request pipeline without
/// affecting other tests sharing this factory.
/// </summary>
public sealed class NoOpNotificationService : INotificationService
{
	public static readonly AsyncLocal<bool> ShouldThrow = new();

	public Task CreateAsync(Guid recipientId, RecipientType recipientType, NotificationType type,
		Guid? referenceId, string body, CancellationToken cancellationToken)
	{
		if (ShouldThrow.Value)
		{
			throw new InvalidOperationException("Simulated notification/email failure for testing.");
		}

		return Task.CompletedTask;
	}

	public Task CreateBatchAsync(IEnumerable<NotificationRequest> requests, CancellationToken cancellationToken)
		=> Task.CompletedTask;
}
