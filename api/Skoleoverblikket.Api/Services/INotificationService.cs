namespace Skoleoverblikket.Api.Services;

public enum RecipientType { Parent, Staff, Board }
public enum NotificationType { NewMessage, NewContactMessage, WeekPlanChanged, AbsenceConfirmed, AbsenceDismissed, VacationRegistrationOpened, GroupMessage }

public sealed record NotificationRequest(
	Guid RecipientId,
	RecipientType RecipientType,
	NotificationType Type,
	Guid? ReferenceId,
	string Body);

public interface INotificationService
{
	Task CreateAsync(
		Guid recipientId,
		RecipientType recipientType,
		NotificationType type,
		Guid? referenceId,
		string body,
		CancellationToken cancellationToken);

	Task CreateBatchAsync(
		IEnumerable<NotificationRequest> requests,
		CancellationToken cancellationToken);
}
