using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RunBase.Application;
using RunBase.Application.Auth;
using RunBase.Application.Health;
using RunBase.Infrastructure;
using RunBase.Infrastructure.Auth;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "RunBase API";
        options.Theme = ScalarTheme.Kepler;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", (IHealthStatusService healthStatusService) =>
{
    return Results.Ok(healthStatusService.GetStatus());
})
.WithName("GetHealth")
.WithSummary("Returns the current API health status.");

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        name = "RunBase API",
        status = "Ready",
        documentation = "/scalar/v1"
    });
})
.WithName("GetApiInfo")
.WithSummary("Returns basic API information.");

var auth = app.MapGroup("/api/auth")
    .WithTags("Auth");

auth.MapPost("/login", async (
    LoginRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var result = await authService.LoginAsync(request, cancellationToken);

    if (result.Succeeded)
    {
        return Results.Ok(result.Value);
    }

    return result.Error switch
    {
        AuthError.InactiveUser => Results.Forbid(),
        _ => Results.Unauthorized()
    };
})
.WithName("Login")
.WithSummary("Authenticates a user and returns an access token.");

auth.MapPost("/refresh", async (
    RefreshTokenRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var result = await authService.RefreshAsync(request, cancellationToken);

    if (result.Succeeded)
    {
        return Results.Ok(result.Value);
    }

    return result.Error switch
    {
        AuthError.InactiveUser => Results.Forbid(),
        AuthError.UserNotFound => Results.NotFound(),
        _ => Results.Unauthorized()
    };
})
.WithName("RefreshToken")
.WithSummary("Rotates a valid refresh token and returns a new token pair.");

auth.MapGet("/me", async (
    ClaimsPrincipal principal,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);

    if (!Guid.TryParse(userIdValue, out var userId))
    {
        return Results.Unauthorized();
    }

    var result = await authService.GetCurrentUserAsync(userId, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result.Value)
        : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetCurrentUser")
.WithSummary("Returns the authenticated user's profile.");

app.Run();
