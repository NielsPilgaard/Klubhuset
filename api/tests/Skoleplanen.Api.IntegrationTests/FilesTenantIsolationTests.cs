using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Skoleplanen.Api.Controllers;
using Skoleplanen.Api.IntegrationTests.Infrastructure;

namespace Skoleplanen.Api.IntegrationTests;

/// <summary>
/// Verifies that files are strictly scoped to the uploading tenant.
/// Skole A's files must never appear in Skole B's list or be fetchable by ID.
/// </summary>
public sealed class FilesTenantIsolationTests
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
    public async Task FilesUploadedByTenantA_AreNotVisibleToTenantB()
    {
        // Arrange — seed a file belonging to Tenant A
        var fileA = await TestDataBuilder.CreateSchoolFileAsync(_factory.Services, TenantA, "skole-a-dokument.pdf");

        // Act — list files as Tenant B
        _factory.TenantContext.TenantId = TenantB;
        using var clientB = _factory.CreateClient();

        var response = await clientB.GetAsync("/api/v1/files");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<FilesController.FilesResponseDto>(JsonOpts);

        // Assert — Tenant B sees no trace of Tenant A's file
        await Assert.That(body!.Files.Any(f => f.Id == fileA.Id)).IsFalse();
    }

    [Test]
    public async Task DeleteFileFromOtherTenant_Returns404_NotDataLeak()
    {
        // Arrange — seed a file belonging to Tenant A
        var fileA = await TestDataBuilder.CreateSchoolFileAsync(_factory.Services, TenantA, "slet-mig-ikke.pdf");

        // Act — Tenant B (admin) tries to delete it
        _factory.TenantContext.TenantId = TenantB;
        using var clientB = _factory.CreateClient();

        var response = await clientB.DeleteAsync($"/api/v1/files/{fileA.Id}");

        // Assert — 404, and the file still exists in Tenant A's storage
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        _factory.TenantContext.TenantId = TenantA;
        using var clientA = _factory.CreateClient();

        var listResponse = await clientA.GetAsync("/api/v1/files");
        var body = await listResponse.Content.ReadFromJsonAsync<FilesController.FilesResponseDto>(JsonOpts);
        await Assert.That(body!.Files.Any(f => f.Id == fileA.Id)).IsTrue();
    }
}
