namespace Application.Common.Dto;

public sealed record UpdateUserDto
{
    public ApplicationUserDto ApplicationUserDto { get; init; } = null!;
}
