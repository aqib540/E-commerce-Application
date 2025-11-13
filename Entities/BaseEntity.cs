using System.ComponentModel.DataAnnotations;

namespace E_commerce_Application.Entities;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }
}

