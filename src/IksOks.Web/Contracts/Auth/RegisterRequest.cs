namespace IksOks.Web.Contracts.Auth;

public sealed record RegisterRequest(
    string UserName,
    string Password);