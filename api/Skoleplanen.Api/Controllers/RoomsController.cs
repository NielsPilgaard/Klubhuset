using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Domain;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/rooms")]
[Authorize]
public sealed class RoomsController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    public record RoomDto(Guid Id, string Name, int? Capacity, string? Description);
    public record UpsertRoomRequest(
        [Required][StringLength(100, MinimumLength = 1)] string Name,
        int? Capacity,
        string? Description);

    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetAll(CancellationToken ct)
    {
        var rooms = await db.Rooms
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoomDto(r.Id, r.Name, r.Capacity, r.Description))
            .ToListAsync(ct);
        return Ok(rooms);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomDto>> GetById(Guid id, CancellationToken ct)
    {
        var room = await db.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (room is null) return NotFound();
        return Ok(new RoomDto(room.Id, room.Name, room.Capacity, room.Description));
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<RoomDto>> Create([FromBody] UpsertRoomRequest req, CancellationToken ct)
    {
        var room = new Room
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            Name = req.Name,
            Capacity = req.Capacity,
            Description = req.Description,
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = room.Id },
            new RoomDto(room.Id, room.Name, room.Capacity, room.Description));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<RoomDto>> Update(Guid id, [FromBody] UpsertRoomRequest req, CancellationToken ct)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (room is null) return NotFound();
        if (room.TenantId != tenant.TenantId) return NotFound();
        room.Name = req.Name;
        room.Capacity = req.Capacity;
        room.Description = req.Description;
        await db.SaveChangesAsync(ct);
        return Ok(new RoomDto(room.Id, room.Name, room.Capacity, room.Description));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var room = await db.Rooms
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId, ct);
        if (room is null) return NotFound();
        db.Rooms.Remove(room);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
