using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using RunBase.Application;
using RunBase.Application.Auth;
using RunBase.Application.Clients;
using RunBase.Application.Health;
using RunBase.Application.Notifications;
using RunBase.Application.Orders;
using RunBase.Application.Plans;
using RunBase.Application.Security;
using RunBase.Application.Users;
using RunBase.Infrastructure;
using RunBase.Infrastructure.Auth;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
const string LoginRateLimitPolicy = "login";
const string SensitiveDataRateLimitPolicy = "sensitive-data";
const string FrontendCorsPolicy = "frontend";

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var allowedFrontendOrigins = builder.Configuration
    .GetSection("Frontend:AllowedOrigins")
    .Get<string[]>() ?? new[]
    {
        "http://localhost:3000",
        "http://localhost:3001"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        FrontendCorsPolicy,
        policy => policy
            .WithOrigins(allowedFrontendOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

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

builder.Services.AddAuthorization(options =>
{
    foreach (var policy in AuthPolicies.All)
    {
        options.AddPolicy(
            policy.Key,
            builder => builder
                .RequireAuthenticatedUser()
                .RequireRole(policy.Value.Select(role => role.ToString())));
    }

    options.AddPolicy(
        AuthPolicies.ViewSensitiveData,
        builder => builder
            .RequireAuthenticatedUser()
            .RequireClaim(AuthPolicies.PermissionClaimType, AuthPolicies.ViewSensitiveData));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        await Results.Json(
            new
            {
                message = "Too many requests. Please wait before trying again."
            },
            statusCode: StatusCodes.Status429TooManyRequests)
            .ExecuteAsync(context.HttpContext);
    };

    options.AddPolicy(LoginRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy(SensitiveDataRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetUserOrClientPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
});

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

app.Use(async (context, next) =>
{
    var logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("RunBase.SafeRequestLog");

    try
    {
        await next();
    }
    finally
    {
        logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode}",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode);
    }
});
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

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
.AddEndpointFilter<ValidationFilter<LoginRequest>>()
.RequireRateLimiting(LoginRateLimitPolicy)
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
.AddEndpointFilter<ValidationFilter<RefreshTokenRequest>>()
.WithName("RefreshToken")
.WithSummary("Rotates a valid refresh token and returns a new token pair.");

auth.MapPost("/logout", async (
    LogoutRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var result = await authService.LogoutAsync(request, cancellationToken);

    return result.Succeeded
        ? Results.NoContent()
        : Results.Unauthorized();
})
.RequireAuthorization()
.AddEndpointFilter<ValidationFilter<LogoutRequest>>()
.WithName("Logout")
.WithSummary("Revokes the provided refresh token.");

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

var users = app.MapGroup("/api/users")
    .RequireAuthorization(AuthPolicies.ManageUsers)
    .WithTags("Users");

users.MapGet("/", async (
    IUsersService usersService,
    CancellationToken cancellationToken) =>
{
    var result = await usersService.ListAsync(cancellationToken);

    return Results.Ok(result);
})
.WithName("ListUsers")
.WithSummary("Lists users.");

users.MapGet("/{id:guid}", async (
    Guid id,
    IUsersService usersService,
    CancellationToken cancellationToken) =>
{
    var result = await usersService.GetByIdAsync(id, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result.Value)
        : Results.NotFound();
})
.WithName("GetUser")
.WithSummary("Gets a user by id.");

users.MapPost("/", async (
    CreateUserRequest request,
    IUsersService usersService,
    CancellationToken cancellationToken) =>
{
    var result = await usersService.CreateAsync(request, cancellationToken);

    return result.Error switch
    {
        UserError.None => Results.Created($"/api/users/{result.Value!.Id}", result.Value),
        UserError.EmailAlreadyExists => Results.Conflict(),
        _ => Results.BadRequest()
    };
})
.AddEndpointFilter<ValidationFilter<CreateUserRequest>>()
.WithName("CreateUser")
.WithSummary("Creates a user with an explicit role.");

users.MapPut("/{id:guid}", async (
    Guid id,
    UpdateUserRequest request,
    IUsersService usersService,
    CancellationToken cancellationToken) =>
{
    var result = await usersService.UpdateAsync(id, request, cancellationToken);

    return result.Error switch
    {
        UserError.None => Results.Ok(result.Value),
        UserError.NotFound => Results.NotFound(),
        UserError.EmailAlreadyExists => Results.Conflict(),
        UserError.LastActiveAdminRequired => Results.BadRequest(),
        _ => Results.BadRequest()
    };
})
.AddEndpointFilter<ValidationFilter<UpdateUserRequest>>()
.WithName("UpdateUser")
.WithSummary("Updates a user profile, role, and status.");

users.MapDelete("/{id:guid}", async (
    Guid id,
    IUsersService usersService,
    CancellationToken cancellationToken) =>
{
    var result = await usersService.DeleteAsync(id, cancellationToken);

    return result.Succeeded
        ? Results.NoContent()
        : result.Error == UserError.NotFound
            ? Results.NotFound()
            : Results.BadRequest();
})
.WithName("DeleteUser")
.WithSummary("Deletes a user.");

var clients = app.MapGroup("/api/clients")
    .RequireAuthorization(AuthPolicies.ManageClients)
    .WithTags("Clients");

clients.MapGet("/", async (
    IClientsService clientsService,
    CancellationToken cancellationToken) =>
{
    var result = await clientsService.ListAsync(cancellationToken);

    return Results.Ok(result);
})
.WithName("ListClients")
.WithSummary("Lists clients.");

clients.MapGet("/{id:guid}", async (
    Guid id,
    IClientsService clientsService,
    CancellationToken cancellationToken) =>
{
    var result = await clientsService.GetByIdAsync(id, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result.Value)
        : Results.NotFound();
})
.WithName("GetClient")
.WithSummary("Gets a client by id.");

app.MapGet("/api/clients/{id:guid}/sensitive", async (
    Guid id,
    ClaimsPrincipal principal,
    ISensitiveDataAccessAuditor sensitiveDataAccessAuditor,
    CancellationToken cancellationToken) =>
{
    var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    Guid? userId = Guid.TryParse(userIdValue, out var parsedUserId)
        ? parsedUserId
        : null;

    var decision = await sensitiveDataAccessAuditor.RecordAttemptAsync(
        new SensitiveDataAccessAttempt(
            userId,
            principal.FindFirstValue(ClaimTypes.Email),
            "Client",
            id,
            "Email"),
        cancellationToken);

    return Results.Json(
        new
        {
            message = decision.Outcome == SensitiveDataAuditOutcome.Blocked
                ? "Sensitive data access is blocked."
                : "Sensitive data access is denied.",
            outcome = decision.Outcome
        },
        statusCode: StatusCodes.Status403Forbidden);
})
.RequireAuthorization()
.RequireRateLimiting(SensitiveDataRateLimitPolicy)
.WithTags("Clients")
.WithName("GetClientSensitiveData")
.WithSummary("Audits and denies attempts to view sensitive client data.");

clients.MapPost("/", async (
    CreateClientRequest request,
    IClientsService clientsService,
    CancellationToken cancellationToken) =>
{
    var result = await clientsService.CreateAsync(request, cancellationToken);

    return result.Error switch
    {
        ClientError.None => Results.Created($"/api/clients/{result.Value!.Id}", result.Value),
        ClientError.EmailAlreadyExists => Results.Conflict(),
        ClientError.BillingDateRequired => Results.BadRequest(),
        _ => Results.BadRequest()
    };
})
.AddEndpointFilter<ValidationFilter<CreateClientRequest>>()
.WithName("CreateClient")
.WithSummary("Creates a client with a current plan stage.");

clients.MapPut("/{id:guid}", async (
    Guid id,
    UpdateClientRequest request,
    IClientsService clientsService,
    CancellationToken cancellationToken) =>
{
    var result = await clientsService.UpdateAsync(id, request, cancellationToken);

    return result.Error switch
    {
        ClientError.None => Results.Ok(result.Value),
        ClientError.NotFound => Results.NotFound(),
        ClientError.EmailAlreadyExists => Results.Conflict(),
        ClientError.BillingDateRequired => Results.BadRequest(),
        _ => Results.BadRequest()
    };
})
.AddEndpointFilter<ValidationFilter<UpdateClientRequest>>()
.WithName("UpdateClient")
.WithSummary("Updates a client's profile, status, and plan stage.");

clients.MapDelete("/{id:guid}", async (
    Guid id,
    IClientsService clientsService,
    CancellationToken cancellationToken) =>
{
    var result = await clientsService.DeleteAsync(id, cancellationToken);

    return result.Succeeded
        ? Results.NoContent()
        : Results.NotFound();
})
.WithName("DeleteClient")
.WithSummary("Deletes a client.");

var plans = app.MapGroup("/api/plans")
    .RequireAuthorization(AuthPolicies.ManagePlans)
    .WithTags("Plans");

plans.MapGet("/", async (
    IPlansService plansService,
    CancellationToken cancellationToken) =>
{
    var result = await plansService.ListAsync(cancellationToken);

    return Results.Ok(result);
})
.WithName("ListPlans")
.WithSummary("Lists plans.");

plans.MapGet("/{id:guid}", async (
    Guid id,
    IPlansService plansService,
    CancellationToken cancellationToken) =>
{
    var result = await plansService.GetByIdAsync(id, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result.Value)
        : Results.NotFound();
})
.WithName("GetPlan")
.WithSummary("Gets a plan by id.");

plans.MapPost("/", async (
    CreatePlanRequest request,
    IPlansService plansService,
    CancellationToken cancellationToken) =>
{
    var result = await plansService.CreateAsync(request, cancellationToken);

    return result.Error switch
    {
        PlanError.None => Results.Created($"/api/plans/{result.Value!.Id}", result.Value),
        PlanError.StageAlreadyExists => Results.Conflict(),
        PlanError.InvalidConfiguration => Results.BadRequest(),
        _ => Results.BadRequest()
    };
})
.AddEndpointFilter<ValidationFilter<CreatePlanRequest>>()
.WithName("CreatePlan")
.WithSummary("Creates a plan.");

plans.MapPut("/{id:guid}", async (
    Guid id,
    UpdatePlanRequest request,
    IPlansService plansService,
    CancellationToken cancellationToken) =>
{
    var result = await plansService.UpdateAsync(id, request, cancellationToken);

    return result.Error switch
    {
        PlanError.None => Results.Ok(result.Value),
        PlanError.NotFound => Results.NotFound(),
        PlanError.StageAlreadyExists => Results.Conflict(),
        PlanError.InvalidConfiguration => Results.BadRequest(),
        _ => Results.BadRequest()
    };
})
.AddEndpointFilter<ValidationFilter<UpdatePlanRequest>>()
.WithName("UpdatePlan")
.WithSummary("Updates a plan.");

plans.MapPatch("/{id:guid}/active", async (
    Guid id,
    SetPlanActiveRequest request,
    IPlansService plansService,
    CancellationToken cancellationToken) =>
{
    var result = await plansService.SetActiveAsync(id, request.IsActive, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result.Value)
        : Results.NotFound();
})
.AddEndpointFilter<ValidationFilter<SetPlanActiveRequest>>()
.WithName("SetPlanActive")
.WithSummary("Toggles whether a plan is active.");

plans.MapDelete("/{id:guid}", async (
    Guid id,
    IPlansService plansService,
    CancellationToken cancellationToken) =>
{
    var result = await plansService.DeleteAsync(id, cancellationToken);

    return result.Succeeded
        ? Results.NoContent()
        : Results.NotFound();
})
.WithName("DeletePlan")
.WithSummary("Deletes a plan.");

var orders = app.MapGroup("/api/orders")
    .RequireAuthorization(AuthPolicies.ManageOrders)
    .WithTags("Orders");

orders.MapGet("/", async (
    IOrdersService ordersService,
    CancellationToken cancellationToken) =>
{
    var result = await ordersService.ListAsync(cancellationToken);

    return Results.Ok(result);
})
.WithName("ListOrders")
.WithSummary("Lists orders.");

orders.MapGet("/{id:guid}", async (
    Guid id,
    IOrdersService ordersService,
    CancellationToken cancellationToken) =>
{
    var result = await ordersService.GetByIdAsync(id, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result.Value)
        : Results.NotFound();
})
.WithName("GetOrder")
.WithSummary("Gets an order by id.");

orders.MapPost("/", async (
    CreateOrderRequest request,
    IOrdersService ordersService,
    CancellationToken cancellationToken) =>
{
    var result = await ordersService.CreateAsync(request, cancellationToken);

    return result.Error switch
    {
        OrderError.None => Results.Created($"/api/orders/{result.Value!.Id}", result.Value),
        OrderError.ClientNotFound => Results.NotFound(),
        OrderError.InvalidAmount => Results.BadRequest(),
        _ => Results.BadRequest()
    };
})
.AddEndpointFilter<ValidationFilter<CreateOrderRequest>>()
.WithName("CreateOrder")
.WithSummary("Creates an order with a preserved final amount.");

orders.MapPut("/{id:guid}", async (
    Guid id,
    UpdateOrderRequest request,
    IOrdersService ordersService,
    CancellationToken cancellationToken) =>
{
    var result = await ordersService.UpdateAsync(id, request, cancellationToken);

    return result.Error switch
    {
        OrderError.None => Results.Ok(result.Value),
        OrderError.NotFound => Results.NotFound(),
        OrderError.ClientNotFound => Results.NotFound(),
        OrderError.InvalidAmount => Results.BadRequest(),
        OrderError.InvalidStatusTransition => Results.BadRequest(),
        _ => Results.BadRequest()
    };
})
.AddEndpointFilter<ValidationFilter<UpdateOrderRequest>>()
.WithName("UpdateOrder")
.WithSummary("Updates an order.");

orders.MapPatch("/{id:guid}/status", async (
    Guid id,
    UpdateOrderStatusRequest request,
    IOrdersService ordersService,
    CancellationToken cancellationToken) =>
{
    var result = await ordersService.UpdateStatusAsync(id, request, cancellationToken);

    return result.Error switch
    {
        OrderError.None => Results.Ok(result.Value),
        OrderError.NotFound => Results.NotFound(),
        OrderError.InvalidStatusTransition => Results.BadRequest(),
        _ => Results.BadRequest()
    };
})
.AddEndpointFilter<ValidationFilter<UpdateOrderStatusRequest>>()
.WithName("UpdateOrderStatus")
.WithSummary("Updates an order status.");

orders.MapDelete("/{id:guid}", async (
    Guid id,
    IOrdersService ordersService,
    CancellationToken cancellationToken) =>
{
    var result = await ordersService.DeleteAsync(id, cancellationToken);

    return result.Succeeded
        ? Results.NoContent()
        : Results.NotFound();
})
.WithName("DeleteOrder")
.WithSummary("Deletes an order.");

var notificationCampaigns = app.MapGroup("/api/notification-campaigns")
    .RequireAuthorization(AuthPolicies.ManageClients)
    .WithTags("Notification Campaigns");

notificationCampaigns.MapGet("/", async (
    INotificationCampaignsService notificationCampaignsService,
    CancellationToken cancellationToken) =>
{
    var result = await notificationCampaignsService.ListAsync(cancellationToken);

    return Results.Ok(result);
})
.WithName("ListNotificationCampaigns")
.WithSummary("Lists notification campaigns with estimated audiences.");

notificationCampaigns.MapGet("/{id:guid}", async (
    Guid id,
    INotificationCampaignsService notificationCampaignsService,
    CancellationToken cancellationToken) =>
{
    var result = await notificationCampaignsService.GetByIdAsync(id, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result.Value)
        : Results.NotFound();
})
.WithName("GetNotificationCampaign")
.WithSummary("Gets a notification campaign by id.");

notificationCampaigns.MapPost("/preview", async (
    NotificationCampaignPreviewRequest request,
    INotificationCampaignsService notificationCampaignsService,
    CancellationToken cancellationToken) =>
{
    var result = await notificationCampaignsService.PreviewAsync(request, cancellationToken);

    return Results.Ok(result);
})
.AddEndpointFilter<ValidationFilter<NotificationCampaignPreviewRequest>>()
.WithName("PreviewNotificationCampaign")
.WithSummary("Previews a notification campaign audience without sending messages.");

notificationCampaigns.MapPost("/", async (
    CreateNotificationCampaignRequest request,
    INotificationCampaignsService notificationCampaignsService,
    CancellationToken cancellationToken) =>
{
    var result = await notificationCampaignsService.CreateAsync(request, cancellationToken);

    return result.Error switch
    {
        NotificationCampaignError.None => Results.Created(
            $"/api/notification-campaigns/{result.Value!.Id}",
            result.Value),
        _ => Results.BadRequest()
    };
})
.AddEndpointFilter<ValidationFilter<CreateNotificationCampaignRequest>>()
.WithName("CreateNotificationCampaign")
.WithSummary("Creates a notification campaign draft or scheduled campaign.");

app.Run();

static string GetClientPartitionKey(HttpContext httpContext)
{
    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";
}

static string GetUserOrClientPartitionKey(HttpContext httpContext)
{
    return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        GetClientPartitionKey(httpContext);
}

internal sealed class ValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is null)
        {
            return await next(context);
        }

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        if (Validator.TryValidateObject(
            request,
            validationContext,
            validationResults,
            validateAllProperties: true))
        {
            return await next(context);
        }

        var errors = validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty), (result, memberName) => new
            {
                MemberName = memberName,
                ErrorMessage = result.ErrorMessage ?? "Invalid value."
            })
            .GroupBy(error => error.MemberName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }
}
