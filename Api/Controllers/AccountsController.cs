using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Common.Models.Responses;
using Application.Features.Identity.Commands;
using Application.Features.Identity.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData.Query;

namespace Api.Controllers;

[Authorize]
public sealed class AccountsController : BaseController
{
    public AccountsController(IMediator mediator, ICurrentUserService currentUserService)
        : base(mediator, currentUserService)
    {
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromBody] LoginParametersDto model, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new LoginCommand(model), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return result.Value.TwoFactorAuthEnabled ? Ok(result.Value) : Redirect(result.Value.AuthUrl);
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new LoginCallbackCommand(code, state), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [Route("logout")]
    public IActionResult Logout()
    {
        return SignOut(IdentityConstants.ApplicationScheme);
    }

    /// <summary>
    /// Authenticate user request.
    /// </summary>
    /// <param name="userCredentials">Authentication credentials (username + password).</param>
    /// <returns><see cref="LoginResponse"/> with a code indicating status of the operation.</returns>
    [AllowAnonymous]
    [HttpPost]
    [Route("access-token")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccessToken([FromBody] LoginParametersDto userCredentials, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAccessTokenQuery(userCredentials), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// replace expired user token.
    /// </summary>
    /// <param name="userCredentials">refresh token.</param>
    /// <returns><see cref="AuthenticateResponse"/> with a code indication status of the operation.</returns>
    [AllowAnonymous]
    [HttpPost]
    [Route("refresh-token")]
    [ProducesResponseType(typeof(AuthenticateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshAccessToken([FromBody] UserCredentials userCredentials, CancellationToken cancellationToken)
    {
        Result<AuthenticateResponse> result = await Mediator.Send(new RefreshAccessTokenQuery(userCredentials), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get current user.
    /// </summary>
    /// <returns>The user having type <see cref="ApplicationUserDto"/>.</returns>
    [HttpGet]
    [Route("user")]
    [ProducesResponseType(typeof(ApplicationUserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        Result<ApplicationUserDto> result = await Mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves a list of users using OData query options for filtering, sorting, and pagination.
    /// </summary>
    /// <param name="filterOptions">The OData query options to apply for filtering, sorting, and pagination.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="OdataResponse{UserInfo}"/> containing the filtered and paginated list of users.</returns>
    [HttpGet]
    [Route("users")]
    [ProducesResponseType(typeof(OdataResponse<ApplicationUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersOdata(ODataQueryOptions<ApplicationUserDto> filterOptions, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetUsersOdataQuery(filterOptions), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a new user with the provided user information.
    /// </summary>
    /// <param name="info">The data to create a new user, provided in the request body.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="AuthenticateResponse"/> containing the authentication details if the user is successfully created (HTTP 201).
    /// A <see cref="Error[]"/> containing validation or error details if the request is invalid (HTTP 400).
    /// </returns>
    [AllowAnonymous]
    [HttpPost]
    [Route("users")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto info, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateUserCommand(info), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// Updates the details of a specified user in the system.
    /// </summary>
    /// <param name="info">The data transfer object containing the updated user information.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the update operation.</returns>
    [HttpPut]
    [Route("users")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto info, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new UpdateUserCommand(info), cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(info.ApplicationUserDto.Id);
    }

    /// <summary>
    /// Deletes a user from the system based on the provided user ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to be deleted.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the delete operation.</returns>
    [HttpDelete]
    [Route("users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUser([FromRoute] int userId, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new DeleteUserCommand(userId), cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// Initiates the password recovery process for a user identified by their email address.
    /// </summary>
    /// <param name="email">The email address of the user requesting password recovery.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the password recovery request.</returns>
    [AllowAnonymous]
    [HttpPost]
    [Route("recover-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecoverPassword([FromRoute] string email, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new RecoverPasswordCommand(email), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// Resets the password for a user based on the provided reset information.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="info">The data transfer object containing the user's id, new password, and any necessary reset token.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the password reset operation.</returns>
    [AllowAnonymous]
    [HttpPatch]
    [Route("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromRoute] int userId, [FromBody] ResetPasswordDto info, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new ResetPasswordCommand(userId, info), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// Changes the password for a specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="info">The data transfer object containing the user's ID, current password, and the new password.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the password change operation.</returns>
    [HttpPatch]
    [Route("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword([FromRoute] int userId, [FromBody] ChangePasswordDto info, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new ChangePasswordCommand(userId, info), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// Sends a confirmation email to the user identified by the specified user ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="info">The data transfer object containing the user's ID to whom the confirmation email will be sent.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the email sending operation.</returns>
    [AllowAnonymous]
    [HttpPost]
    [Route("confirmation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendConfirmationEmail([FromRoute] int userId, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new SendConfirmationEmailCommand(userId), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// Confirms the email address of a user based on the provided confirmation information.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="info">The data transfer object containing the user's ID and the confirmation token.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the email confirmation operation.</returns>
    [AllowAnonymous]
    [HttpPatch]
    [Route("confirmation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmEmail([FromRoute] int userId, [FromBody] ConfirmEmailDto info, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new ConfirmEmailCommand(userId, info), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// Deactivates the current user account.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the account deactivation operation.</returns>
    [HttpPatch]
    [Route("deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivateAccount(CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new DeactivateAccountCommand(), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// Retrieves all available roles along with their respective claims.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult with a list of roles and their claims.</returns>
    [HttpGet]
    [Route("roles")]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        IEnumerable<RoleDto> roles = await Mediator.Send(new GetRolesQuery(), cancellationToken);

        return Ok(roles);
    }

    /// <summary>
    /// Checks if current user is in a specific role.
    /// </summary>
    /// <param name="roleName">The name of the role to check against the user.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating whether the user is in the specified role.</returns>
    [HttpGet]
    [Route("user-enroled")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> UserInRole([FromRoute] string roleName, CancellationToken cancellationToken)
    {
        bool result = await Mediator.Send(new UserInRoleQuery(roleName), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves the current user role.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult with the user role.</returns>
    [HttpGet]
    [Route("user-role")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRole(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetUserRoleQuery(), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Updates current user's profile.
    /// </summary>
    /// <param name="info">Profile info to update.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [HttpPut]
    [Route("profile")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] ApplicationUserDto info, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new UpdateProfileCommand(info), cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(info.Id);
    }

    /// <summary>
    /// Enables the authenticator app for the currently logged-in user.
    /// </summary>
    /// <remarks>
    /// This endpoint triggers the process of enabling two-factor authentication (2FA)
    /// by generating and returning the necessary setup information (e.g., key, QR code URL).
    /// It sends an <see cref="EnableAuthenticatorCommand"/> to the application layer
    /// via MediatR and returns the result of that operation.
    /// </remarks>
    /// <param name="cancellationToken">Token used to cancel the operation if needed.</param>
    /// <returns>
    /// A 200 OK response with the setup details if successful,
    /// or a 400 Bad Request with error details if the operation fails.
    /// </returns>
    [HttpPost]
    [Route("authenticator")]
    public async Task<IActionResult> EnableAuthenticator(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new EnableAuthenticatorCommand(), cancellationToken);

        return !result.Success ? StatusCode(result.StatusCode, result.Errors) : Ok(result.Value);
    }

    /// <summary>
    /// Verifies the authenticator setup for two-factor authentication (2FA).
    /// </summary>
    /// <remarks>
    /// This endpoint validates the one-time verification code provided by the user
    /// after scanning the authenticator QR code or entering the setup key.
    /// It sends a <see cref="VerifyAuthenticatorCommand"/> through MediatR to confirm
    /// that the authenticator configuration is correct and can be activated.
    /// </remarks>
    /// <param name="model">The verification data containing the code generated by the authenticator app.</param>
    /// <param name="cancellationToken">Token used to cancel the operation if needed.</param>
    /// <returns>
    /// A 200 OK response if the verification succeeds,
    /// or a 400 Bad Request with validation errors if the code is invalid or the operation fails.
    /// </returns>
    [HttpPost]
    [Route("authenticator-status")]
    public async Task<IActionResult> VerifyAuthenticator([FromBody] VerifyAuthenticatorDto model, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new VerifyAuthenticatorCommand(model), cancellationToken);

        return !result.Success ? StatusCode(result.StatusCode, result.Errors) : Ok();
    }
}