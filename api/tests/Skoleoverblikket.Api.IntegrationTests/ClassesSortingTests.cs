using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Skoleoverblikket.Api.Controllers;
using Skoleoverblikket.Api.IntegrationTests.Infrastructure;

namespace Skoleoverblikket.Api.IntegrationTests;

/// <summary>
/// Verifies that GET /api/v1/classes returns classes in numeric-aware order.
/// Lexicographic sort produces "0.a, 10.a, 1.a, 2.a" — the correct order is "0.a, 1.a, 2.a, 10.a".
/// </summary>
public sealed class ClassesSortingTests
{
	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		Converters = { new JsonStringEnumConverter() },
		PropertyNameCaseInsensitive = true,
	};

	private ApiFactory _factory = null!;
	private HttpClient _adminClient = null!;

	[Before(Test)]
	public async Task SetUp()
	{
		_factory = new ApiFactory();
		await _factory.StartAsync();
		await TestDataBuilder.CreateSchoolAsync(_factory.Services, TestTenantContext.DefaultTenantId);
		_adminClient = _factory.CreateClient();
		_adminClient.DefaultRequestHeaders.Add("X-Test-Roles", "admin");
	}

	[After(Test)]
	public async Task TearDown()
	{
		_adminClient.Dispose();
		await _factory.StopAsync();
		await _factory.DisposeAsync();
	}

	[Test]
	public async Task GetAll_Admin_ReturnsClassesInNumericOrder()
	{
		// Insert out-of-order so the test fails if the API sorts lexicographically
		foreach (var name in new[] { "10.a", "2.a", "0.a", "1.a" })
		{
			await TestDataBuilder.CreateClassWithSchemaAsync(
				_factory.Services, TestTenantContext.DefaultTenantId, name);
		}

		var response = await _adminClient.GetAsync("/api/v1/classes");
		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

		var classes = await response.Content.ReadFromJsonAsync<List<ClassesController.ClassDto>>(JsonOpts);
		var names = classes!.Select(c => c.Name).ToList();

		var zeroIdx = names.IndexOf("0.a");
		var oneIdx = names.IndexOf("1.a");
		var twoIdx = names.IndexOf("2.a");
		var tenIdx = names.IndexOf("10.a");

		await Assert.That(zeroIdx).IsLessThan(oneIdx);
		await Assert.That(oneIdx).IsLessThan(twoIdx);
		await Assert.That(twoIdx).IsLessThan(tenIdx);
	}

	[Test]
	public async Task GetAll_NonAdmin_ReturnsClassesInNumericOrder()
	{
		const string subject = "teacher-sort-test";
		await TestDataBuilder.CreateStaffAsync(
			_factory.Services, TestTenantContext.DefaultTenantId, keycloakSubject: subject);

		foreach (var name in new[] { "10.b", "2.b", "0.b", "1.b" })
		{
			await TestDataBuilder.CreateClassWithSchemaAsync(
				_factory.Services, TestTenantContext.DefaultTenantId, name);
		}

		using var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-Roles", "user");
		client.DefaultRequestHeaders.Add("X-Test-Subject", subject);

		var response = await client.GetAsync("/api/v1/classes");
		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

		var classes = await response.Content.ReadFromJsonAsync<List<ClassesController.ClassDto>>(JsonOpts);
		var names = classes!.Select(c => c.Name).ToList();

		var zeroIdx = names.IndexOf("0.b");
		var oneIdx = names.IndexOf("1.b");
		var twoIdx = names.IndexOf("2.b");
		var tenIdx = names.IndexOf("10.b");

		await Assert.That(zeroIdx).IsLessThan(oneIdx);
		await Assert.That(oneIdx).IsLessThan(twoIdx);
		await Assert.That(twoIdx).IsLessThan(tenIdx);
	}
}
