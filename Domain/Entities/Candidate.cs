using Domain.Entities.Abstraction;
using Domain.Static;

namespace Domain.Entities;

public class Candidate : EntityBase, IAuditableEntity
{
    public string FullName { get; set; } = string.Empty;

    public DateTime? Birthdate { get; set; }

    public Gender? Gender { get; set; }

    public MaritalStatus? MaritalStatus { get; set; }

    public string? NationalityCode { get; set; }

    public string? CountryCode { get; set; }

    public string? CityCode { get; set; }

    public int? ProfilePictureId { get; set; }

    public int UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastModifiedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public string? LastModifiedBy { get; set; }
}
