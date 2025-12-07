using System.Linq.Expressions;
using Application.Common.Models.Responses;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Application.Common.Interfaces.Repositories;

public interface IIdentityRepository
{
    Task<LoginResponse> LoginAsync(LoginParametersDto model, CancellationToken cancellationToken);

    Task<LoginResponse> LoginCallback(string code, string state, CancellationToken cancellationToken);

    Task<LoginResponse> GetAccessTokenAsync(LoginParametersDto model, CancellationToken cancellationToken);

    Task<AuthenticateResponse> RefreshTokenAsync(UserCredentials userCredentials, CancellationToken cancellationToken);

    Task<ApplicationUser> GetUserAsync(int userId, CancellationToken cancellationToken);

    Task<bool> IsInRoleAsync(int userId, string role, CancellationToken cancellationToken);

    Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, CancellationToken cancellationToken);

    Task<IdentityResult> UpdateUserAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken);

    Task<bool> DeleteUserAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<ApplicationUser> FindByNameOrEmailAsync(string emailOrUsername, CancellationToken cancellationToken);

    Task<ApplicationUser> FindByNameAsync(string username, CancellationToken cancellationToken);

    Task<ApplicationUser> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token, CancellationToken cancellationToken);

    Task<bool> ValidatePasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken);

    Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword, CancellationToken cancellationToken);

    Task<IdentityResult> RemovePasswordAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<IdentityResult> AddPasswordAsync(ApplicationUser user, string newPassword, CancellationToken cancellationToken);

    Task<bool> VerifyUserTokenAsync(ApplicationUser user, string purpose, string token, CancellationToken cancellationToken);

    Task<bool> EmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken);

    Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword, CancellationToken cancellationToken);

    Task<IReadOnlyList<IdentityUserRole<int>>> GetUserRolesAsync(Expression<Func<IdentityUserRole<int>, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IdentityUserClaim<int>>> GetUserClaimsAsync(Expression<Func<IdentityUserClaim<int>, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<ApplicationRole> GetRoleAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationRole>> GetRolesAsync(Expression<Func<ApplicationRole, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IdentityRoleClaim<int>>> GetRoleClaimsAsync(Expression<Func<IdentityRoleClaim<int>, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<IdentityResult> AssignUserRolesAsync(int userId, IEnumerable<int> userRoleIds, CancellationToken cancellationToken);

    Task<IdentityResult> AssignUserClaimsAsync(int userId, IEnumerable<IdentityUserClaim<int>> claims, CancellationToken cancellationToken);

    Task<IdentityResult> RemoveUserRolesAsync(int userId, CancellationToken cancellationToken);

    Task<IdentityResult> RemoveUserClaimsAsync(int userId, CancellationToken cancellationToken);

    Task<ApplicationRole> GetUserRoleAsync(int userId, CancellationToken cancellationToken = default);

    Task ResetAuthenticatorKeyAsync(ApplicationUser user);

    Task<string?> GetAuthenticatorKeyAsync(ApplicationUser user);

    Task SetTwoFactorEnabled(ApplicationUser user, bool enabled);

    Task<bool> VerifyTwoFactorTokenAsync(ApplicationUser user, string code);
}
