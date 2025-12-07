using Application.Common.Interfaces;
using Application.Common.Models.Responses;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.OData.Query;

namespace Application.Features.Identity.Queries;

public sealed record GetUsersOdataQuery(ODataQueryOptions<ApplicationUserDto> FilterOptions)
    : IRequest<Result<OdataResponse<ApplicationUserDto>>>;

public class GetUsersOdataQueryHandler : IRequestHandler<GetUsersOdataQuery, Result<OdataResponse<ApplicationUserDto>>>
{
    private readonly IUnitOfWork unitOfWork;

    public GetUsersOdataQueryHandler(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<OdataResponse<ApplicationUserDto>>> Handle(GetUsersOdataQuery request, CancellationToken cancellationToken)
    {
        var query = unitOfWork.GetAsQueryable<ApplicationUser>()
         .Select(x => new ApplicationUserDto
         {
             Id = x.Id,
             Username = x.UserName,
             FirstName = x.FirstName,
             MiddleName = x.MiddleName,
             LastName = x.LastName,
             PhoneNumber = x.PhoneNumber,
             FullName = string.Join(" ", new[] { x.FirstName, x.MiddleName, x.LastName }.Where(n => !string.IsNullOrWhiteSpace(n))),
             Email = x.Email,
             CreatedAt = x.CreatedAt,
             Active = x.Active,
             ActiveStatus = x.Active ? "Active" : "NotActive",
             EmailConfirmed = x.EmailConfirmed,
             EmailConfirmedStatus = x.EmailConfirmed ? "Confirmed" : "NotConfirmed",
             ApplicationUserRole = new ApplicationUserRole() { Role = new ApplicationRole() { Name = x.UserRoles.FirstOrDefault() != null ? x.UserRoles.First().Role.Name : string.Empty } },
             RoleName = x.UserRoles.Any() ? (x.UserRoles.OrderBy(ur => ur.RoleId).Select(ur => ur.Role.Name).FirstOrDefault() ?? string.Empty) : string.Empty
         });

        var result = await unitOfWork.ODataGetAsync(request.FilterOptions, query, cancellationToken);

        return Result<OdataResponse<ApplicationUserDto>>.CreateSuccess(result);
    }
}
