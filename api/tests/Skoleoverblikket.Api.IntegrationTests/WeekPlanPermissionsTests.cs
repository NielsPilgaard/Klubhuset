using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Skoleoverblikket.Api.Controllers;
using Skoleoverblikket.Api.IntegrationTests.Infrastructure;

namespace Skoleoverblikket.Api.IntegrationTests;

/// <summary>
/// Verifies WeekPlan access for teachers assigned to a SchemaSlot.
///
/// WeekPlan editing ([PUT] ugeplan/slots) is open to all authenticated users — any teacher
/// can update the plan description for their lesson even when ClassPermissions restrict
/// schema editing. These tests guard against accidentally adding a schema-edit policy
/// to the WeekPlan endpoints.
/// </summary>
public sealed class WeekPlanPermissionsTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    private ApiFactory _factory = null!;
    private HttpClient _adminClient = null!;
    private readonly Guid _tenantId = TestTenantContext.DefaultTenantId;

    private const int TestYear = 2025;
    private const int TestWeek = 20;

    [Before(Test)]
    public async Task SetUp()
    {
        _factory = new ApiFactory();
        await _factory.StartAsync();
        await TestDataBuilder.CreateSchoolAsync(_factory.Services, _tenantId);
        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Add("X-Test-Roles", "admin");
        _adminClient.DefaultRequestHeaders.Add("X-Test-Subject", "admin-subject");
    }

    [After(Test)]
    public async Task TearDown()
    {
        _adminClient.Dispose();
        await _factory.StopAsync();
        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Teacher assigned to a SchemaSlot can edit the WeekPlan — even when
    /// ClassPermissions are active that would block schema editing for this teacher.
    /// </summary>
    [Test]
    public async Task AssignedTeacher_CanUpsertWeekPlanSlot_EvenWhenClassPermissionsExist()
    {
        const string teacherSubject = "assigned-teacher-subject";
        var teacher = await TestDataBuilder.CreateStaffAsync(
            _factory.Services, _tenantId, keycloakSubject: teacherSubject);

        var timeSlot = await TestDataBuilder.CreateTimeSlotAsync(
            _factory.Services, _tenantId, new TimeOnly(8, 0), new TimeOnly(8, 45));
        var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId);
        var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId);

        // Assign teacher to a slot in the schema
        var schemaSlot = await TestDataBuilder.CreateSchemaSlotAsync(
            _factory.Services, _tenantId,
            schema.Id, timeSlot.Id, course.Id, teacher.Id, DayOfWeek.Monday);

        // Activate restricted mode by granting a different admin permission on this class
        var otherAdmin = await TestDataBuilder.CreateStaffAsync(
            _factory.Services, _tenantId, isAdmin: true);
        await _adminClient.PostAsJsonAsync(
            $"/api/v1/classes/{klass.Id}/permissions",
            new { staffId = otherAdmin.Id });

        // The assigned teacher has no ClassPermission row and cannot edit the schema,
        // but must be able to update the WeekPlan for their own slot.
        using var teacherClient = _factory.CreateClient();
        teacherClient.DefaultRequestHeaders.Add("X-Test-Roles", "user");
        teacherClient.DefaultRequestHeaders.Add("X-Test-Subject", teacherSubject);

        var response = await teacherClient.PutAsJsonAsync(
            $"/api/v1/classes/{klass.Id}/ugeplan/slots?isoYear={TestYear}&isoWeek={TestWeek}",
            new WeekPlanController.UpsertWeekPlanSlotRequest(schemaSlot.Id, "Vi læser kapitel 5", null, null));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var slot = await response.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanSlotDto>(JsonOpts);
        await Assert.That(slot!.Beskrivelse).IsEqualTo("Vi læser kapitel 5");
    }

    /// <summary>
    /// Any authenticated user can read the WeekPlan — teachers not assigned to a class
    /// can still see its plan.
    /// </summary>
    [Test]
    public async Task AnyAuthenticatedUser_CanGetWeekPlan()
    {
        var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", "user");
        client.DefaultRequestHeaders.Add("X-Test-Subject", "random-teacher");

        var response = await client.GetAsync(
            $"/api/v1/classes/{klass.Id}/ugeplan?isoYear={TestYear}&isoWeek={TestWeek}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>
    /// A non-admin teacher not assigned to any slot can still edit the WeekPlan
    /// (WeekPlan editing is intentionally open to all authenticated staff).
    /// </summary>
    [Test]
    public async Task NonAssignedTeacher_CanUpsertWeekPlanSlot()
    {
        const string teacherSubject = "unassigned-teacher-subject";
        await TestDataBuilder.CreateStaffAsync(
            _factory.Services, _tenantId, keycloakSubject: teacherSubject);

        var timeSlot = await TestDataBuilder.CreateTimeSlotAsync(
            _factory.Services, _tenantId, new TimeOnly(9, 0), new TimeOnly(9, 45));
        var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId);
        var assignedTeacher = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId);
        var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId, "5.a");

        var schemaSlot = await TestDataBuilder.CreateSchemaSlotAsync(
            _factory.Services, _tenantId,
            schema.Id, timeSlot.Id, course.Id, assignedTeacher.Id, DayOfWeek.Tuesday);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", "user");
        client.DefaultRequestHeaders.Add("X-Test-Subject", teacherSubject);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/classes/{klass.Id}/ugeplan/slots?isoYear={TestYear}&isoWeek={TestWeek}",
            new WeekPlanController.UpsertWeekPlanSlotRequest(schemaSlot.Id, "Lektier: side 12-15", null, null));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
