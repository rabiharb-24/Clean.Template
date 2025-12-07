namespace Application.Common.Models;

public sealed record UserCredentials(
    string UserName,
    string Password,
    string Token);
