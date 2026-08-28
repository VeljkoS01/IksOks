namespace IksOks.Web.Contracts.Auth;

public sealed record LoginRequest(
    string UserName,
    string Password);