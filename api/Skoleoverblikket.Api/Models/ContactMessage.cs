using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

public enum SenderType { Parent, Staff }

public class ContactMessage : ITenantScoped, IEntityTypeConfiguration<ContactMessage>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid ThreadId { get; set; }
	public ContactThread? Thread { get; set; }
	public SenderType SenderType { get; set; }
	public Guid SenderId { get; set; }

	[MaxLength(4000)]
	public required string Body { get; set; }

	public DateTimeOffset SentAt { get; set; }
	public DateTimeOffset? ReadAt { get; set; }

	public void Configure(EntityTypeBuilder<ContactMessage> builder)
	{
		builder.HasOne(m => m.Thread)
			.WithMany(t => t.Messages)
			.HasForeignKey(m => m.ThreadId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
