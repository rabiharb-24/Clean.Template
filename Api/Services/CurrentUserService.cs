using System.Security.Claims;
using Application.Common.Interfaces.Services;
using static Domain.Static.Constants;

namespace Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public int GetId()
    {
        var claimValue = httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

        if (int.TryParse(claimValue, out var id))
        {
            return id;
        }

        return default;
    }

    public string GetUsername()
    {
        return httpContextAccessor.HttpContext?.User?.FindFirstValue(IdentityApi.ApiClaims.Username) ?? string.Empty;
    }

    public string GetEmail()
    {
        return httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    }

    public bool IsCurrentUser(int userId)
    {
        var currentUser = GetId();

        return currentUser != default && currentUser == userId;
    }

    public ClaimsPrincipal? GetUser()
    {
        return httpContextAccessor.HttpContext?.User;
    }
}
