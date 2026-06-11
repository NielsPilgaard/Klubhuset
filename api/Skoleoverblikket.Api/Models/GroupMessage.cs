using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

public enum BroadcastAudience
{
	AllParents,
	ClassParents,
	SfoParents,
	AllStaff,
	StaffByRole,
}

public sealed class GroupMessage : ITenantScoped, IEntityTypeConfiguration<GroupMessage>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	public Guid? SenderStaffId { get; set; }
	public Guid? SenderParentId { get; set; }

	[MaxLength(200)]
	public required string SenderName { get; set; }

	public BroadcastAudience Audience { get; set; }
	public Guid? ClassId { get; set; }
	public StaffRole? StaffRole { get; set; }

	[MaxLength(200)]
	public required string Subject { get; set; }

	[MaxLength(10000)]
	public required string Body { get; set; }

	public int RecipientCount { get; set; }

	public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;

	public void Configure(EntityTypeBuilder<GroupMessage> builder)
	{
		builder.ToTable(t => t.HasCheckConstraint(
			"CK_GroupMessages_Sender",
			"\"SenderStaffId\" IS NOT NULL OR \"SenderParentId\" IS NOT NULL"));
	}
}
