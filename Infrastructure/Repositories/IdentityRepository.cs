using System.Linq.Expressions;
using System.Net.Mail;
using Duende.IdentityModel.Client;
using Application.Common.Dto;
using Application.Common.Models;
using Application.Common.Models.Responses;
using Application.Configuration;
using Domain.Entities.Identity;
using Infrastructure.Extensions;
using Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using static Domain.Static.Constants;

namespace Infrastructure.Repositories;

public sealed class IdentityRepository : IIdentityRepository
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly RoleManager<ApplicationRole> roleManager;
    private readonly ApplicationDbContext context;
    private readonly UrlsConfiguration urlConfiguration;
    private readonly IUnitOfWork unitOfWork;
    private readonly HttpClient httpClient;
    private readonly IEmailService emailService;
    private readonly IdentityApiConfiguration identityOptions;
    private readonly IConfiguration configuration;

    public IdentityRepository(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        IEmailService emailService,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IOptions<IdentityApiConfiguration> identityOptions,
        IOptions<UrlsConfiguration> urlConfiguration)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.roleManager = roleManager;
        this.context = context;
        this.urlConfiguration = urlConfiguration.Value;
        this.unitOfWork = unitOfWork;
        httpClient = httpClientFactory.CreateClient();
        this.identityOptions = identityOptions.Value;
        this.emailService = emailService;
        this.configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginParametersDto model, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(model.Username);

        // Step 1: Validate User
        var validateResult = await ValidateUserAsync(user, model);
        if (!validateResult.Success)
        {
            return new LoginResponse(false, StatusCodes.Status400BadRequest, validateResult.Error);
        }

        // Step 2: Check Two Factor Authentication
        if (model.Verify2FA)
        {
            var result = await TwoFactorSignInUserAsync(user!, model);
            if (!result)
            {
                return new LoginResponse(false, StatusCodes.Status400BadRequest, new("InvalidCode"));
            }
        }
        else
        {
            // Step 3: Sign in user to check if 2FA is required
            SignInResult signinResult = await signInManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: false, lockoutOnFailure: false);

            // Step 4: If 2FA is required, generate and send the code
            if (signinResult.RequiresTwoFactor)
            {
                if (user!.TwoFactorType == TwoFactorTypes.Email && !await GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, cancellationToken))
                {
                    return new LoginResponse(false, StatusCodes.Status400BadRequest, new("CannotGenerate2faCode"));
                }

                return new LoginResponse(true, StatusCodes.Status200OK) { TwoFactorAuthEnabled = true };
            }

            if (!signinResult.Succeeded)
            {
                return new LoginResponse(false, StatusCodes.Status401Unauthorized);
            }
        }

        var client = identityOptions.Clients.ElementAt(1);

        Dictionary<string, string?> parameters = new()
        {
            [Constants.IdentityApi.Endpoints.Parameters.ClientId] = client.ClientId,
            [Constants.IdentityApi.Endpoints.Parameters.ClientSecret] = client.ClientSecrets.ElementAt(0).Value,
            [Constants.IdentityApi.Endpoints.Parameters.RedirectUri] = client.RedirectUris.ElementAt(0),
            [Constants.IdentityApi.Endpoints.Parameters.ResponseType] = "code",
            [Constants.IdentityApi.Endpoints.Parameters.Scope] = string.Join(" ", client.AllowedScopes),
            [Constants.IdentityApi.Endpoints.Parameters.State] = Guid.NewGuid().ToString("N"),
        };

        var uri = $"{urlConfiguration.Authority.TrimEnd('/')}{KnownEndpoints.AuthorizeEndpoint}";

        string authCodeFlowRedirectUrl = QueryHelpers.AddQueryString(uri, parameters);

        return new LoginResponse(true, StatusCodes.Status200OK) { AuthUrl = authCodeFlowRedirectUrl };
    }

    public async Task<LoginResponse> LoginCallback(string code, string state, CancellationToken cancellationToken)
    {
        var client = identityOptions.Clients.ElementAt(1);

        // Rquest ID token
        TokenResponse tokenResponse = await httpClient.RequestAuthorizationCodeTokenAsync(
            new AuthorizationCodeTokenRequest
        {
            Address = $"{urlConfiguration.Authority.TrimEnd('/')}{KnownEndpoints.TokenEndpoint}",
            ClientId = client.ClientId,
            ClientSecret = Constants.IdentityApi.Clients.Api.CodeFlowSecret,
            Code = code,
            RedirectUri = client.RedirectUris.ElementAt(0),
        }, cancellationToken: cancellationToken);

        if (tokenResponse is null || !string.IsNullOrEmpty(tokenResponse.Error) || string.IsNullOrEmpty(tokenResponse.IdentityToken))
        {
            await signInManager.SignOutAsync();
            return new LoginResponse(false, StatusCodes.Status401Unauthorized);
        }

        return new LoginResponse(true, StatusCodes.Status200OK) { IdToken = tokenResponse.IdentityToken };
    }

    public async Task<LoginResponse> GetAccessTokenAsync(LoginParametersDto model, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(model.Username);

        // Step 1: Validate User
        var validateResult = await ValidateUserAsync(user, model);
        if (!validateResult.Success)
        {
            return new LoginResponse(false, validateResult.StatusCode, validateResult.Error);
        }

        // Step 2: Check Two Factor Authentication
        var twoFactorResult = await HandleTwoFactorAuthenticationAsync(user!, model.Code, model.Verify2FA, cancellationToken);
        if (twoFactorResult is not null)
        {
            return new LoginResponse(twoFactorResult.Success, twoFactorResult.StatusCode, twoFactorResult.Error) { TwoFactorAuthEnabled = true };
        }

        // Step 3: Request access and refresh tokens
        HttpResponseMessage res = await httpClient.SendAsync(CreateAuthenticateRequest(model.Username, model.Password), cancellationToken);

        AuthenticateResponse? authenticationResult = (await res.Content.ReadAsStringAsync(cancellationToken))
            .DeserializeJsonString<AuthenticateResponse>() ?? throw new InvalidOperationException();

        return new LoginResponse(true, StatusCodes.Status200OK) { Token = authenticationResult.AccessToken, RefreshToken = authenticationResult.RefreshToken };
    }

    public async Task<AuthenticateResponse> RefreshTokenAsync(UserCredentials userCredentials, CancellationToken cancellationToken)
    {
        HttpResponseMessage res = await httpClient.SendAsync(CreateRefreshTokenRequest(userCredentials.Token), cancellationToken);
        AuthenticateResponse? authenticationResult = (await res.Content.ReadAsStringAsync(cancellationToken))
            .DeserializeJsonString<AuthenticateResponse>();

        return authenticationResult ?? throw new InvalidOperationException();
    }

    public async Task<ApplicationUser> GetUserAsync(int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());

        return user is null ? new ApplicationUser() { Id = default } : user;
    }

    public async Task<bool> EmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.IsEmailConfirmedAsync(user);
    }

    public async Task<ApplicationUser> FindByNameOrEmailAsync(string emailOrUsername, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(emailOrUsername))
        {
            return new ApplicationUser() { Id = default };
        }

        ApplicationUser user = await FindByNameAsync(emailOrUsername, cancellationToken);
        if (user.Id == default)
        {
            user = await FindByEmailAsync(emailOrUsername, cancellationToken);
        }

        return user;
    }

    public async Task<ApplicationUser> FindByNameAsync(string username, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(username))
        {
            return new ApplicationUser() { Id = default };
        }

        ApplicationUser? user = await userManager.FindByNameAsync(username);

        return user is null ? new ApplicationUser() { Id = default } : user;
    }

    public async Task<ApplicationUser> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(email))
        {
            return new ApplicationUser() { Id = default };
        }

        ApplicationUser? user = await userManager.FindByEmailAsync(email);

        return user is null ? new ApplicationUser() { Id = default } : user;
    }

    public async Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.ConfirmEmailAsync(user, token);
    }

    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (user is null || user.Email is null || user.UserName is null)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        IdentityResult result = await userManager.CreateAsync(user, password);
        return !result.Succeeded ? result : await userManager.SetLockoutEnabledAsync(user, false);
    }

    public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.UpdateAsync(user);
    }

    public async Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId == default)
        {
            return false;
        }

        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        return user is null || user == default || await DeleteUserAsync(user, cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser? existingUser = await userManager.FindByIdAsync(user.Id.ToString());

        return existingUser == default || (await userManager.DeleteAsync(user)).Succeeded;
    }

    public async Task<bool> IsInRoleAsync(int userId, string role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId == default || string.IsNullOrEmpty(role))
        {
            return false;
        }

        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());

        return user != default && await userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> ValidatePasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.ResetPasswordAsync(user, token, newPassword);
    }

    public async Task<IdentityResult> RemovePasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.RemovePasswordAsync(user);
    }

    public async Task<IdentityResult> AddPasswordAsync(ApplicationUser user, string newPassword, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.AddPasswordAsync(user, newPassword);
    }

    public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<bool> VerifyUserTokenAsync(ApplicationUser user, string purpose, string token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, purpose, token);
    }

    public async Task<IReadOnlyList<IdentityUserRole<int>>> GetUserRolesAsync(Expression<Func<IdentityUserRole<int>, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resultQueryable = filter is null ? context.UserRoles : context.UserRoles.Where(filter);

        var resultSet = await resultQueryable.ToListAsync(cancellationToken);

        return resultSet is null ? [] : resultSet;
    }

    public async Task<IReadOnlyList<IdentityUserClaim<int>>> GetUserClaimsAsync(Expression<Func<IdentityUserClaim<int>, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resultQueryable = filter is null ? context.UserClaims : context.UserClaims.Where(filter);

        var resultSet = await resultQueryable.ToListAsync(cancellationToken);

        return resultSet is null ? [] : resultSet;
    }

    public async Task<ApplicationRole> GetRoleAsync(string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(name))
        {
            return new ApplicationRole() { Id = default };
        }

        var role = await roleManager.Roles.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        return role is null ? new ApplicationRole() { Id = default } : role;
    }

    public async Task<IReadOnlyList<ApplicationRole>> GetRolesAsync(Expression<Func<ApplicationRole, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resultQueryable = filter is null ? roleManager.Roles : roleManager.Roles.Where(filter);

        var resultSet = await resultQueryable.ToListAsync(cancellationToken);

        return resultSet is null ? [] : resultSet;
    }

    public async Task<IReadOnlyList<IdentityRoleClaim<int>>> GetRoleClaimsAsync(Expression<Func<IdentityRoleClaim<int>, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resultQueryable = filter is null ? context.RoleClaims : context.RoleClaims.Where(filter);

        var resultSet = await resultQueryable.ToListAsync(cancellationToken);

        return resultSet is null ? [] : resultSet;
    }

    public async Task<IdentityResult> AssignUserRolesAsync(int userId, IEnumerable<int> roleIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser user = await GetUserAsync(userId, cancellationToken);
        if (user.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        if (roleIds is null || !roleIds.Any())
        {
            return IdentityResult.Success;
        }

        var identityRoles = roleIds.Select(x =>
        {
            return new ApplicationUserRole() { UserId = userId, RoleId = x };
        }) ?? [];

        await context.UserRoles.AddRangeAsync(identityRoles, cancellationToken);

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> AssignUserClaimsAsync(int userId, IEnumerable<IdentityUserClaim<int>> claims, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser user = await GetUserAsync(userId, cancellationToken);
        if (user.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        if (claims is null || !claims.Any())
        {
            return IdentityResult.Success;
        }

        await context.UserClaims.AddRangeAsync(claims, cancellationToken);

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> RemoveUserRolesAsync(int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser user = await GetUserAsync(userId, cancellationToken);
        if (user.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        var rolesToRemove = context.UserRoles.Where(x => x.UserId == userId);

        context.UserRoles.RemoveRange(rolesToRemove);

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> RemoveUserClaimsAsync(int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser user = await GetUserAsync(userId, cancellationToken);
        if (user.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        var claimsToRemove = context.UserClaims.Where(x => x.UserId == userId);

        context.UserClaims.RemoveRange(claimsToRemove);

        return IdentityResult.Success;
    }

    public async Task<ApplicationRole> GetUserRoleAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(x => x.UserId == userId, cancellationToken);
        if (userRoles.Count == 0)
        {
            return new ApplicationRole() { Id = default };
        }

        var roles = await GetRolesAsync(x => x.Id == userRoles[0].RoleId, cancellationToken);
        if (roles.Count == 0)
        {
            return new ApplicationRole() { Id = default };
        }

        return roles[0];
    }

    public async Task SetTwoFactorEnabled(ApplicationUser user, bool enabled)
    {
        await userManager.SetTwoFactorEnabledAsync(user, enabled);
    }

    public async Task ResetAuthenticatorKeyAsync(ApplicationUser user)
    {
        await userManager.ResetAuthenticatorKeyAsync(user);
    }

    public async Task<string?> GetAuthenticatorKeyAsync(ApplicationUser user)
    {
        return await userManager.GetAuthenticatorKeyAsync(user);
    }

    public async Task<bool> VerifyTwoFactorTokenAsync(ApplicationUser user, string code)
    {
        return await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);
    }

    private HttpRequestMessage CreateAuthenticateRequest(string username, string password)
    {
        return new(
        HttpMethod.Post,
        string.Concat(urlConfiguration.Authority, "/", IdentityApi.Endpoints.Routes.AccessToken))
        {
            Content = new FormUrlEncodedContent([
            new (IdentityApi.Endpoints.Parameters.GrantType, IdentityApi.Clients.Api.GrantTypes.Password),
            new (IdentityApi.Endpoints.Parameters.ClientId, IdentityApi.Clients.Api.Id),
            new (IdentityApi.Endpoints.Parameters.ClientSecret, IdentityApi.Clients.Api.Secret),
            new (IdentityApi.Endpoints.Parameters.Scope, IdentityApi.Clients.Api.DefaultScope),
            new (IdentityApi.Endpoints.Parameters.UserName, username),
            new (IdentityApi.Endpoints.Parameters.Password, password)]),
        };
    }

    private HttpRequestMessage CreateRefreshTokenRequest(string refreshToken)
    {
        return new(
        HttpMethod.Post,
        string.Concat(urlConfiguration.Authority, "/", IdentityApi.Endpoints.Routes.AccessToken))
        {
            Content = new FormUrlEncodedContent([
            new (IdentityApi.Endpoints.Parameters.GrantType, IdentityApi.Clients.Api.GrantTypes.RefreshToken),
            new (IdentityApi.Endpoints.Parameters.ClientId, IdentityApi.Clients.Api.Id),
            new (IdentityApi.Endpoints.Parameters.ClientSecret, IdentityApi.Clients.Api.Secret),
            new (IdentityApi.Endpoints.Parameters.RefreshToken, refreshToken)]),
        };
    }

    private async Task<GeneralResponse> ValidateUserAsync(ApplicationUser? user, LoginParametersDto model)
    {
        if (user is null)
        {
            return new GeneralResponse() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Error = new("WrongUser") };
        }

        if (!await userManager.CheckPasswordAsync(user, model.Password))
        {
            return new GeneralResponse() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Error = new("WrongUser") };
        }

        if (!await userManager.IsEmailConfirmedAsync(user))
        {
            return new GeneralResponse() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Error = new("UserConfirmError") };
        }

        return new GeneralResponse() { Success = true, StatusCode = StatusCodes.Status200OK };
    }

    /// <summary>
    /// Calls the appropriate sign-in method based on the 2FA type  (email or authenticator app).
    /// </summary>
    private async Task<bool> TwoFactorSignInUserAsync(ApplicationUser user, LoginParametersDto model)
    {
        SignInResult result;

        switch (user.TwoFactorType)
        {
            case TwoFactorTypes.Email:
                {
                    result = await signInManager.TwoFactorSignInAsync(TokenOptions.DefaultEmailProvider, model.Code, false, false);
                    break;
                }

            case TwoFactorTypes.AuthenticatorApp:
                {
                    var authenticationUser = await signInManager.GetTwoFactorAuthenticationUserAsync();
                    if (authenticationUser is null)
                    {
                        return false;
                    }

                    result = await signInManager.TwoFactorAuthenticatorSignInAsync(model.Code, false, false);
                    break;
                }

            default: return false;
        }

        if (result.Succeeded)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Generate two factor authentication code and send it to user email.
    /// </summary>
    private async Task<bool> GenerateTwoFactorTokenAsync(ApplicationUser user, string tokenProvider, CancellationToken cancellationToken)
    {
        try
        {
            string code = await userManager.GenerateTwoFactorTokenAsync(user, tokenProvider);

            if (!string.IsNullOrEmpty(user.Email))
            {
                var message = new EmailMessage($"The code is: {code}", true, "2FA Verification Code", new MailAddress(user.Email));
                await emailService.SendEmailAsync(message, cancellationToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message, ex);
        }

        return false;
    }

    /// <summary>
    /// Generate or verify two factor authentication code.
    /// </summary>
    private async Task<GeneralResponse?> HandleTwoFactorAuthenticationAsync(ApplicationUser user, string code, bool verify2FA, CancellationToken cancellationToken)
    {
        // Check if 2fa is enabled
        var enabled = await userManager.GetTwoFactorEnabledAsync(user);
        if (!enabled)
        {
            return null;
        }

        // Verify 2fa code
        if (verify2FA)
        {
            var verifyResult = await VerifyTwoFactorCodeAsync(user, code);
            if (verifyResult.Success)
            {
                return null;
            }

            return verifyResult;
        }

        // Generate and send 2fa code
        if (user.TwoFactorType == TwoFactorTypes.Email && !await GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, cancellationToken))
        {
            return new GeneralResponse() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Error = new Error("CannotGenerate2faCode") };
        }

        return new GeneralResponse() { Success = true, StatusCode = StatusCodes.Status200OK };
    }

    /// <summary>
    /// Verify two factor authentication code based on type.
    /// </summary>
    private async Task<GeneralResponse> VerifyTwoFactorCodeAsync(ApplicationUser user, string code)
    {
        if (code is null)
        {
            return new GeneralResponse() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Error = new Error("InvalidCode") };
        }

        switch (user.TwoFactorType)
        {
            case TwoFactorTypes.Email:
                {
                    var success = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, code);

                    return !success ? new GeneralResponse() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Error = new Error("InvalidCode") }
                                    : new GeneralResponse() { Success = true, StatusCode = StatusCodes.Status200OK };
                }

            case TwoFactorTypes.AuthenticatorApp:
                {
                    var isValid = await unitOfWork.IdentityRepository.VerifyTwoFactorTokenAsync(user, code);
                    if (!isValid)
                    {
                        return new GeneralResponse() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Error = new Error("InvalidCode") };
                    }

                    return new GeneralResponse() { Success = true, StatusCode = StatusCodes.Status200OK };
                }
        }

        return new GeneralResponse() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Error = new Error("InvalidCode") };
    }
}
