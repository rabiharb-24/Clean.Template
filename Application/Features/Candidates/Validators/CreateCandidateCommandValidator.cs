namespace Application.Features.Candidates.Validators;

public class CreateCandidateCommandValidator : AbstractValidator<CreateCandidateCommand>
{
    public CreateCandidateCommandValidator()
    {
    }

    public override Task<ValidationResult> ValidateAsync(ValidationContext<CreateCandidateCommand> context, CancellationToken cancellation = default)
    {
        RuleFor(x => x.Candidate.Birthdate)
           .NotNull()
           .WithMessage(Constants.ValidationMessages.Candidate.BirthdateRequired);

        RuleFor(x => x.Candidate.Gender)
           .NotNull()
           .WithMessage("GenderRequired");

        RuleFor(x => x.Candidate.NationalityCode)
           .NotEmpty()
           .WithMessage("NationalityRequired");

        RuleFor(x => x.Candidate.CountryCode)
          .NotEmpty()
          .WithMessage("CountryRequired");

        RuleFor(x => x.Candidate.CityCode)
          .NotEmpty()
          .WithMessage("CityRequired");

        return base.ValidateAsync(context, cancellation);
    }
}
