using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Skoleoverblikket.Api.Controllers;
using Skoleoverblikket.Api.IntegrationTests.Infrastructure;
namespace Skoleoverblikket.Api.IntegrationTests;

/// <summary>
/// Verifies that the EF Core global query filter correctly scopes all data
/// to the current tenant. A room created by tenant A must never be visible
/// to tenant B — even when both share the same PostgreSQL database.
/// </summary>
public sealed class TenantIsolationTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private ApiFactory _factory = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        _factory = new ApiFactory();
        await _factory.StartAsync();
        await TestDataBuilder.CreateSchoolAsync(_factory.Services, TenantA, "Skole A");
        await TestDataBuilder.CreateSchoolAsync(_factory.Services, TenantB, "Skole B");
    }

    [After(Test)]
    public async Task TearDown()
    {
        await _factory.StopAsync();
        await _factory.DisposeAsync();
    }

    [Test]
    public async Task RoomsCreatedByTenantA_AreNotVisibleToTenantB()
    {
        // Arrange — create a room as Tenant A
        _factory.TenantContext.TenantId = TenantA;
        using var clientA = _factory.CreateClient();

        var createResponse = await clientA.PostAsJsonAsync("/api/v1/rooms",
            new RoomsController.UpsertRoomRequest("Lokale A-101", null, null));
        createResponse.EnsureSuccessStatusCode();
        var roomA = (await createResponse.Content.ReadFromJsonAsync<RoomsController.RoomDto>())!;

        // Act — list rooms as Tenant B
        _factory.TenantContext.TenantId = TenantB;
        using var clientB = _factory.CreateClient();

        var listResponse = await clientB.GetAsync("/api/v1/rooms");
        await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var rooms = await listResponse.Content.ReadFromJsonAsync<List<RoomsController.RoomDto>>();

        // Assert — Tenant B sees no rooms
        await Assert.That(rooms!.Any(r => r.Id == roomA.Id)).IsFalse();
    }

    [Test]
    public async Task GetByIdCrossTenant_Returns404_NotDataLeak()
    {
        // Arrange — create a room as Tenant A
        _factory.TenantContext.TenantId = TenantA;
        using var clientA = _factory.CreateClient();

        var createResponse = await clientA.PostAsJsonAsync("/api/v1/rooms",
            new RoomsController.UpsertRoomRequest("Geografi A", null, null));
        createResponse.EnsureSuccessStatusCode();
        var roomA = (await createResponse.Content.ReadFromJsonAsync<RoomsController.RoomDto>())!;

        // Act — attempt to fetch it as Tenant B by knowing the ID
        _factory.TenantContext.TenantId = TenantB;
        using var clientB = _factory.CreateClient();

        var getResponse = await clientB.GetAsync($"/api/v1/rooms/{roomA.Id}");

        // Assert — must be 404, never 200 with another tenant's data
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task StaffCreatedByTenantA_AreNotVisibleToTenantB()
    {
        // Arrange — create a staff member as Tenant A
        _factory.TenantContext.TenantId = TenantA;
        using var clientA = _factory.CreateClient();

        var createResponse = await clientA.PostAsJsonAsync("/api/v1/staff",
            new { name = "Hanne Hansen", role = "Teacher" });
        createResponse.EnsureSuccessStatusCode();
        var staffA = (await createResponse.Content.ReadFromJsonAsync<StaffController.StaffDto>(JsonOpts))!;

        // Act — list staff as Tenant B
        _factory.TenantContext.TenantId = TenantB;
        using var clientB = _factory.CreateClient();

        var listResponse = await clientB.GetAsync("/api/v1/staff");
        await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var staff = await listResponse.Content.ReadFromJsonAsync<List<StaffController.StaffDto>>(JsonOpts);

        // Assert
        await Assert.That(staff!.Any(s => s.Id == staffA.Id)).IsFalse();
    }
}
