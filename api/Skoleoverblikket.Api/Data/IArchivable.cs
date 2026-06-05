namespace Skoleoverblikket.Api.Data;

/// <summary>
/// Entities implementing this interface support soft-delete via ArchivedAt.
/// AppDbContext automatically excludes archived rows from all queries.
/// </summary>
public interface IArchivable
{
	DateTimeOffset? ArchivedAt { get; }
}
