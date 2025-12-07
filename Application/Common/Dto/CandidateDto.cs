using Domain.Entities;
using Domain.Entities.Identity;

namespace Application.Common.Dto;

public sealed class CandidateDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public string FullName { get; set; } = string.Empty;

    public Gender? Gender { get; set; }

    public DateTime? Birthdate { get; set; }

    public MaritalStatus? MaritalStatus { get; set; }

    public string? NationalityCode { get; set; }

    public string? OtherNationalityCode { get; set; }

    public int NumberOfDependents { get; set; } = 0;

    public string? CountryCode { get; set; }

    public string? CityCode { get; set; }

    public string? Location { get; set; }

    public int? ResumeId { get; set; }

    public string? ResumeUploadedDescription { get; set; }

    public int? ProfilePictureId { get; set; }

    public string? ProfilePictureBase64 { get; set; }

    public string? LatestExperience { get; set; }

    public string? LatestEducation { get; set; }

    public string? ExtraInfo { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastModifiedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public string? LastModifiedBy { get; set; }

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Candidate, CandidateDto>();
            CreateMap<CandidateDto, Candidate>();
        }
    }
}
