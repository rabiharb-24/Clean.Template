namespace Application.Common.Dto;

public class GetAuthenticatorResponseDto
{
    public string? AuthenticatorUri { get; init; }

    public string? TwoFactorSetupKey { get; init; }
}
