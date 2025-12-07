using System.Net.Mail;

namespace Application.Common.Models;

public sealed record EmailMessage(
    string? Body,
    bool IsBodyHtml,
    string Subject,
    MailAddress To,
    MailAddress? From = default)
{
}
