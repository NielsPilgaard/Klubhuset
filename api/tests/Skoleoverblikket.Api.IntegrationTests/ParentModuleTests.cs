using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Controllers;
using Skoleoverblikket.Api.IntegrationTests.Infrastructure;
using Skoleoverblikket.Api.Models;

namespace Skoleoverblikket.Api.IntegrationTests;

public sealed class ParentModuleTests
{
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

	// -- helper to insert a parent + student directly via DB --
	private async Task<(Parent parent, Student student, Class klass)> SeedParentWithStudentAsync(
		string keycloakSubject)
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var klass = new Class
		{
			Id = Guid.NewGuid(),
			TenantId = TestTenantContext.DefaultTenantId,
			Name = "3.a",
		};
		db.Classes.Add(klass);

		var student = new Student
		{
			Id = Guid.NewGuid(),
			TenantId = TestTenantContext.DefaultTenantId,
			Name = "Lars Larsen",
			ClassId = klass.Id,
		};
		db.Students.Add(student);

		var parent = new Parent
		{
			Id = Guid.NewGuid(),
			TenantId = TestTenantContext.DefaultTenantId,
			Name = "Bente Larsen",
			Email = "bente@test.dk",
			KeycloakSubject = keycloakSubject,
		};
		parent.Students.Add(student);
		db.Parents.Add(parent);

		await db.SaveChangesAsync();
		return (parent, student, klass);
	}

	[Test]
	public async Task GetParents_ReturnsEmptyList_WhenNoneExist()
	{
		var response = await _adminClient.GetAsync("/api/v1/parents");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var list = await response.Content.ReadFromJsonAsync<List<ParentsController.ParentDto>>();
		await Assert.That(list).IsNotNull();
		await Assert.That(list!.Count).IsEqualTo(0);
	}

	[Test]
	public async Task GetParents_Returns403_ForNonAdmin()
	{
		using var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-Roles", "teacher");

		var response = await client.GetAsync("/api/v1/parents");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
	}

	[Test]
	public async Task GetStudents_ReturnsEmptyList_WhenNoneExist()
	{
		var response = await _adminClient.GetAsync("/api/v1/students");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var list = await response.Content.ReadFromJsonAsync<List<StudentsController.StudentDto>>();
		await Assert.That(list).IsNotNull();
		await Assert.That(list!.Count).IsEqualTo(0);
	}

	[Test]
	public async Task GetStudents_Returns403_ForNonAdmin()
	{
		using var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-Roles", "teacher");

		var response = await client.GetAsync("/api/v1/students");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
	}

	[Test]
	public async Task GetSchemas_Returns200_ForParent_WithAccessToClass()
	{
		const string parentSubject = "parent-sub-001";
		var (_, _, klass) = await SeedParentWithStudentAsync(parentSubject);

		using var parentClient = _factory.CreateClient();
		parentClient.DefaultRequestHeaders.Add("X-Test-Roles", "parent");
		parentClient.DefaultRequestHeaders.Add("X-Test-Subject", parentSubject);

		var response = await parentClient.GetAsync($"/api/v1/classes/{klass.Id}/schemas");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
	}

	[Test]
	public async Task GetSchemas_Returns403_ForParent_WithNoAccessToClass()
	{
		const string parentSubject = "parent-sub-002";
		await SeedParentWithStudentAsync(parentSubject);

		// A different class this parent has no student in
		var otherClass = new Class
		{
			Id = Guid.NewGuid(),
			TenantId = TestTenantContext.DefaultTenantId,
			Name = "7.x",
		};
		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			db.Classes.Add(otherClass);
			await db.SaveChangesAsync();
		}

		using var parentClient = _factory.CreateClient();
		parentClient.DefaultRequestHeaders.Add("X-Test-Roles", "parent");
		parentClient.DefaultRequestHeaders.Add("X-Test-Subject", parentSubject);

		var response = await parentClient.GetAsync($"/api/v1/classes/{otherClass.Id}/schemas");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
	}

	[Test]
	public async Task GetSchemas_Returns200_ForAdmin_Always()
	{
		var (_, _, klass) = await SeedParentWithStudentAsync("some-other-subject");

		var response = await _adminClient.GetAsync($"/api/v1/classes/{klass.Id}/schemas");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
	}
}
