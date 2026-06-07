namespace Skoleoverblikket.Api.Services;

public enum RecipientType { Parent, Staff, Board }
public enum NotificationType { NewMessage, NewContactMessage, WeekPlanChanged, AbsenceConfirmed, AbsenceDismissed, VacationRegistrationOpened, GroupMessage }

public interface INotificationService
{
	Task CreateAsync(
		Guid recipientId,
		RecipientType recipientType,
		NotificationType type,
		Guid? referenceId,
		string body,
		CancellationToken cancellationToken);
}
