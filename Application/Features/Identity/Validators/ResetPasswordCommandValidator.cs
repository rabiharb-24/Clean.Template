namespace Application.Features.Identity.Validators;

internal class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.ResetPasswordDto.ResetToken)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.Identity.InvalidResetToken);

        RuleFor(x => x.ResetPasswordDto.NewPassword)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.Identity.InvalidNewPassword);
    }
}
