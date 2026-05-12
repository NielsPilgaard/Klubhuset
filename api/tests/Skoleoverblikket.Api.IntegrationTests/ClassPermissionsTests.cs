using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Skoleoverblikket.Api.Controllers;
using Skoleoverblikket.Api.IntegrationTests.Infrastructure;
using Skoleoverblikket.Api.Models;

namespace Skoleoverblikket.Api.IntegrationTests;

/// <summary>
/// Integration tests for class-level permissions.
/// Covers:
///   - Superadmin (no ClassPermission rows) can edit any class schema.
///   - Admin with a ClassPermission row can edit that class, gets 403 on others.
///   - Granting/revoking permissions reflects immediately on subsequent requests.
///   - Non-admin cannot call permission management endpoints.
/// </summary>
public sealed class ClassPermissionsTests
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
		_adminClient = CreateAdminClient();
	}

	[After(Test)]
	public async Task TearDown()
	{
		_adminClient.Dispose();
		await _factory.StopAsync();
		await _factory.DisposeAsync();
	}

	private HttpClient CreateAdminClient(string subject = "test-user-id")
	{
		var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-Roles", "admin");
		client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
		return client;
	}

	private HttpClient CreateNonAdminClient()
	{
		var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-Roles", "user");
		return client;
	}

	// ── Superadmin mode (no permission rows) ──────────────────────────────────

	[Test]
	public async Task Superadmin_CanCreateSchema_WhenNoPermissionRowsExist()
	{
		var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId);

		var response = await _adminClient.PostAsJsonAsync(
			$"/api/v1/classes/{klass.Id}/schemas",
			new { name = "Nyt skema" });

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
	}

	[Test]
	public async Task Superadmin_CanUpsertSlot_WhenNoPermissionRowsExist()
	{
		var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId);

		var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, TestTenantContext.DefaultTenantId);
		var teacher = await TestDataBuilder.CreateStaffAsync(_factory.Services, TestTenantContext.DefaultTenantId,
			isAdmin: true, keycloakSubject: "test-user-id");
		var timeSlot = await TestDataBuilder.CreateTimeSlotAsync(
			_factory.Services, TestTenantContext.DefaultTenantId,
			new TimeOnly(8, 0), new TimeOnly(9, 0));

		var response = await _adminClient.PutAsJsonAsync(
			$"/api/v1/classes/{klass.Id}/schemas/{schema.Id}/slots",
			new { timeSlotId = timeSlot.Id, weekday = 1, courseId = course.Id, teacherId = teacher.Id });

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
	}

	// ── Permission-restricted mode ─────────────────────────────────────────────

	[Test]
	public async Task RestrictedAdmin_CanCreateSchema_OnAssignedClass()
	{
		const string subject = "restricted-admin-subject";
		var staff = await TestDataBuilder.CreateStaffAsync(_factory.Services, TestTenantContext.DefaultTenantId,
			isAdmin: false, keycloakSubject: subject);

		var (assignedClass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId, "3.a");
		var (otherClass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId, "4.b");

		// Grant permission on assignedClass only (this creates a permission row, enabling restricted mode)
		await _adminClient.PostAsJsonAsync(
			$"/api/v1/classes/{assignedClass.Id}/permissions",
			new { staffId = staff.Id });

		using var restrictedClient = CreateAdminClient(subject);

		var response = await restrictedClient.PostAsJsonAsync(
			$"/api/v1/classes/{assignedClass.Id}/schemas",
			new { name = "Mit skema" });

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
	}

	[Test]
	public async Task RestrictedAdmin_Gets403_OnUnassignedClass()
	{
		const string subject = "restricted-admin-subject-2";
		var staff = await TestDataBuilder.CreateStaffAsync(_factory.Services, TestTenantContext.DefaultTenantId,
			isAdmin: false, keycloakSubject: subject);

		var (assignedClass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId, "5.a");
		var (otherClass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId, "6.b");

		// Grant permission on assignedClass only — activates restricted mode
		await _adminClient.PostAsJsonAsync(
			$"/api/v1/classes/{assignedClass.Id}/permissions",
			new { staffId = staff.Id });

		using var restrictedClient = CreateAdminClient(subject);

		var response = await restrictedClient.PostAsJsonAsync(
			$"/api/v1/classes/{otherClass.Id}/schemas",
			new { name = "Uautoriseret skema" });

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
	}

	// ── Grant/Revoke ────────────────────────────────────────────────────────────

	[Test]
	public async Task GrantPermission_AppearsInGetList()
	{
		var staff = await TestDataBuilder.CreateStaffAsync(_factory.Services, TestTenantContext.DefaultTenantId);
		var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId);

		await _adminClient.PostAsJsonAsync(
			$"/api/v1/classes/{klass.Id}/permissions",
			new { staffId = staff.Id });

		var listResponse = await _adminClient.GetAsync($"/api/v1/classes/{klass.Id}/permissions");
		await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

		var list = await listResponse.Content.ReadFromJsonAsync<List<ClassPermissionsController.ClassPermissionDto>>(JsonOpts);
		await Assert.That(list!.Any(p => p.StaffId == staff.Id)).IsTrue();
	}

	[Test]
	public async Task GrantPermission_Returns409_WhenAlreadyGranted()
	{
		var staff = await TestDataBuilder.CreateStaffAsync(_factory.Services, TestTenantContext.DefaultTenantId);
		var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId);

		await _adminClient.PostAsJsonAsync(
			$"/api/v1/classes/{klass.Id}/permissions",
			new { staffId = staff.Id });

		var duplicate = await _adminClient.PostAsJsonAsync(
			$"/api/v1/classes/{klass.Id}/permissions",
			new { staffId = staff.Id });

		await Assert.That(duplicate.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
	}

	[Test]
	public async Task RevokePermission_RemovesFromList_AndRestoresAccess()
	{
		const string subject = "revoke-test-subject";
		var staff = await TestDataBuilder.CreateStaffAsync(_factory.Services, TestTenantContext.DefaultTenantId,
			isAdmin: true, keycloakSubject: subject);
		var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId);

		// Grant — enters restricted mode
		await _adminClient.PostAsJsonAsync(
			$"/api/v1/classes/{klass.Id}/permissions",
			new { staffId = staff.Id });

		// Revoke — removes the only permission row; tenant returns to superadmin mode
		var revokeResponse = await _adminClient.DeleteAsync(
			$"/api/v1/classes/{klass.Id}/permissions/{staff.Id}");
		await Assert.That(revokeResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

		// No more rows → superadmin mode → restricted admin now has full access
		using var restrictedClient = CreateAdminClient(subject);
		var schemaResponse = await restrictedClient.PostAsJsonAsync(
			$"/api/v1/classes/{klass.Id}/schemas",
			new { name = "Skema efter revoke" });

		await Assert.That(schemaResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
	}

	[Test]
	public async Task RevokePermission_Returns404_ForUnknownStaff()
	{
		var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId);

		var response = await _adminClient.DeleteAsync(
			$"/api/v1/classes/{klass.Id}/permissions/{Guid.NewGuid()}");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
	}

	// ── Role guard on permissions endpoints ────────────────────────────────────

	[Test]
	public async Task NonAdmin_Gets403_OnGetPermissions()
	{
		var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId);

		using var client = CreateNonAdminClient();
		var response = await client.GetAsync($"/api/v1/classes/{klass.Id}/permissions");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
	}

	[Test]
	public async Task NonAdmin_Gets403_OnGrantPermission()
	{
		var staff = await TestDataBuilder.CreateStaffAsync(_factory.Services, TestTenantContext.DefaultTenantId);
		var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(
			_factory.Services, TestTenantContext.DefaultTenantId);

		using var client = CreateNonAdminClient();
		var response = await client.PostAsJsonAsync(
			$"/api/v1/classes/{klass.Id}/permissions",
			new { staffId = staff.Id });

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
	}
}
