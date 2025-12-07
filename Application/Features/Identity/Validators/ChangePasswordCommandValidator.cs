namespace Application.Features.Identity.Validators;

internal class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.Info.CurrentPassword)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.Identity.CurrentPasswordRequired);

        RuleFor(x => x.Info.NewPassword)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.Identity.NewPasswordRequired);
    }
}
