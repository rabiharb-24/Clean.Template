namespace Application.Common.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken);
}
