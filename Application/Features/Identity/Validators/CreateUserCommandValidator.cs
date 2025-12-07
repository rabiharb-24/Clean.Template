namespace Application.Features.Identity.Validators;

internal class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        _ = RuleFor(x => x.Info.ApplicationUserDto.Username)
           .NotEmpty()
           .WithMessage(Constants.ValidationMessages.Identity.UsernameRequired);

        _ = RuleFor(x => x.Info.ApplicationUserDto.FirstName)
          .NotEmpty()
          .WithMessage(Constants.ValidationMessages.Identity.FirstNameRequired);

        _ = RuleFor(x => x.Info.ApplicationUserDto.LastName)
          .NotEmpty()
          .WithMessage(Constants.ValidationMessages.Identity.LastNameRequired);

        _ = RuleFor(x => x.Info.ApplicationUserDto.Email)
            .EmailAddress()
            .WithMessage(Constants.ValidationMessages.Identity.InvalidEmail);

        _ = RuleFor(x => x.Info.ApplicationUserDto.Password)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.Identity.PasswordRequired);

        _ = RuleFor(x => x.Info.ApplicationUserDto.RoleName)
         .NotEmpty()
         .WithMessage(Constants.ValidationMessages.Identity.RoleRequired);
    }
}
