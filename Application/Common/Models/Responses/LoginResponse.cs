namespace Application.Common.Models.Responses;

public class LoginResponse : GeneralResponse
{
    public string AuthUrl { get; init; } = string.Empty;

    public string IdToken { get; init; } = string.Empty;

    public string Token { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public bool TwoFactorAuthEnabled { get; init; }

    public string TwoFactorQrCode { get; init; } = string.Empty;

    public LoginResponse(bool success, int statusCode, Error? error = null)
    {
        Success = success;
        StatusCode = statusCode;
        Error = error;
    }
}
