namespace DeliverX.Domain.Entities;

public class RolePermission
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty; // DP, DPCM, DBC, EC, Inspector, SuperAdmin
    public Guid PermissionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Permission Permission { get; set; } = null!;
}
