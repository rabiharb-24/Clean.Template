namespace Application.Features.Identity.Validators;

internal class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Info.Token)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.Identity.InvalidEmailoken);
    }
}
