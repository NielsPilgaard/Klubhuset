using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Models;

namespace Skoleplanen.Api.Data;

public static class DbSeeder
{
    // Well-known IDs — stable across environments so seeding is idempotent.
    public static readonly Guid SeedSchoolId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SeedStaffId  = new("22222222-2222-2222-2222-222222222222");

    /// <summary>Keycloak subject for the seed admin user (matches the fixed id in Skoleplanen-realm.json).</summary>
    private const string SeedAdminKeycloakSubject = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    public static async Task SeedAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        await SeedSchoolAsync(db);
        await SeedStaffAsync(db);
    }

    private static async Task SeedSchoolAsync(AppDbContext db)
    {
        var exists = await db.Schools.IgnoreQueryFilters().AnyAsync(s => s.Id == SeedSchoolId);
        if (exists)
        {
            return;
        }

        db.Schools.Add(new School
        {
            Id           = SeedSchoolId,
            Name         = "Debugskolen",
            ContactEmail = "admin@debugskolen.dk",
            CreatedAt    = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedStaffAsync(AppDbContext db)
    {
        var exists = await db.Staff.IgnoreQueryFilters().AnyAsync(s => s.Id == SeedStaffId);
        if (exists)
        {
            return;
        }

        db.Staff.Add(new Staff
        {
            Id               = SeedStaffId,
            TenantId         = SeedSchoolId,
            Name             = "Debug Admin",
            Email            = "admin@debugskolen.dk",
            Role             = StaffRole.Teacher,
            KeycloakSubject  = SeedAdminKeycloakSubject,
        });

        await db.SaveChangesAsync();
    }
}
