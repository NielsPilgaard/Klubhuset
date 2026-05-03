using System.Net;
using System.Net.Http.Json;
using Skoleoverblikket.Api.Controllers;
using Skoleoverblikket.Api.IntegrationTests.Infrastructure;
namespace Skoleoverblikket.Api.IntegrationTests;

/// <summary>
/// Full CRUD lifecycle tests for /api/v1/rooms.
/// Rooms are a simple resource with no cross-entity dependencies,
/// making them a good smoke-test for the HTTP pipeline + DB integration.
/// </summary>
public sealed class RoomsCrudTests
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
    public async Task GetAll_ReturnsEmptyList_WhenNoRoomsExist()
    {
        var response = await _client.GetAsync("/api/v1/rooms");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var rooms = await response.Content.ReadFromJsonAsync<List<RoomsController.RoomDto>>();
        await Assert.That(rooms).IsNotNull();
        await Assert.That(rooms!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_ThenGetById_ReturnsCreatedRoom()
    {
        var request = new RoomsController.UpsertRoomRequest("Lokale 12", 30, "Fysiklokale");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/rooms", request);
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<RoomsController.RoomDto>();
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.Name).IsEqualTo("Lokale 12");
        await Assert.That(created.Capacity).IsEqualTo(30);

        var getResponse = await _client.GetAsync($"/api/v1/rooms/{created.Id}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<RoomsController.RoomDto>();
        await Assert.That(fetched!.Name).IsEqualTo("Lokale 12");
    }

    [Test]
    public async Task Update_ChangesRoomName()
    {
        var created = await CreateRoomAsync("Gym");

        var updateRequest = new RoomsController.UpsertRoomRequest("Gymnastiksalen", 60, null);
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/rooms/{created.Id}", updateRequest);
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<RoomsController.RoomDto>();
        await Assert.That(updated!.Name).IsEqualTo("Gymnastiksalen");
        await Assert.That(updated.Capacity).IsEqualTo(60);
    }

    [Test]
    public async Task Delete_RemovesRoom_AndGetByIdReturns404()
    {
        var created = await CreateRoomAsync("Musikrum");

        var deleteResponse = await _client.DeleteAsync($"/api/v1/rooms/{created.Id}");
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/rooms/{created.Id}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Create_WithMissingName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/rooms", new { capacity = 10 });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/rooms/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    private async Task<RoomsController.RoomDto> CreateRoomAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/rooms",
            new RoomsController.UpsertRoomRequest(name, null, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RoomsController.RoomDto>())!;
    }
}
