using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Skoleoverblikket.Api.Controllers;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.IntegrationTests.Infrastructure;
using Skoleoverblikket.Api.Models;

namespace Skoleoverblikket.Api.IntegrationTests;

/// <summary>
/// Integration tests for SchedulesController.
/// Covers class schedule and staff schedule endpoints, including active/inactive
/// schema filtering and 404 behaviour for unknown resources.
/// </summary>
public sealed class SchedulesTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        _factory = new ApiFactory();
        await _factory.StartAsync();
        await TestDataBuilder.CreateSchoolAsync(_factory.Services, TestTenantContext.DefaultTenantId);
        _client = _factory.CreateClient();
    }

    [After(Test)]
    public async Task TearDown()
    {
        _client.Dispose();
        await _factory.StopAsync();
        await _factory.DisposeAsync();
    }

    // ── GET /api/v1/classes/{classId}/schedule ────────────────────────────────────

    [Test]
    public async Task GetClassSchedule_WithActiveSchema_ReturnsSlots()
    {
        // Arrange — class with an active schema (StartDate = today-1m, EndDate = today+11m)
        var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(
            _factory.Services, TestTenantContext.DefaultTenantId, "1.a");

        var timeSlot = await TestDataBuilder.CreateTimeSlotAsync(
            _factory.Services, TestTenantContext.DefaultTenantId,
            new TimeOnly(8, 0), new TimeOnly(8, 45), sortOrder: 1);

        var course = await TestDataBuilder.CreateCourseAsync(
            _factory.Services, TestTenantContext.DefaultTenantId, "Matematik");

        var staff = await TestDataBuilder.CreateStaffAsync(
            _factory.Services, TestTenantContext.DefaultTenantId, "Lone Lærer");

        await TestDataBuilder.CreateSchemaSlotAsync(
            _factory.Services, TestTenantContext.DefaultTenantId,
            schema.Id, timeSlot.Id, course.Id, staff.Id,
            weekday: DayOfWeek.Monday);

        // Act
        var response = await _client.GetAsync($"/api/v1/classes/{klass.Id}/schedule");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var slots = await response.Content.ReadFromJsonAsync<List<SchedulesController.ScheduleSlotDto>>(JsonOpts);
        await Assert.That(slots).IsNotNull();
        await Assert.That(slots!.Count).IsEqualTo(1);

        var slot = slots[0];
        await Assert.That(slot.Weekday).IsEqualTo(DayOfWeek.Monday);
        await Assert.That(slot.StartTime).IsEqualTo("08:00");
        await Assert.That(slot.EndTime).IsEqualTo("08:45");
        await Assert.That(slot.CourseName).IsEqualTo("Matematik");
        await Assert.That(slot.ClassName).IsEqualTo("1.a");
        await Assert.That(slot.ClassId).IsEqualTo(klass.Id);
        await Assert.That(slot.SchemaId).IsEqualTo(schema.Id);
        await Assert.That(slot.TeacherId).IsEqualTo(staff.Id);
        await Assert.That(slot.TeacherName).IsEqualTo("Lone Lærer");
    }

    [Test]
    public async Task GetClassSchedule_ClassNotFound_Returns404()
    {
        // Act — unknown classId
        var response = await _client.GetAsync($"/api/v1/classes/{Guid.NewGuid()}/schedule");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetClassSchedule_NoActiveSchema_ReturnsEmpty()
    {
        // Arrange — class with a schema whose date range ended in the past
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var klass = new Class
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            Name = "2.b",
        };
        db.Classes.Add(klass);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var schema = new Schema
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            ClassId = klass.Id,
            Name = "Gammelt skema",
            StartDate = today.AddMonths(-6),
            EndDate = today.AddMonths(-1),  // ended in the past → not active today
        };
        db.Schemas.Add(schema);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/classes/{klass.Id}/schedule");

        // Assert — class exists but no active schema → empty list
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var slots = await response.Content.ReadFromJsonAsync<List<SchedulesController.ScheduleSlotDto>>(JsonOpts);
        await Assert.That(slots).IsNotNull();
        await Assert.That(slots!.Count).IsEqualTo(0);
    }

    // ── GET /api/v1/staff/{staffId}/schedule ──────────────────────────────────────

    [Test]
    public async Task GetStaffSchedule_WithSlots_ReturnsSlots()
    {
        // Arrange — staff is the teacher in an active schema slot
        var staff = await TestDataBuilder.CreateStaffAsync(
            _factory.Services, TestTenantContext.DefaultTenantId, "Peter Lærer");

        var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(
            _factory.Services, TestTenantContext.DefaultTenantId, "3.a");

        var timeSlot = await TestDataBuilder.CreateTimeSlotAsync(
            _factory.Services, TestTenantContext.DefaultTenantId,
            new TimeOnly(9, 0), new TimeOnly(9, 45), sortOrder: 2);

        var course = await TestDataBuilder.CreateCourseAsync(
            _factory.Services, TestTenantContext.DefaultTenantId, "Dansk");

        await TestDataBuilder.CreateSchemaSlotAsync(
            _factory.Services, TestTenantContext.DefaultTenantId,
            schema.Id, timeSlot.Id, course.Id, staff.Id,
            weekday: DayOfWeek.Tuesday);

        // Act
        var response = await _client.GetAsync($"/api/v1/staff/{staff.Id}/schedule");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var slots = await response.Content.ReadFromJsonAsync<List<SchedulesController.ScheduleSlotDto>>(JsonOpts);
        await Assert.That(slots).IsNotNull();
        await Assert.That(slots!.Count).IsEqualTo(1);

        var slot = slots[0];
        await Assert.That(slot.Weekday).IsEqualTo(DayOfWeek.Tuesday);
        await Assert.That(slot.StartTime).IsEqualTo("09:00");
        await Assert.That(slot.EndTime).IsEqualTo("09:45");
        await Assert.That(slot.CourseName).IsEqualTo("Dansk");
        await Assert.That(slot.ClassName).IsEqualTo("3.a");
        await Assert.That(slot.ClassId).IsEqualTo(klass.Id);
        await Assert.That(slot.TeacherId).IsEqualTo(staff.Id);
        await Assert.That(slot.TeacherName).IsEqualTo("Peter Lærer");
    }

    [Test]
    public async Task GetStaffSchedule_StaffNotFound_Returns404()
    {
        // Act — unknown staffId
        var response = await _client.GetAsync($"/api/v1/staff/{Guid.NewGuid()}/schedule");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
