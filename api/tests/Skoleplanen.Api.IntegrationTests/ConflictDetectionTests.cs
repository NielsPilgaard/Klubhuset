using System.Net;
using System.Net.Http.Json;
using Skoleplanen.Api.Controllers;
using Skoleplanen.Api.IntegrationTests.Infrastructure;
using Skoleplanen.Api.Services;

namespace Skoleplanen.Api.IntegrationTests;

/// <summary>
/// Tests for the conflict detection pipeline via the HTTP API.
/// Conflict detection must catch teacher / room double-bookings
/// when the same resource is assigned to overlapping time slots.
/// </summary>
public sealed class ConflictDetectionTests
{
	private ApiFactory _factory = null!;
	private HttpClient _client = null!;
	private readonly Guid _tenantId = TestTenantContext.DefaultTenantId;

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
	public async Task NoConflicts_WhenTeacherAssignedToNonOverlappingSlots()
	{
		var slot1 = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services,
															  _tenantId,
															  new TimeOnly(8, 0),
															  new TimeOnly(8, 45),
															  sortOrder: 1);

		var slot2 = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services,
															  _tenantId,
															  new TimeOnly(9, 0),
															  new TimeOnly(9, 45),
															  sortOrder: 2);

		var teacher = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId, "Anders Andersen");
		var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId);
		var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId);

		await UpsertSlotAsync(klass.Id, schema.Id, slot1.Id, weekday: DayOfWeek.Monday, course.Id, teacher.Id);

		var response = await UpsertSlotAsync(klass.Id,
											 schema.Id,
											 slot2.Id,
											 weekday: DayOfWeek.Monday,
											 course.Id,
											 teacher.Id);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

		var result = await response.Content.ReadFromJsonAsync<SlotsAndConflictsDto>();
		await Assert.That(result!.Conflicts.Count).IsEqualTo(0);
	}

	[Test]
	public async Task TeacherDoubleBooked_WhenAssignedToTwoOverlappingSlots()
	{
		var slot1 = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services,
															  _tenantId,
															  new TimeOnly(10, 0),
															  new TimeOnly(10, 45),
															  sortOrder: 3);

		var slot2 = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services,
															  _tenantId,
															  new TimeOnly(10, 0),
															  new TimeOnly(10, 45),
															  sortOrder: 4);

		var teacher = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId, "Birthe Bjerg");
		var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId, "Matematik");
		var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId, "3.a");

		await UpsertSlotAsync(klass.Id, schema.Id, slot1.Id, weekday: DayOfWeek.Tuesday, course.Id, teacher.Id);

		var response = await UpsertSlotAsync(klass.Id,
											 schema.Id,
											 slot2.Id,
											 weekday: DayOfWeek.Tuesday,
											 course.Id,
											 teacher.Id);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

		var result = await response.Content.ReadFromJsonAsync<SlotsAndConflictsDto>();
		await Assert.That(result!.Conflicts.Count).IsGreaterThan(0);

		var conflict = result.Conflicts.First();
		await Assert.That(conflict.Type).IsEqualTo(ConflictType.TeacherDoubleBooked);
		await Assert.That(conflict.ResourceId).IsEqualTo(teacher.Id);
	}

	[Test]
	public async Task RoomDoubleBooked_WhenSameRoomAssignedToOverlappingSlots()
	{
		var slot1 = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services,
															  _tenantId,
															  new TimeOnly(11, 0),
															  new TimeOnly(11, 45),
															  sortOrder: 5);

		var slot2 = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services,
															  _tenantId,
															  new TimeOnly(11, 0),
															  new TimeOnly(11, 45),
															  sortOrder: 6);

		var teacher1 = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId, "Carl Carlsen");
		var teacher2 = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId, "Dorte Dam");
		var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId, "Idræt");
		var room = await CreateRoomAsync("Sportshal");
		var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId, "4.b");

		await UpsertSlotAsync(klass.Id,
							  schema.Id,
							  slot1.Id,
							  weekday: DayOfWeek.Wednesday,
							  course.Id,
							  teacher1.Id,
							  roomId: room.Id);

		var response = await UpsertSlotAsync(klass.Id,
											 schema.Id,
											 slot2.Id,
											 weekday: DayOfWeek.Wednesday,
											 course.Id,
											 teacher2.Id,
											 roomId: room.Id);

		var result = await response.Content.ReadFromJsonAsync<SlotsAndConflictsDto>();

		await Assert.That(result!.Conflicts.Any(c => c.Type == ConflictType.RoomDoubleBooked)).IsTrue();
		await Assert.That(result.Conflicts.First(c => c.Type == ConflictType.RoomDoubleBooked).ResourceId)
					.IsEqualTo(room.Id);
	}

	[Test]
	public async Task MarkComplete_Returns422_WhenConflictsExist()
	{
		var slot1 = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services,
															  _tenantId,
															  new TimeOnly(13, 0),
															  new TimeOnly(13, 45),
															  sortOrder: 7);

		var slot2 = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services,
															  _tenantId,
															  new TimeOnly(13, 0),
															  new TimeOnly(13, 45),
															  sortOrder: 8);

		var teacher = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId, "Erik Eriksen");
		var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId, "Engelsk");
		var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId, "5.c");

		await UpsertSlotAsync(klass.Id, schema.Id, slot1.Id, weekday: DayOfWeek.Thursday, course.Id, teacher.Id);
		await UpsertSlotAsync(klass.Id, schema.Id, slot2.Id, weekday: DayOfWeek.Thursday, course.Id, teacher.Id);

		var response = await _client.PostAsync(
						   $"/api/v1/classes/{klass.Id}/schemas/{schema.Id}/complete",
						   null);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
	}

	[Test]
	public async Task MarkComplete_Returns200_WhenNoConflicts()
	{
		var slot1 = await TestDataBuilder.CreateTimeSlotAsync(_factory.Services,
															  _tenantId,
															  new TimeOnly(14, 0),
															  new TimeOnly(14, 45),
															  sortOrder: 9);

		var teacher = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId, "Frede Frederiksen");
		var course = await TestDataBuilder.CreateCourseAsync(_factory.Services, _tenantId, "Geografi");
		var (klass, schema) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId, "6.a");

		await UpsertSlotAsync(klass.Id, schema.Id, slot1.Id, weekday: DayOfWeek.Monday, course.Id, teacher.Id);

		var response = await _client.PostAsync(
						   $"/api/v1/classes/{klass.Id}/schemas/{schema.Id}/complete",
						   null);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
	}

	private async Task<HttpResponseMessage> UpsertSlotAsync(
		Guid classId, Guid schemaId, Guid timeSlotId, DayOfWeek weekday,
		Guid courseId, Guid teacherId, Guid? roomId = null, Guid? aideId = null)
	{
		return await _client.PutAsJsonAsync(
				   $"/api/v1/classes/{classId}/schemas/{schemaId}/slots",
				   new { timeSlotId, weekday, courseId, teacherId, roomId, aideId });
	}

	private async Task<RoomsController.RoomDto> CreateRoomAsync(string name)
	{
		var response = await _client.PostAsJsonAsync("/api/v1/rooms",
													 new RoomsController.UpsertRoomRequest(name, null, null));

		response.EnsureSuccessStatusCode();
		return (await response.Content.ReadFromJsonAsync<RoomsController.RoomDto>())!;
	}

	// Local DTOs for deserializing the response — avoids coupling to internal controller types
	private record SlotsAndConflictsDto(
		IReadOnlyList<SlotDto> Slots,
		IReadOnlyList<ConflictInfoDto> Conflicts);

	private record SlotDto(
		Guid Id,
		Guid TimeSlotId,
		DayOfWeek Weekday,
		Guid CourseId,
		string CourseName,
		Guid TeacherId,
		string TeacherName,
		Guid? RoomId,
		string? RoomName,
		Guid? AideId,
		string? AideName);

	private record ConflictInfoDto(
		ConflictType Type,
		Guid SlotAId,
		Guid SlotBId,
		Guid ResourceId,
		string ResourceName,
		DayOfWeek Weekday,
		TimeOnly StartTime,
		TimeOnly EndTime);
}
