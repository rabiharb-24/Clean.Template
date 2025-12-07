namespace Application.Common.Interfaces.Services;

public interface IReCaptchaService
{
    Task<bool> ValidateReCaptchaTokenAsync(string token);
}