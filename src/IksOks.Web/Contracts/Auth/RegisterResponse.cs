namespace IksOks.Web.Contracts.Auth;

public sealed record RegisterResponse(
    Guid Id,
    string UserName);