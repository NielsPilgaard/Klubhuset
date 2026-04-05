using System.Net;
using System.Net.Http.Json;
using Skoleplanen.Api.Controllers;
using Skoleplanen.Api.IntegrationTests.Infrastructure;
using Skoleplanen.Api.Models;

namespace Skoleplanen.Api.IntegrationTests;

public sealed class CalendarCrudTests
{
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

    [Test]
    public async Task GetAll_ReturnsEmptyList_WhenNoEntriesExist()
    {
        var response = await _client.GetAsync("/api/v1/calendar");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<CalendarController.CalendarEntryDto>>();
        await Assert.That(entries).IsNotNull();
        await Assert.That(entries!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_ThenGetAll_ReturnsCreatedEntry()
    {
        var request = new CalendarController.CreateCalendarEntryRequest(
            "Efterårsferie", CalendarEntryType.Ferie,
            new DateOnly(2025, 10, 13), new DateOnly(2025, 10, 17));

        var createResponse = await _client.PostAsJsonAsync("/api/v1/calendar", request);
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CalendarController.CalendarEntryDto>();
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.Title).IsEqualTo("Efterårsferie");

        var getResponse = await _client.GetAsync("/api/v1/calendar");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var entries = await getResponse.Content.ReadFromJsonAsync<List<CalendarController.CalendarEntryDto>>();
        await Assert.That(entries!.Count).IsEqualTo(1);
        await Assert.That(entries[0].Title).IsEqualTo("Efterårsferie");
    }

    [Test]
    public async Task Create_WithEndDateBeforeStartDate_Returns400()
    {
        var request = new CalendarController.CreateCalendarEntryRequest(
            "Ugyldig", CalendarEntryType.Ferie,
            new DateOnly(2025, 10, 17), new DateOnly(2025, 10, 13));

        var response = await _client.PostAsJsonAsync("/api/v1/calendar", request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_ChangesTitle()
    {
        var created = await CreateEntryAsync("Gammel titel");

        var updateRequest = new CalendarController.UpdateCalendarEntryRequest(
            "Ny titel", CalendarEntryType.Begivenhed,
            new DateOnly(2025, 11, 1), new DateOnly(2025, 11, 1));
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/calendar/{created.Id}", updateRequest);
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<CalendarController.CalendarEntryDto>();
        await Assert.That(updated!.Title).IsEqualTo("Ny titel");
        await Assert.That(updated.Type).IsEqualTo(CalendarEntryType.Begivenhed);
    }

    [Test]
    public async Task Delete_RemovesEntry()
    {
        var created = await CreateEntryAsync("Slet mig");

        var deleteResponse = await _client.DeleteAsync($"/api/v1/calendar/{created.Id}");
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync("/api/v1/calendar");
        var entries = await getResponse.Content.ReadFromJsonAsync<List<CalendarController.CalendarEntryDto>>();
        await Assert.That(entries!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetDefaults_ReturnsNonEmptyList()
    {
        var response = await _client.GetAsync("/api/v1/calendar/defaults?year=2025");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var defaults = await response.Content.ReadFromJsonAsync<List<CalendarController.DefaultHolidayDto>>();
        await Assert.That(defaults).IsNotNull();
        await Assert.That(defaults!.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task TenantIsolation_EntryNotVisibleToOtherTenant()
    {
        // Create entry for default tenant
        await CreateEntryAsync("Tenant 1 ferie");

        // Create a second factory with a different tenant
        await using var factory2 = new ApiFactory();
        await factory2.StartAsync();
        var secondTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await TestDataBuilder.CreateSchoolAsync(factory2.Services, secondTenantId, "Anden skole");
        // Seed the entry directly for the second tenant
        await TestDataBuilder.CreateCalendarEntryAsync(
            factory2.Services, secondTenantId,
            CalendarEntryType.Ferie, "Tenant 2 ferie",
            new DateOnly(2025, 12, 22), new DateOnly(2026, 1, 2));

        // The default tenant's client should only see its own entry
        var response = await _client.GetAsync("/api/v1/calendar");
        var entries = await response.Content.ReadFromJsonAsync<List<CalendarController.CalendarEntryDto>>();
        await Assert.That(entries!.Count).IsEqualTo(1);
        await Assert.That(entries[0].Title).IsEqualTo("Tenant 1 ferie");
    }

    private async Task<CalendarController.CalendarEntryDto> CreateEntryAsync(string title)
    {
        var request = new CalendarController.CreateCalendarEntryRequest(
            title, CalendarEntryType.Ferie,
            new DateOnly(2025, 10, 13), new DateOnly(2025, 10, 17));
        var response = await _client.PostAsJsonAsync("/api/v1/calendar", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CalendarController.CalendarEntryDto>())!;
    }
}
