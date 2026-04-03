using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Domain;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
    : DbContext(options)
{
    public DbSet<School> Schools => Set<School>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<TimeSlotTemplate> TimeSlotTemplates => Set<TimeSlotTemplate>();
    public DbSet<TimeSlotTemplateBreak> TimeSlotTemplateBreaks => Set<TimeSlotTemplateBreak>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Schema> Schemas => Set<Schema>();
    public DbSet<SchemaSlot> SchemaSlots => Set<SchemaSlot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<School>(e =>
        {
            e.HasIndex(s => s.Slug).IsUnique();
        });

        // SchemaSlot has two FK to Staff (Teacher and Aide) — configure explicitly
        modelBuilder.Entity<SchemaSlot>(e =>
        {
            e.HasOne(s => s.Teacher)
             .WithMany()
             .HasForeignKey(s => s.TeacherId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Aide)
             .WithMany()
             .HasForeignKey(s => s.AideId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // All tenant-scoped entities must implement ITenantScoped.
        // The global query filter below ensures every query is automatically
        // filtered to the current tenant — never bypass this.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, [modelBuilder]);
            }
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => e.TenantId == tenantContext.TenantId);
    }
}
