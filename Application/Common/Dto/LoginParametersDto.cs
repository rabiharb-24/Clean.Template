namespace Application.Common.Dto;

public sealed record LoginParametersDto
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool Verify2FA { get; set; }

    public string Code { get; set; } = string.Empty;
}
