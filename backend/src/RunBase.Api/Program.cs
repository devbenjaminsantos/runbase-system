using RunBase.Application;
using RunBase.Application.Health;
using RunBase.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

app.Run();
