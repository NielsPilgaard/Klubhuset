using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Models;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
	: DbContext(options)
{
	public DbSet<School> Schools => Set<School>();
	public DbSet<Staff> Staff => Set<Staff>();
	public DbSet<StaffInvitation> StaffInvitations => Set<StaffInvitation>();
	public DbSet<Class> Classes => Set<Class>();
	public DbSet<Course> Courses => Set<Course>();
	public DbSet<Room> Rooms => Set<Room>();
	public DbSet<TimeSlotTemplate> TimeSlotTemplates => Set<TimeSlotTemplate>();
	public DbSet<TimeSlotTemplateBreak> TimeSlotTemplateBreaks => Set<TimeSlotTemplateBreak>();
	public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
	public DbSet<Schema> Schemas => Set<Schema>();
	public DbSet<SchemaSlot> SchemaSlots => Set<SchemaSlot>();
	public DbSet<SchoolFile> SchoolFiles => Set<SchoolFile>();
	public DbSet<CalendarEntry> CalendarEntries => Set<CalendarEntry>();
	public DbSet<Subscription> Subscriptions => Set<Subscription>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

		// All tenant-scoped entities must implement ITenantScoped.
		// The global query filter below ensures every query is automatically
		// filtered to the current tenant — never bypass this.
		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
			{
				continue;
			}

			var method = typeof(AppDbContext)
						 .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
						 .MakeGenericMethod(entityType.ClrType);

			method.Invoke(this, [modelBuilder]);
		}
	}

	private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
		where TEntity : class, ITenantScoped
	{
		modelBuilder.Entity<TEntity>()
			.HasQueryFilter(e => e.TenantId == tenantContext.TenantId);
	}
}
