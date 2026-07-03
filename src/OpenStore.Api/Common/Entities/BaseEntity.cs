using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenStore.Api.Common.Entities;

public abstract class BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; internal set; }

    [Required]
    public Guid PublicId { get; internal set; }

    public DateTimeOffset CreatedAtUtc { get; internal set; }

    public DateTimeOffset? UpdatedAtUtc { get; internal set; }

    public bool IsActive { get; internal set; } = true;

    public DateTimeOffset? DeletedAtUtc { get; internal set; }

    public void MarkAsDeleted(DateTimeOffset utcNow)
    {
        IsActive = false;
        DeletedAtUtc = utcNow;
    }

    public void Restore()
    {
        IsActive = true;
        DeletedAtUtc = null;
    }
}
