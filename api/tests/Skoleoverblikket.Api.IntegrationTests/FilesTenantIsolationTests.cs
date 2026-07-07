using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Skoleoverblikket.Api.Controllers;
using Skoleoverblikket.Api.IntegrationTests.Infrastructure;

namespace Skoleoverblikket.Api.IntegrationTests;

/// <summary>
/// Verifies that files are strictly scoped to the uploading tenant.
/// Skole A's files must never appear in Skole B's list or be fetchable by ID.
/// </summary>
[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
public sealed class FilesTenantIsolationTests(ApiFactory factory)
{
	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		Converters = { new JsonStringEnumConverter() },
		PropertyNameCaseInsensitive = true,
	};

	private readonly ApiFactory _factory = factory;

	private Guid _tenantA;
	private Guid _tenantB;

	[Before(Test)]
	public async Task SetUpTenants()
	{
		_tenantA = Guid.NewGuid();
		_tenantB = Guid.NewGuid();
		await TestDataBuilder.CreateSchoolAsync(_factory.Services, _tenantA, "Skole A");
		await TestDataBuilder.CreateSchoolAsync(_factory.Services, _tenantB, "Skole B");
	}

	private HttpClient CreateAdminClient(Guid tenantId)
	{
		var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-TenantId", tenantId.ToString());
		client.DefaultRequestHeaders.Add("X-Test-Roles", "admin");
		return client;
	}

	[Test]
	public async Task FilesUploadedByTenantA_AreNotVisibleToTenantB()
	{
		var fileA = await TestDataBuilder.CreateSchoolFileAsync(_factory.Services, _tenantA, "skole-a-dokument.pdf");

		using var clientB = CreateAdminClient(_tenantB);

		var response = await clientB.GetAsync("/api/v1/files");
		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

		var body = await response.Content.ReadFromJsonAsync<FilesController.FilesResponseDto>(JsonOpts);

		await Assert.That(body!.Files.Any(f => f.Id == fileA.Id)).IsFalse();
	}

	[Test]
	public async Task DeleteFileFromOtherTenant_Returns404_NotDataLeak()
	{
		var fileA = await TestDataBuilder.CreateSchoolFileAsync(_factory.Services, _tenantA, "slet-mig-ikke.pdf");

		using var clientB = CreateAdminClient(_tenantB);
		var response = await clientB.DeleteAsync($"/api/v1/files/{fileA.Id}");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

		// File must still exist under Tenant A
		using var clientA = CreateAdminClient(_tenantA);
		var listResponse = await clientA.GetAsync("/api/v1/files");
		var body = await listResponse.Content.ReadFromJsonAsync<FilesController.FilesResponseDto>(JsonOpts);
		await Assert.That(body!.Files.Any(f => f.Id == fileA.Id)).IsTrue();
	}
}
