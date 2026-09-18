using IksOks.Web.Domain.Entities;
using IksOks.Web.Domain.States;
using IksOks.Web.Domain.Strategies;
using IksOks.Web.Endpoints;
using IksOks.Web.Infrastructure.Persistence;
using IksOks.Web.Messaging;
using IksOks.Web.Realtime;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IksOks.Web.Application.Commands;
using IksOks.Web.Application.Commands.Matches;
using IksOks.Web.Application.Background;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IksOksDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("Default");

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "IksOks.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(
        RabbitMqOptions.SectionName));

builder.Services.AddSingleton<
    IEventPublisher,
    RabbitMqEventPublisher>();

builder.Services.AddHostedService<
    MatchFinishedConsumer>();

builder.Services.AddHostedService<
    OutboxPublisher>();

builder.Services.AddSingleton<
    ClassicGameRulesStrategy>();

builder.Services.AddSingleton<
    ConnectKGameRulesStrategy>();

builder.Services.AddSingleton<
    GameRulesStrategyFactory>();

builder.Services.AddSingleton<
    IMatchState,
    WaitingForOpponentMatchState>();

builder.Services.AddSingleton<
    IMatchState,
    InProgressMatchState>();

builder.Services.AddSingleton<
    IMatchState,
    FinishedMatchState>();

builder.Services.AddSingleton<
    IMatchState,
    PausedMatchState>();

builder.Services.AddSingleton<
    MatchStateFactory>();

builder.Services.AddScoped<
    ICommandHandler<
        MakeMoveCommand,
        MakeMoveCommandResult>,
        MakeMoveCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<
        RequestPauseCommand,
        MatchControlCommandResult>,
    MatchControlCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<
        PauseMatchCommand,
        MatchControlCommandResult>,
    MatchControlCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<
        RejectPauseRequestCommand,
        MatchControlCommandResult>,
    MatchControlCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<
        ResumeMatchCommand,
        MatchControlCommandResult>,
    MatchControlCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<
        RequestResumeCommand,
        MatchControlCommandResult>,
    MatchControlCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<
        RejectResumeRequestCommand,
        MatchControlCommandResult>,
    MatchControlCommandHandler>();

builder.Services.AddHostedService<
    MatchTimeoutWorker>();


var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        application = "IksOks"
    });
});

app.UseAuthentication();
app.UseAuthorization();
app.MapAuthEndpoints();
app.MapMatchEndpoints();
app.MapUserEndpoints();
app.MapHub<MatchHub>("/hubs/match")
    .RequireAuthorization();

app.Run();