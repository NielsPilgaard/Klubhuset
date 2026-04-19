using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Skoleplanen.Api.Controllers;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.IntegrationTests.Infrastructure;
using Skoleplanen.Api.Models;

namespace Skoleplanen.Api.IntegrationTests;

public sealed class WeekPlanTests
{
    // The API serializes enums as strings; use the same options when deserializing responses.
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    private ApiFactory _factory = null!;
    private HttpClient _client = null!;
    private readonly Guid _tenantId = TestTenantContext.DefaultTenantId;

    // A fixed ISO week well away from year boundaries
    private const int TestYear = 2025;
    private const int TestWeek = 10;

    [Before(Test)]
    public async Task SetUp()
    {
        _factory = new ApiFactory();
        await _factory.StartAsync();
        await TestDataBuilder.CreateSchoolAsync(_factory.Services, _tenantId);
        _client = _factory.CreateClient();
    }

    [After(Test)]
    public async Task TearDown()
    {
        _client.Dispose();
        await _factory.StopAsync();
        await _factory.DisposeAsync();
    }

    [Test]
    public async Task GetWeekPlan_NoActiveSchema_ReturnsEmptySlots()
    {
        // A class with no schema at all
        _ = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId, "1.a", "Skema");
        // Deactivate the schema by re-creating without IsActive=true — actually CreateClassWithSchemaAsync creates it active.
        // We create a class without a schema instead by using the DB directly.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var noSchemaClass = new Class { Id = Guid.NewGuid(), TenantId = _tenantId, Name = "NoSchema" };
        db.Classes.Add(noSchemaClass);
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/v1/classes/{noSchemaClass.Id}/ugeplan?isoYear={TestYear}&isoWeek={TestWeek}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanDto>(JsonOpts);
        await Assert.That(dto).IsNotNull();
        await Assert.That(dto!.Slots.Count).IsEqualTo(0);
        await Assert.That(dto.IsHolidayWeek).IsFalse();
    }

    [Test]
    public async Task GetWeekPlan_WithActiveSchema_ReturnsSlotCount()
    {
        var timeSlot = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services, _tenantId,
            new TimeOnly(8, 0), new TimeOnly(8, 45), sortOrder: 1);
        var teacher = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId);
        var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId);
        var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId);

        // Add a schema slot via the API
        await _client.PutAsJsonAsync($"/api/v1/classes/{klass.Id}/schemas/{schema.Id}/slots",
            new { timeSlotId = timeSlot.Id, weekday = (int)DayOfWeek.Monday, courseId = course.Id, teacherId = teacher.Id });

        var response = await _client.GetAsync($"/api/v1/classes/{klass.Id}/ugeplan?isoYear={TestYear}&isoWeek={TestWeek}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanDto>(JsonOpts);
        await Assert.That(dto!.Slots.Count).IsEqualTo(1);
        await Assert.That(dto.Slots[0].CourseName).IsEqualTo(course.Name);
    }

    [Test]
    public async Task GetWeekPlan_HolidayWeek_IsHolidayWeekTrue()
    {
        var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId);

        // Seed a Ferie entry spanning TestYear week TestWeek (Mon = 2025-03-03, Fri = 2025-03-07)
        var weekMon = new DateOnly(2025, 3, 3);
        var weekFri = new DateOnly(2025, 3, 7);
        await TestDataBuilder.CreateCalendarEntryAsync(
            _factory.Services, _tenantId,
            CalendarEntryType.Ferie, "Vinterferie", weekMon, weekFri);

        var response = await _client.GetAsync($"/api/v1/classes/{klass.Id}/ugeplan?isoYear={TestYear}&isoWeek={TestWeek}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanDto>(JsonOpts);
        await Assert.That(dto!.IsHolidayWeek).IsTrue();
        await Assert.That(dto.HolidayTitle).IsEqualTo("Vinterferie");
    }

    [Test]
    public async Task GetWeekPlan_MissingParams_Returns400()
    {
        var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId);
        var response = await _client.GetAsync($"/api/v1/classes/{klass.Id}/ugeplan");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpsertSlot_CreatesBeskrivelse_AndReturnsMergedSlot()
    {
        var timeSlot = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services, _tenantId,
            new TimeOnly(9, 0), new TimeOnly(9, 45), sortOrder: 2);
        var teacher = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId);
        var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId);
        var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId);

        var upsertSlotResponse = await _client.PutAsJsonAsync(
            $"/api/v1/classes/{klass.Id}/schemas/{schema.Id}/slots",
            new { timeSlotId = timeSlot.Id, weekday = (int)DayOfWeek.Tuesday, courseId = course.Id, teacherId = teacher.Id });
        upsertSlotResponse.EnsureSuccessStatusCode();

        // Get the schemaSlotId from the GET response
        var getResponse = await _client.GetAsync($"/api/v1/classes/{klass.Id}/ugeplan?isoYear={TestYear}&isoWeek={TestWeek}");
        var planDto = await getResponse.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanDto>(JsonOpts);
        var schemaSlotId = planDto!.Slots[0].SchemaSlotId;

        var putResponse = await _client.PutAsJsonAsync(
            $"/api/v1/classes/{klass.Id}/ugeplan/slots?isoYear={TestYear}&isoWeek={TestWeek}",
            new WeekPlanController.UpsertWeekPlanSlotRequest(schemaSlotId, "Vi læser kapitel 3", null, null));

        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var slotDto = await putResponse.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanSlotDto>(JsonOpts);
        await Assert.That(slotDto!.Beskrivelse).IsEqualTo("Vi læser kapitel 3");
        await Assert.That(slotDto.CourseName).IsEqualTo(course.Name);
        await Assert.That(slotDto.OriginalCourseId).IsNull();
    }

    [Test]
    public async Task UpsertSlot_WithFagSwap_ReturnsOverriddenCourseName()
    {
        var timeSlot = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services, _tenantId,
            new TimeOnly(10, 0), new TimeOnly(10, 45), sortOrder: 3);
        var teacher = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId);
        var originalCourse = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId, "Dansk");
        var swapCourse = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId, "Matematik");
        var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId);

        await _client.PutAsJsonAsync($"/api/v1/classes/{klass.Id}/schemas/{schema.Id}/slots",
            new { timeSlotId = timeSlot.Id, weekday = (int)DayOfWeek.Wednesday, courseId = originalCourse.Id, teacherId = teacher.Id });

        var getResponse = await _client.GetAsync($"/api/v1/classes/{klass.Id}/ugeplan?isoYear={TestYear}&isoWeek={TestWeek}");
        var planDto = await getResponse.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanDto>(JsonOpts);
        var schemaSlotId = planDto!.Slots[0].SchemaSlotId;

        var putResponse = await _client.PutAsJsonAsync(
            $"/api/v1/classes/{klass.Id}/ugeplan/slots?isoYear={TestYear}&isoWeek={TestWeek}",
            new WeekPlanController.UpsertWeekPlanSlotRequest(schemaSlotId, null, null, swapCourse.Id));

        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var slotDto = await putResponse.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanSlotDto>(JsonOpts);
        await Assert.That(slotDto!.CourseName).IsEqualTo("Matematik");
        await Assert.That(slotDto.OriginalCourseName).IsEqualTo("Dansk");
        await Assert.That(slotDto.OriginalCourseId).IsEqualTo(originalCourse.Id);
    }

    [Test]
    public async Task UpsertSlot_UnknownSchemaSlot_Returns400()
    {
        var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId);
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/classes/{klass.Id}/ugeplan/slots?isoYear={TestYear}&isoWeek={TestWeek}",
            new WeekPlanController.UpsertWeekPlanSlotRequest(Guid.NewGuid(), null, null, null));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    private async Task<(Guid classId, Guid schemaSlotId)> SetupClassWithSlotAsync(string className = "2.b")
    {
        var timeSlot = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services, _tenantId,
            new TimeOnly(11, 0), new TimeOnly(11, 45), sortOrder: 4);
        var teacher = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId);
        var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId);
        var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId, className);

        await _client.PutAsJsonAsync($"/api/v1/classes/{klass.Id}/schemas/{schema.Id}/slots",
            new { timeSlotId = timeSlot.Id, weekday = (int)DayOfWeek.Thursday, courseId = course.Id, teacherId = teacher.Id });

        var getResponse = await _client.GetAsync($"/api/v1/classes/{klass.Id}/ugeplan?isoYear={TestYear}&isoWeek={TestWeek}");
        var planDto = await getResponse.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanDto>(JsonOpts);
        var schemaSlotId = planDto!.Slots[0].SchemaSlotId;

        return (klass.Id, schemaSlotId);
    }

    private async Task<Guid> UpsertWeekPlanSlotAndGetId(Guid classId, Guid schemaSlotId)
    {
        var putResponse = await _client.PutAsJsonAsync(
            $"/api/v1/classes/{classId}/ugeplan/slots?isoYear={TestYear}&isoWeek={TestWeek}",
            new WeekPlanController.UpsertWeekPlanSlotRequest(schemaSlotId, "test", null, null));
        putResponse.EnsureSuccessStatusCode();
        var slotDto = await putResponse.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanSlotDto>(JsonOpts);
        return slotDto!.Id;
    }

    [Test]
    public async Task AddFile_ThenRemoveFile_RoundTrips()
    {
        var (classId, schemaSlotId) = await SetupClassWithSlotAsync("3.a");
        var file = await TestDataBuilder.CreateSchoolFileAsync(_factory.Services, _tenantId, "opgave.pdf");
        var slotId = await UpsertWeekPlanSlotAndGetId(classId, schemaSlotId);

        // Add file
        var addResponse = await _client.PostAsJsonAsync(
            $"/api/v1/classes/{classId}/ugeplan/slots/{slotId}/files",
            new WeekPlanController.AddFileToSlotRequest(file.Id));

        await Assert.That(addResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var fileDto = await addResponse.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanSlotFileDto>();
        await Assert.That(fileDto!.FileName).IsEqualTo("opgave.pdf");

        // Verify it appears in GET
        var getResponse = await _client.GetAsync($"/api/v1/classes/{classId}/ugeplan?isoYear={TestYear}&isoWeek={TestWeek}");
        var planDto = await getResponse.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanDto>(JsonOpts);
        var slot = planDto!.Slots.First(s => s.Id == slotId);
        await Assert.That(slot.Files.Count).IsEqualTo(1);

        // Remove file
        var deleteResponse = await _client.DeleteAsync(
            $"/api/v1/classes/{classId}/ugeplan/slots/{slotId}/files/{fileDto.Id}");
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Verify removed
        var getResponse2 = await _client.GetAsync($"/api/v1/classes/{classId}/ugeplan?isoYear={TestYear}&isoWeek={TestWeek}");
        var planDto2 = await getResponse2.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanDto>(JsonOpts);
        var slot2 = planDto2!.Slots.First(s => s.Id == slotId);
        await Assert.That(slot2.Files.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AddFile_Duplicate_Returns409()
    {
        var (classId, schemaSlotId) = await SetupClassWithSlotAsync("4.a");
        var file = await TestDataBuilder.CreateSchoolFileAsync(_factory.Services, _tenantId, "duplikat.pdf");
        var slotId = await UpsertWeekPlanSlotAndGetId(classId, schemaSlotId);

        var addRequest = new WeekPlanController.AddFileToSlotRequest(file.Id);

        var first = await _client.PostAsJsonAsync(
            $"/api/v1/classes/{classId}/ugeplan/slots/{slotId}/files", addRequest);
        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync(
            $"/api/v1/classes/{classId}/ugeplan/slots/{slotId}/files", addRequest);
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task TenantIsolation_WeekPlanNotVisibleToOtherTenant()
    {
        var timeSlot = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services, _tenantId,
            new TimeOnly(12, 0), new TimeOnly(12, 45), sortOrder: 5);
        var teacher = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId);
        var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId);
        var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId);

        // Create a schema slot and upsert a week plan slot as tenant A
        await _client.PutAsJsonAsync($"/api/v1/classes/{klass.Id}/schemas/{schema.Id}/slots",
            new { timeSlotId = timeSlot.Id, weekday = (int)DayOfWeek.Friday, courseId = course.Id, teacherId = teacher.Id });

        var getResponse = await _client.GetAsync($"/api/v1/classes/{klass.Id}/ugeplan?isoYear={TestYear}&isoWeek={TestWeek}");
        var planDto = await getResponse.Content.ReadFromJsonAsync<WeekPlanController.WeekPlanDto>(JsonOpts);
        var schemaSlotId = planDto!.Slots[0].SchemaSlotId;

        await _client.PutAsJsonAsync(
            $"/api/v1/classes/{klass.Id}/ugeplan/slots?isoYear={TestYear}&isoWeek={TestWeek}",
            new WeekPlanController.UpsertWeekPlanSlotRequest(schemaSlotId, "Tenant A beskrivelse", null, null));

        // Switch to tenant B
        var secondTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await using var factory2 = new ApiFactory();
        await factory2.StartAsync();
        await TestDataBuilder.CreateSchoolAsync(factory2.Services, secondTenantId, "Anden skole");
        factory2.TenantContext.TenantId = secondTenantId;
        using var clientB = factory2.CreateClient();

        // Tenant B cannot see tenant A's class
        var responseTenantB = await clientB.GetAsync($"/api/v1/classes/{klass.Id}/ugeplan?isoYear={TestYear}&isoWeek={TestWeek}");
        await Assert.That(responseTenantB.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
