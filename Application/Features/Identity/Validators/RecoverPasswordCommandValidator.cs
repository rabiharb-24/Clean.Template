namespace Application.Features.Identity.Validators;

internal class RecoverPasswordCommandValidator : AbstractValidator<RecoverPasswordCommand>
{
    public RecoverPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage(Constants.ValidationMessages.Identity.InvalidEmail);
    }
}
