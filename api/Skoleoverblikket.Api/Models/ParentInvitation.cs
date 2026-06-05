using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Models;

public sealed class ParentInvitation : ITenantScoped, IEntityTypeConfiguration<ParentInvitation>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	public Guid ParentId { get; set; }
	public Parent Parent { get; set; } = null!;

	[StringLength(500)]
	public required string Email { get; set; }

	[StringLength(128)]
	public required string Token { get; set; }

	public DateTimeOffset ExpiresAt { get; set; }
	public DateTimeOffset? AcceptedAt { get; set; }
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	[Timestamp]
	public byte[]? RowVersion { get; set; }

	public InvitationStatus Status =>
		AcceptedAt.HasValue ? InvitationStatus.Accepted
		: DateTimeOffset.UtcNow > ExpiresAt ? InvitationStatus.Expired
		: InvitationStatus.Pending;

	public void Configure(EntityTypeBuilder<ParentInvitation> builder)
	{
		builder.HasIndex(i => i.Token).IsUnique();
		builder.HasOne(i => i.Parent)
			   .WithMany()
			   .HasForeignKey(i => i.ParentId)
			   .OnDelete(DeleteBehavior.Cascade);
		builder.Ignore(i => i.Status);
	}
}
