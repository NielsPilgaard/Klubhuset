using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

public enum InvitationStatus
{
	Pending,
	Accepted,
	Expired,
}

public sealed class StaffInvitation : ITenantScoped, IEntityTypeConfiguration<StaffInvitation>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	public Guid StaffId { get; set; }
	public Staff Staff { get; set; } = null!;

	[StringLength(500)]
	public required string Email { get; set; }

	/// <summary>Cryptographically random token included in the invitation link.</summary>
	[StringLength(128)]
	public required string Token { get; set; }

	public DateTimeOffset ExpiresAt { get; set; }
	public DateTimeOffset? AcceptedAt { get; set; }
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>Optimistic concurrency control token for preventing concurrent acceptance attempts.</summary>
	[Timestamp]
	public byte[]? RowVersion { get; set; }

	public InvitationStatus Status =>
		AcceptedAt.HasValue ? InvitationStatus.Accepted
		: DateTimeOffset.UtcNow > ExpiresAt ? InvitationStatus.Expired
		: InvitationStatus.Pending;

	public void Configure(EntityTypeBuilder<StaffInvitation> builder)
	{
		builder.HasIndex(i => i.Token).IsUnique();
		builder.HasOne(i => i.Staff)
			   .WithMany()
			   .HasForeignKey(i => i.StaffId)
			   .OnDelete(DeleteBehavior.Cascade);
		builder.Ignore(i => i.Status);
	}
}
