using System.Net;
using System.Net.Mail;
using Application.Common.Extensions;
using Application.Configuration;
using Microsoft.Extensions.Options;

namespace Application.Features.Identity.Notifications;

public sealed record ConfirmUserEmailNotification(string Email, int UserId, string Token) : INotification;

public class ConfirmUserEmailNotificationHandler : INotificationHandler<ConfirmUserEmailNotification>
{
    private readonly IEmailService emailService;
    private readonly UrlsConfiguration urlsConfiguration;

    public ConfirmUserEmailNotificationHandler(
        IEmailService emailService,
        IOptions<UrlsConfiguration> urlsConfiguration)
    {
        this.emailService = emailService;
        this.urlsConfiguration = urlsConfiguration.Value;
    }

    public async Task Handle(ConfirmUserEmailNotification notification, CancellationToken cancellationToken)
    {
        EmailMessage message = CreateEmailConfirmationMessage(notification.Email, notification.UserId, notification.Token);
        await emailService.SendEmailAsync(message, cancellationToken);
    }

    private EmailMessage CreateEmailConfirmationMessage(string emailTo, int userId, string token)
    {
        Dictionary<string, string> tokens = new()
        {
            {
                Constants.PlaceHolders.BaseUrl,
                urlsConfiguration.WebUrl.ToString().Trim('/') ?? string.Empty
            },
            { Constants.PlaceHolders.Token, WebUtility.UrlEncode(token) },
            { Constants.PlaceHolders.UserId, userId.ToString() },
        };

        return new EmailMessage(
            Constants.Localization.EmailConfirmationBody.ReplaceEmailTokens(tokens),
            true,
            Constants.Localization.EmailConfirmationSubject,
            new MailAddress(emailTo));
    }
}
