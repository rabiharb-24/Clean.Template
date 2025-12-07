using System.ComponentModel.DataAnnotations;

namespace Application.Configuration;

public sealed record UrlsConfiguration
{
    public const string SectionName = Constants.Configuration.Sections.Urls;

    [Required]
    public string WebUrl { get; init; } = string.Empty;

    [Required]
    public string Authority { get; init; } = string.Empty;
}