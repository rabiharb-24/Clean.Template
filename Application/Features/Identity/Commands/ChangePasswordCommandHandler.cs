using Application.Common.Extensions;
using Application.Common.Interfaces;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace Application.Features.Identity.Commands;

public sealed record ChangePasswordCommand(int UserId, ChangePasswordDto Info) : IRequest<Result>;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;

    public ChangePasswordCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await unitOfWork.IdentityRepository.GetUserAsync(request.UserId, cancellationToken);
        if (user.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        if (string.IsNullOrEmpty(request.Info.CurrentPassword))
        {
            return await ChangePassword(user, request.Info.NewPassword, cancellationToken);
        }

        bool validPassword = await unitOfWork.IdentityRepository.CheckPasswordAsync(user, request.Info.CurrentPassword, cancellationToken);
        if (!validPassword)
        {
            return Result.CreateFailure([Constants.Errors.IncorrectCurrentPassword]);
        }

        return (await unitOfWork.IdentityRepository
            .ChangePasswordAsync(user, request.Info.CurrentPassword, request.Info.NewPassword, cancellationToken))
            .ToApplicationResult();
    }

    private async Task<Result> ChangePassword(ApplicationUser user, string newPassword, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            IdentityResult result = await unitOfWork.IdentityRepository.RemovePasswordAsync(user, cancellationToken);
            if (!result.Succeeded)
            {
                return result.ToApplicationResult();
            }

            result = await unitOfWork.IdentityRepository.AddPasswordAsync(user, newPassword, cancellationToken);
            if (!result.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);

                return result.ToApplicationResult();
            }

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.CreateSuccess();
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);

            await unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result.CreateFailure([ex.Message]);
        }
    }
}