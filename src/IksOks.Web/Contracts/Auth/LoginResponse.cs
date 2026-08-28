namespace IksOks.Web.Contracts.Auth;

public sealed record LoginResponse(
    Guid Id,
    string UserName);