using System.Security.Claims;

namespace Application.Common.Interfaces.Services;

public interface ICurrentUserService
{
    int GetId();

    string GetUsername();

    string GetEmail();

    bool IsCurrentUser(int userId);

    ClaimsPrincipal? GetUser();
}
