using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models;

public abstract class AuditableEntity
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
