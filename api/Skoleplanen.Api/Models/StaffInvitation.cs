using System.ComponentModel.DataAnnotations;
using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Models;

public enum InvitationStatus
{
    Pending,
    Accepted,
    Expired,
}

public sealed class StaffInvitation : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;

    [StringLength(500)]
    public required string Email { get; set; }

    /// <summary>Cryptographically random token included in the invitation link.</summary>
    [StringLength(128)]
    public required string Token { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public InvitationStatus Status =>
        AcceptedAt.HasValue ? InvitationStatus.Accepted
        : DateTimeOffset.UtcNow > ExpiresAt ? InvitationStatus.Expired
        : InvitationStatus.Pending;
}
