namespace Application.Common.Dto;

public sealed record CreateUserDto
{
    public ApplicationUserDto ApplicationUserDto { get; init; } = null!;
}
