using Application.Common.Interfaces;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Identity.Commands;

public sealed record UpdateProfileCommand(ApplicationUserDto Info) : IRequest<Result>;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result>
{
    private readonly IMapper mapper;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;

    public UpdateProfileCommandHandler(
        IMapper mapper,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        this.mapper = mapper;
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsCurrentUser(request.Info.Id))
        {
            return Result.CreateFailure([Constants.Errors.Unauthorized]);
        }

        ApplicationUser oldUser = await unitOfWork.IdentityRepository.GetUserAsync(request.Info.Id, cancellationToken);
        if (oldUser.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        (bool success, string? error) = await ValidateUser(request.Info.Username, request.Info.Email, request.Info.Id, cancellationToken);
        if (!success)
        {
            return Result<string>.CreateFailure(error is not null ? [error] : []);
        }

        ApplicationUser updatedUser = mapper.Map<ApplicationUser>(request.Info);

        ApplyProfileUpdates(oldUser, updatedUser);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        // Handle 2FA toggling
        await UpdateTwoFactorSettingsAsync(oldUser, request.Info, cancellationToken);

        IdentityResult result = await unitOfWork.IdentityRepository.UpdateUserAsync(oldUser, cancellationToken);
        if (!result.Succeeded)
        {
            return Result.CreateFailure([result.Errors?.FirstOrDefault()?.Description ?? string.Empty]);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.CommitTransactionAsync(cancellationToken);

        return Result.CreateSuccess();
    }

    private static void ApplyProfileUpdates(ApplicationUser oldUser, ApplicationUser updatedUser)
    {
        string oldEmail = oldUser.Email;
        string newEmail = updatedUser.Email;

        oldUser.UserName = updatedUser.UserName;
        oldUser.FirstName = updatedUser.FirstName;
        oldUser.MiddleName = updatedUser.MiddleName;
        oldUser.LastName = updatedUser.LastName;
        oldUser.PhoneNumber = updatedUser.PhoneNumber;
        oldUser.Active = updatedUser.Active;

        if (oldEmail != newEmail)
        {
            // Save last confirmed email
            oldUser.OldConfirmedEmail = oldUser.EmailConfirmed ? oldEmail : oldUser.OldConfirmedEmail;

            // If reverted to old email
            oldUser.EmailConfirmed = newEmail == oldUser.OldConfirmedEmail;

            oldUser.Email = newEmail;
        }
    }

    private async Task<(bool success, string? error)> ValidateUser(string username, string email, int updatedUserId, CancellationToken cancellationToken)
    {
        ApplicationUser existingUser = await unitOfWork.IdentityRepository.FindByNameAsync(username, cancellationToken);
        if (existingUser.Id == default)
        {
            existingUser = await unitOfWork.IdentityRepository.FindByEmailAsync(email, cancellationToken);
        }

        if (existingUser.Id != default && existingUser.Id != updatedUserId)
        {
            return (false, Constants.Errors.UserAlreadyExists);
        }

        existingUser = await unitOfWork.IdentityRepository.FindByEmailAsync(username, cancellationToken);
        if (existingUser.Id != default && existingUser.Id != updatedUserId)
        {
            return (false, Constants.Errors.UsernameMustBeDifferentThanEmail);
        }

        return (true, null);
    }

    private async Task UpdateTwoFactorSettingsAsync(ApplicationUser user, ApplicationUserDto newInfo, CancellationToken cancellationToken)
    {
        if (!newInfo.TwoFactorEnabled)
        {
            await unitOfWork.IdentityRepository.SetTwoFactorEnabled(user, false);
            user.TwoFactorType = null;
        }
        else if (newInfo.TwoFactorType == TwoFactorTypes.Email)
        {
            await unitOfWork.IdentityRepository.SetTwoFactorEnabled(user, true);
            user.TwoFactorType = TwoFactorTypes.Email;
        }
    }
}
