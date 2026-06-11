using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Services;

namespace Skoleoverblikket.Api.Models;

public class Message : ITenantScoped, IEntityTypeConfiguration<Message>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid SenderId { get; set; }
	public RecipientType SenderType { get; set; }
	public Guid RecipientId { get; set; }
	public RecipientType RecipientType { get; set; }
	[MaxLength(200)]
	public required string Subject { get; set; }
	[MaxLength(10000)]
	public required string Body { get; set; }
	public DateTimeOffset SentAt { get; set; }
	public DateTimeOffset? ReadAt { get; set; }
	public Guid? GroupMessageId { get; set; }

	public void Configure(EntityTypeBuilder<Message> builder)
	{
		builder.HasIndex(m => m.GroupMessageId)
			.HasFilter("\"GroupMessageId\" IS NOT NULL")
			.HasDatabaseName("IX_Messages_GroupMessageId");
	}
}
