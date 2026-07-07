using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Skoleoverblikket.Api.Controllers;
using Skoleoverblikket.Api.IntegrationTests.Infrastructure;

namespace Skoleoverblikket.Api.IntegrationTests;

/// <summary>
/// Tests for time slot template upsert — specifically the break validation
/// that checks breaks land on module boundaries after accounting for earlier
/// breaks shifting subsequent module start times.
/// </summary>
[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
public sealed class TimeSlotTemplateTests(ApiFactory factory)
{
	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		Converters = { new JsonStringEnumConverter() },
		PropertyNameCaseInsensitive = true,
	};

	private readonly ApiFactory _factory = factory;
	private readonly Guid _tenantId = Guid.NewGuid();
	private HttpClient _client = null!;
	private Guid _classId;

	[Before(HookType.Class)]
	public async Task SetUp()
	{
		await TestDataBuilder.CreateSchoolAsync(_factory.Services, _tenantId);
		_client = _factory.CreateClient();
		_client.DefaultRequestHeaders.Add("X-Test-TenantId", _tenantId.ToString());

		// Create a class so we can query the school-level slot fallback
		var (klass, _) = await TestDataBuilder.CreateClassWithSchemaAsync(_factory.Services, _tenantId);
		_classId = klass.Id;
	}

	private async Task<List<TimeSlotsController.TimeSlotDto>> GetSchoolSlotsAsync()
	{
		var response = await _client.GetAsync($"/api/v1/classes/{_classId}/time-slots");
		response.EnsureSuccessStatusCode();
		return (await response.Content.ReadFromJsonAsync<List<TimeSlotsController.TimeSlotDto>>(JsonOpts))!;
	}

	[Test]
	public async Task UpsertTemplate_NoBreaks_GeneratesContiguousLessonSlots()
	{
		// Day 08:00–10:30, 45-min lessons → 3 slots: 08:00, 08:45, 09:30
		var req = new TimeSlotsController.UpsertTemplateRequest(
			LessonDurationMinutes: 45,
			DayStartTime: new TimeOnly(8, 0),
			DayEndTime: new TimeOnly(10, 30),
			ActiveDays: "MTWHF",
			Breaks: []);

		var response = await _client.PutAsJsonAsync("/api/v1/time-slot-template", req);
		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

		var slots = await GetSchoolSlotsAsync();
		var lessonSlots = slots.Where(s => !s.IsBreak).OrderBy(s => s.SortOrder).ToList();

		await Assert.That(lessonSlots.Count).IsEqualTo(3);
		await Assert.That(lessonSlots[0].StartTime).IsEqualTo(new TimeOnly(8, 0));
		await Assert.That(lessonSlots[0].EndTime).IsEqualTo(new TimeOnly(8, 45));
		await Assert.That(lessonSlots[1].StartTime).IsEqualTo(new TimeOnly(8, 45));
		await Assert.That(lessonSlots[2].StartTime).IsEqualTo(new TimeOnly(9, 30));
	}

	[Test]
	public async Task UpsertTemplate_BreakOnModuleBoundary_Returns200AndInsertsBreakSlot()
	{
		// Day 08:00–11:00, 45-min lessons, break at 08:45 (exact boundary after module 1)
		var req = new TimeSlotsController.UpsertTemplateRequest(
			LessonDurationMinutes: 45,
			DayStartTime: new TimeOnly(8, 0),
			DayEndTime: new TimeOnly(11, 0),
			ActiveDays: "MTWHF",
			Breaks: [new TimeSlotsController.UpsertBreakRequest(new TimeOnly(8, 45), 15)]);

		var response = await _client.PutAsJsonAsync("/api/v1/time-slot-template", req);
		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

		var slots = await GetSchoolSlotsAsync();
		var breakSlot = slots.SingleOrDefault(s => s.IsBreak);

		await Assert.That(breakSlot).IsNotNull();
		await Assert.That(breakSlot!.StartTime).IsEqualTo(new TimeOnly(8, 45));
		await Assert.That(breakSlot.EndTime).IsEqualTo(new TimeOnly(9, 0));
	}

	[Test]
	public async Task UpsertTemplate_BreakShiftsSubsequentModuleStart_LessonsResumeAfterBreak()
	{
		// Day 08:00–11:00, 45-min lessons, break 08:45–09:00 (15 min)
		// Module sequence: 08:00–08:45, [pause 08:45–09:00], 09:00–09:45, 09:45–10:30
		var req = new TimeSlotsController.UpsertTemplateRequest(
			LessonDurationMinutes: 45,
			DayStartTime: new TimeOnly(8, 0),
			DayEndTime: new TimeOnly(11, 0),
			ActiveDays: "MTWHF",
			Breaks: [new TimeSlotsController.UpsertBreakRequest(new TimeOnly(8, 45), 15)]);

		await _client.PutAsJsonAsync("/api/v1/time-slot-template", req);

		var slots = await GetSchoolSlotsAsync();
		var lessonSlots = slots.Where(s => !s.IsBreak).OrderBy(s => s.SortOrder).ToList();

		// Lesson after break must start at 09:00, not 08:45
		await Assert.That(lessonSlots[1].StartTime).IsEqualTo(new TimeOnly(9, 0));
		await Assert.That(lessonSlots[1].EndTime).IsEqualTo(new TimeOnly(9, 45));
	}

	[Test]
	public async Task UpsertTemplate_BreakMidModule_Returns422()
	{
		// Day 08:00–11:00, 45-min lessons, break at 08:30 (middle of first module 08:00–08:45)
		var req = new TimeSlotsController.UpsertTemplateRequest(
			LessonDurationMinutes: 45,
			DayStartTime: new TimeOnly(8, 0),
			DayEndTime: new TimeOnly(11, 0),
			ActiveDays: "MTWHF",
			Breaks: [new TimeSlotsController.UpsertBreakRequest(new TimeOnly(8, 30), 15)]);

		var response = await _client.PutAsJsonAsync("/api/v1/time-slot-template", req);
		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
	}

	[Test]
	public async Task UpsertTemplate_BreakBeforeDayStart_Returns422()
	{
		var req = new TimeSlotsController.UpsertTemplateRequest(
			LessonDurationMinutes: 45,
			DayStartTime: new TimeOnly(8, 0),
			DayEndTime: new TimeOnly(11, 0),
			ActiveDays: "MTWHF",
			Breaks: [new TimeSlotsController.UpsertBreakRequest(new TimeOnly(7, 30), 15)]);

		var response = await _client.PutAsJsonAsync("/api/v1/time-slot-template", req);
		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
	}

	[Test]
	public async Task UpsertTemplate_TwoBreaks_SecondBreakBoundaryShiftedByFirst_Returns200()
	{
		// Day 08:00–12:00, 45-min lessons
		// Break 1 at 08:45 (15 min) → next module starts 09:00
		// Break 2 at 09:45 (exact boundary of module 09:00–09:45) → valid
		var req = new TimeSlotsController.UpsertTemplateRequest(
			LessonDurationMinutes: 45,
			DayStartTime: new TimeOnly(8, 0),
			DayEndTime: new TimeOnly(12, 0),
			ActiveDays: "MTWHF",
			Breaks:
			[
				new TimeSlotsController.UpsertBreakRequest(new TimeOnly(8, 45), 15),
				new TimeSlotsController.UpsertBreakRequest(new TimeOnly(9, 45), 10),
			]);

		var response = await _client.PutAsJsonAsync("/api/v1/time-slot-template", req);
		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

		var slots = await GetSchoolSlotsAsync();
		await Assert.That(slots.Count(s => s.IsBreak)).IsEqualTo(2);
	}

	[Test]
	public async Task UpsertTemplate_TwoBreaks_SecondBreakMidModuleAfterFirstShift_Returns422()
	{
		// Day 08:00–12:00, 45-min lessons
		// Break 1 at 08:45 (15 min) → next module starts 09:00
		// Break 2 at 09:30 = mid-module of 09:00–09:45 → invalid
		var req = new TimeSlotsController.UpsertTemplateRequest(
			LessonDurationMinutes: 45,
			DayStartTime: new TimeOnly(8, 0),
			DayEndTime: new TimeOnly(12, 0),
			ActiveDays: "MTWHF",
			Breaks:
			[
				new TimeSlotsController.UpsertBreakRequest(new TimeOnly(8, 45), 15),
				new TimeSlotsController.UpsertBreakRequest(new TimeOnly(9, 30), 10),
			]);

		var response = await _client.PutAsJsonAsync("/api/v1/time-slot-template", req);
		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
	}

	[Test]
	public async Task UpsertTemplate_BreakAtExactDayEndBoundary_Returns200()
	{
		// Day 08:00–10:15, 45-min lessons → 3 full modules: 08:00–08:45, 08:45–09:30, 09:30–10:15
		// Break at 09:30 = exact end of second module boundary, still valid
		var req = new TimeSlotsController.UpsertTemplateRequest(
			LessonDurationMinutes: 45,
			DayStartTime: new TimeOnly(8, 0),
			DayEndTime: new TimeOnly(10, 15),
			ActiveDays: "MTWHF",
			Breaks: [new TimeSlotsController.UpsertBreakRequest(new TimeOnly(9, 30), 15)]);

		var response = await _client.PutAsJsonAsync("/api/v1/time-slot-template", req);
		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
	}
}
