using Application.Common.Interfaces.Services;

namespace Api.Controllers;

[Route(Constants.Controller.Route)]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public abstract class BaseController : ControllerBase
{
    protected BaseController(IMediator mediator, ICurrentUserService currentUserService)
    {
        Mediator = mediator;
        this.currentUserService = currentUserService;
    }

    private readonly ICurrentUserService currentUserService;

    public int UserId => currentUserService.GetId();

    protected IMediator Mediator { get; }
}
