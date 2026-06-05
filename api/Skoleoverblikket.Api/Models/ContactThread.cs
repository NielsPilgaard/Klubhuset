using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

public class ContactThread : ITenantScoped, IEntityTypeConfiguration<ContactThread>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid StudentId { get; set; }
	public Student? Student { get; set; }
	public ICollection<ContactMessage> Messages { get; set; } = [];
	public DateTimeOffset CreatedAt { get; set; }

	public void Configure(EntityTypeBuilder<ContactThread> builder)
	{
		builder.HasIndex(t => new { t.TenantId, t.StudentId }).IsUnique();

		builder.HasOne(t => t.Student)
			.WithMany()
			.HasForeignKey(t => t.StudentId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
