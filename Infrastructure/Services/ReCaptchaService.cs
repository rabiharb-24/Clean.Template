using Application.Common.Dto;
using Application.Configuration;
using Infrastructure.Extensions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class ReCaptchaService : IReCaptchaService
{
    public ReCaptchaService(IHttpClientFactory httpClientFactory, IOptions<ReCaptchaConfiguration> reCaptchaOptions)
    {
        httpClient = httpClientFactory.CreateClient();
        reCaptchaConfiguration = reCaptchaOptions.Value;
    }

    private readonly HttpClient httpClient;
    private readonly ReCaptchaConfiguration reCaptchaConfiguration;

    public async Task<bool> ValidateReCaptchaTokenAsync(string token)
    {
        var response = await httpClient.PostAsync(reCaptchaConfiguration.Url, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { Constants.ReCaptcha.Parameters.Secret, reCaptchaConfiguration.SecretKey },
                { Constants.ReCaptcha.Parameters.Response, token },
            }));

        var reCaptchaResponse = (await response.Content.ReadAsStringAsync())
            .DeserializeJsonString<ReCaptchaResponseDto>();

        return reCaptchaResponse?.Success ?? false;
    }
}
