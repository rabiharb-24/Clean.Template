using Application.Configuration;

namespace Application.Common.Dto;

public sealed record ClientConfigurationDto
{
    public ReCaptchaConfiguration? ReCaptchaConfig { get; init; }

    public string? ApiUri { get; init; }

}