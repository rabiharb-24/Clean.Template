using System.Net;
using System.Net.Mail;
using Application.Common.Extensions;
using Application.Configuration;
using Microsoft.Extensions.Options;

namespace Application.Features.Identity.Notifications;

public sealed record ResetUserPasswordNotification(string Email, int UserId, string Token) : INotification;

public class ResetUserPasswordNotificationHandler : INotificationHandler<ResetUserPasswordNotification>
{
    private readonly IEmailService emailService;
    private readonly UrlsConfiguration urlsConfiguration;

    public ResetUserPasswordNotificationHandler(
    IEmailService emailService,
    IOptions<UrlsConfiguration> urlsConfiguration)
    {
        this.emailService = emailService;
        this.urlsConfiguration = urlsConfiguration.Value;
    }

    public async Task Handle(ResetUserPasswordNotification notification, CancellationToken cancellationToken)
    {
        EmailMessage message = CreateResetPasswordMessage(notification.Email, notification.UserId, notification.Token);
        await emailService.SendEmailAsync(message, cancellationToken);
    }

    private EmailMessage CreateResetPasswordMessage(string emailTo, int userId, string token)
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
            Constants.Localization.RecoverPasswordEmailBody.ReplaceEmailTokens(tokens),
            true,
            Constants.Localization.RecoverPasswordEmailSubject,
            new MailAddress(emailTo));
    }
}
