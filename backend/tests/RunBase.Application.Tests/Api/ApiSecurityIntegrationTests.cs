using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using RunBase.Application.Auth;
using RunBase.Application.Clients;
using RunBase.Application.Users;
using RunBase.Domain;
using RunBase.Domain.Clients;
using RunBase.Domain.Plans;
using RunBase.Domain.Users;

namespace RunBase.Application.Tests.Api;

public sealed class ApiSecurityIntegrationTests
{
    private const string AdminEmail = "admin@runbase.local";
    private const string AdminPassword = "Admin123!Secure";
    private const string SetupKey = "runbase-development-setup-key-change-before-production";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    [Theory]
    [InlineData("/api/users")]
    [InlineData("/api/clients")]
    [InlineData("/api/plans")]
    [InlineData("/api/orders")]
    [InlineData("/api/notification-campaigns")]
    [InlineData("/api/clients/22222222-2222-2222-2222-222222222222/sensitive")]
    public async Task ProtectedEndpoints_WithoutToken_ReturnUnauthorized(string path)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Manager, "/api/users", HttpStatusCode.Forbidden)]
    [InlineData(UserRole.Manager, "/api/clients", HttpStatusCode.OK)]
    [InlineData(UserRole.Manager, "/api/plans", HttpStatusCode.OK)]
    [InlineData(UserRole.Manager, "/api/orders", HttpStatusCode.OK)]
    [InlineData(UserRole.Support, "/api/users", HttpStatusCode.Forbidden)]
    [InlineData(UserRole.Support, "/api/clients", HttpStatusCode.OK)]
    [InlineData(UserRole.Support, "/api/plans", HttpStatusCode.Forbidden)]
    [InlineData(UserRole.Support, "/api/orders", HttpStatusCode.OK)]
    [InlineData(UserRole.Viewer, "/api/users", HttpStatusCode.Forbidden)]
    [InlineData(UserRole.Viewer, "/api/clients", HttpStatusCode.Forbidden)]
    [InlineData(UserRole.Viewer, "/api/plans", HttpStatusCode.Forbidden)]
    [InlineData(UserRole.Viewer, "/api/orders", HttpStatusCode.Forbidden)]
    public async Task RoleAccess_EnforcesRbacPolicy(
        UserRole role,
        string path,
        HttpStatusCode expectedStatus)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var token = await CreateAndLoginUserAsync(client, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token.AccessToken);

        var response = await client.GetAsync(path);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task InvalidBearerToken_ReturnsUnauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "invalid.jwt.token");

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InitialSetup_WithInvalidKey_ReturnsUnauthorizedAndKeepsSetupOpen()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/setup",
            new InitialAccountRequest(
                "Unauthorized Admin",
                "unauthorized-admin@runbase.local",
                "UnauthorizedAdmin123!",
                "invalid-initial-setup-key"),
            JsonOptions);
        var status = await client.GetFromJsonAsync<InitialSetupStatusResponse>(
            "/api/auth/setup",
            JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(status!.SetupRequired);
    }

    [Fact]
    public async Task AccessTokenInQueryString_IsNotAccepted()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        var encodedToken = Uri.EscapeDataString(token.AccessToken);

        var response = await client.GetAsync($"/api/users?access_token={encodedToken}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SqlInjectionPayload_DoesNotBypassLogin()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@runbase.local", "' OR 1=1; --"),
            JsonOptions);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("accessToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task XssPayload_IsReturnedAsEscapedJsonData()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);
        const string suspiciousName = "<script>alert('runbase')</script>";

        var response = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest(
                suspiciousName,
                $"xss-{Guid.NewGuid():N}@runbase.local",
                ClientStatus.Active,
                PlanStage.Free,
                null,
                DataSource.Manual),
            JsonOptions);
        var body = await response.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ClientResponse>(body, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("<script>", body, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(created);
        Assert.Equal(suspiciousName, created.Name);
    }

    [Fact]
    public async Task MalformedJson_ReturnsBadRequestWithoutServerDetails()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        using var content = new StringContent(
            "{\"email\":\"admin@runbase.local\",\"password\":",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/auth/login", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("stack trace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection string", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownEnumValue_ReturnsBadRequest()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);
        using var content = new StringContent(
            """
            {
              "name": "Unknown Plan",
              "stage": "Enterprise",
              "price": 0,
              "billingCycle": "None",
              "isActive": true,
              "nextBillingAt": null
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/plans", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OversizedPayloadField_ReturnsValidationProblem()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest(
                new string('A', 161),
                $"oversized-{Guid.NewGuid():N}@runbase.local",
                ClientStatus.Active,
                PlanStage.Free,
                null,
                DataSource.Manual),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
            });
    }

    private static async Task<AuthTokenResponse> CreateAndLoginUserAsync(
        HttpClient client,
        UserRole role)
    {
        var adminToken = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            adminToken.AccessToken);
        var email = $"{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@runbase.local";
        const string password = "RoleTest123!";
        var createResponse = await client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                $"Integration {role}",
                email,
                password,
                role,
                UserStatus.Active),
            JsonOptions);
        createResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = null;

        return await LoginAsync(client, email, password);
    }

    private static async Task AuthorizeAsAdminAsync(HttpClient client)
    {
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token.AccessToken);
    }

    private static async Task<AuthTokenResponse> LoginAsync(
        HttpClient client,
        string email = AdminEmail,
        string password = AdminPassword)
    {
        if (email == AdminEmail)
        {
            await EnsureInitialAdminAsync(client);
        }

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            JsonOptions);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);

        Assert.NotNull(token);

        return token;
    }

    private static async Task EnsureInitialAdminAsync(HttpClient client)
    {
        var status = await client.GetFromJsonAsync<InitialSetupStatusResponse>(
            "/api/auth/setup",
            JsonOptions);

        if (status?.SetupRequired != true)
        {
            return;
        }

        var response = await client.PostAsJsonAsync(
            "/api/auth/setup",
            new InitialAccountRequest(
                "RunBase Admin",
                AdminEmail,
                AdminPassword,
                SetupKey),
            JsonOptions);

        response.EnsureSuccessStatusCode();
    }
}
