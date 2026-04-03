using Microsoft.Extensions.DependencyInjection;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Domain;

namespace Skoleplanen.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Arranges test data directly via DbContext (not via the API) to keep
/// test setup fast and independent of the endpoints under test.
/// Only used for prerequisite entities (tenant root, time slots, etc.) —
/// test assertions always go through the HTTP API.
/// </summary>
public static class TestDataBuilder
{
    public static async Task<School> CreateSchoolAsync(IServiceProvider services, Guid tenantId, string name = "Teststole")
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var school = new School
        {
            Id = tenantId,
            Name = name,
            Slug = $"test-{tenantId:N}",
            ContactEmail = "test@skole.dk",
        };
        db.Schools.Add(school);
        await db.SaveChangesAsync();
        return school;
    }

    public static async Task<Staff> CreateStaffAsync(IServiceProvider services, Guid tenantId, string name = "Anders Lærer", StaffRole role = StaffRole.Teacher)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var staff = new Staff
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Role = role,
        };
        db.Staff.Add(staff);
        await db.SaveChangesAsync();
        return staff;
    }

    public static async Task<Course> CreateCourseAsync(IServiceProvider services, Guid tenantId, string name = "Dansk")
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return course;
    }

    public static async Task<TimeSlot> CreateTimeSlotAsync(
        IServiceProvider services, Guid tenantId,
        TimeOnly start, TimeOnly end, int sortOrder = 1)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var slot = new TimeSlot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StartTime = start,
            EndTime = end,
            SortOrder = sortOrder,
        };
        db.TimeSlots.Add(slot);
        await db.SaveChangesAsync();
        return slot;
    }

    public static async Task<(Class klass, Schema schema)> CreateClassWithSchemaAsync(
        IServiceProvider services, Guid tenantId,
        string className = "2.b", string schemaName = "Skema 2024")
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var klass = new Class
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = className,
        };
        db.Classes.Add(klass);
        await db.SaveChangesAsync();

        var schema = new Schema
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClassId = klass.Id,
            Name = schemaName,
            IsActive = true,
        };
        db.Schemas.Add(schema);
        await db.SaveChangesAsync();

        return (klass, schema);
    }
}
