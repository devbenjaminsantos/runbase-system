using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using RunBase.Application.Auth;
using RunBase.Application.Clients;
using RunBase.Application.Dashboard;
using RunBase.Application.Orders;
using RunBase.Application.Plans;
using RunBase.Application.Users;
using RunBase.Domain;
using RunBase.Domain.Clients;
using RunBase.Domain.Orders;
using RunBase.Domain.Plans;
using RunBase.Domain.Users;

namespace RunBase.Application.Tests.Api;

public sealed class ApiIntegrationTests
{
    private const string AdminEmail = "admin@runbase.local";
    private const string AdminPassword = "Admin123!Secure";
    private const string SetupKey = "runbase-development-setup-key-change-before-production";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    [Fact]
    public async Task Health_ReturnsHealthyStatusAndSecurityHeaders()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
    }

    [Fact]
    public async Task InitialSetup_CreatesAdminAndClosesRegistration()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var initialStatus = await client.GetFromJsonAsync<InitialSetupStatusResponse>(
            "/api/auth/setup",
            JsonOptions);
        var setupResponse = await client.PostAsJsonAsync(
            "/api/auth/setup",
            CreateInitialAccountRequest(),
            JsonOptions);
        var token = await ReadAsync<AuthTokenResponse>(setupResponse);
        var completedStatus = await client.GetFromJsonAsync<InitialSetupStatusResponse>(
            "/api/auth/setup",
            JsonOptions);
        var repeatedSetupResponse = await client.PostAsJsonAsync(
            "/api/auth/setup",
            CreateInitialAccountRequest(),
            JsonOptions);

        Assert.True(initialStatus!.SetupRequired);
        Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(token.RefreshToken));
        Assert.Equal(AdminEmail, token.User.Email);
        Assert.Equal(UserRole.Admin, token.User.Role);
        Assert.False(completedStatus!.SetupRequired);
        Assert.Equal(HttpStatusCode.Conflict, repeatedSetupResponse.StatusCode);
    }

    [Fact]
    public async Task InitialSetup_WithConcurrentRequests_CreatesOnlyOneAdmin()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/setup", CreateInitialAccountRequest(), JsonOptions),
            client.PostAsJsonAsync(
                "/api/auth/setup",
                new InitialAccountRequest(
                    "Competing Admin",
                    "competing-admin@runbase.local",
                    "CompetingAdmin123!",
                    SetupKey),
                JsonOptions));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithCreatedAdmin_ReturnsTokenPairAndAdminProfile()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var token = await LoginAsync(client);

        Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(token.RefreshToken));
        Assert.Equal(AdminEmail, token.User.Email);
        Assert.Equal(UserRole.Admin, token.User.Role);
    }

    [Fact]
    public async Task AuthSession_RotatesRefreshTokenAndLogoutRevokesIt()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var initialToken = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            initialToken.AccessToken);

        var profileResponse = await client.GetAsync("/api/auth/me");
        var profile = await ReadAsync<UserProfileResponse>(profileResponse);
        var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest(initialToken.RefreshToken),
            JsonOptions);
        var refreshedToken = await ReadAsync<AuthTokenResponse>(refreshResponse);
        var reusedRefreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest(initialToken.RefreshToken),
            JsonOptions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            refreshedToken.AccessToken);
        var logoutResponse = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new LogoutRequest(refreshedToken.RefreshToken),
            JsonOptions);
        var refreshAfterLogoutResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest(refreshedToken.RefreshToken),
            JsonOptions);

        Assert.Equal("admin@runbase.local", profile.Email);
        Assert.NotEqual(initialToken.RefreshToken, refreshedToken.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reusedRefreshResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogoutResponse.StatusCode);
    }

    [Fact]
    public async Task UsersCrud_AsAdmin_PersistsUpdatesAndDeletesUser()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);
        var email = $"manager-{Guid.NewGuid():N}@runbase.local";

        var createResponse = await client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "Integration Manager",
                email,
                "Manager123!",
                UserRole.Manager,
                UserStatus.Active),
            JsonOptions);
        var created = await ReadAsync<UserResponse>(createResponse);

        var getResponse = await client.GetAsync($"/api/users/{created.Id}");
        var fetched = await ReadAsync<UserResponse>(getResponse);
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/users/{created.Id}",
            new UpdateUserRequest(
                "Integration Viewer",
                email,
                UserRole.Viewer,
                UserStatus.Active),
            JsonOptions);
        var updated = await ReadAsync<UserResponse>(updateResponse);
        var deleteResponse = await client.DeleteAsync($"/api/users/{created.Id}");
        var getAfterDeleteResponse = await client.GetAsync($"/api/users/{created.Id}");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(UserRole.Manager, fetched.Role);
        Assert.Equal("Integration Viewer", updated.Name);
        Assert.Equal(UserRole.Viewer, updated.Role);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithMissingRoleOrStatus_ReturnsBadRequest()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);

        var missingRoleResponse = await client.PostAsJsonAsync(
            "/api/users",
            new
            {
                name = "Missing Role",
                email = $"missing-role-{Guid.NewGuid():N}@runbase.local",
                password = "MissingRole123!",
                status = "Active"
            });
        var missingStatusResponse = await client.PostAsJsonAsync(
            "/api/users",
            new
            {
                name = "Missing Status",
                email = $"missing-status-{Guid.NewGuid():N}@runbase.local",
                password = "MissingStatus123!",
                role = "Manager"
            });

        Assert.Equal(HttpStatusCode.BadRequest, missingRoleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingStatusResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WithMissingRoleOrStatus_ReturnsBadRequestAndPreservesUser()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);
        var email = $"partial-update-{Guid.NewGuid():N}@runbase.local";
        var createResponse = await client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "Partial Update User",
                email,
                "PartialUpdate123!",
                UserRole.Manager,
                UserStatus.Active),
            JsonOptions);
        var user = await ReadAsync<UserResponse>(createResponse);

        var missingRoleResponse = await client.PutAsJsonAsync(
            $"/api/users/{user.Id}",
            new
            {
                name = "Missing Role Update",
                email,
                status = "Inactive"
            });
        var missingStatusResponse = await client.PutAsJsonAsync(
            $"/api/users/{user.Id}",
            new
            {
                name = "Missing Status Update",
                email,
                role = "Viewer"
            });
        var getResponse = await client.GetAsync($"/api/users/{user.Id}");
        var unchangedUser = await ReadAsync<UserResponse>(getResponse);

        Assert.Equal(HttpStatusCode.BadRequest, missingRoleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingStatusResponse.StatusCode);
        Assert.Equal("Partial Update User", unchangedUser.Name);
        Assert.Equal(UserRole.Manager, unchangedUser.Role);
        Assert.Equal(UserStatus.Active, unchangedUser.Status);
    }

    [Fact]
    public async Task ClientsCrud_AsAdmin_ReturnsOnlyMaskedEmailAndDeletesClient()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);
        var email = $"client-{Guid.NewGuid():N}@runbase.local";

        var createResponse = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest(
                "Integration Client",
                email,
                ClientStatus.Active,
                PlanStage.Free,
                null,
                DataSource.Manual),
            JsonOptions);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ClientResponse>(createBody, JsonOptions);
        Assert.NotNull(created);

        var getResponse = await client.GetAsync($"/api/clients/{created.Id}");
        var fetched = await ReadAsync<ClientResponse>(getResponse);
        var nextBillingAt = DateTimeOffset.UtcNow.AddMonths(1);
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/clients/{created.Id}",
            new UpdateClientRequest(
                "Integration Premium Client",
                ClientStatus.Active,
                PlanStage.Premium,
                DataSource.Imported,
                nextBillingAt),
            JsonOptions);
        var updated = await ReadAsync<ClientResponse>(updateResponse);
        var deleteResponse = await client.DeleteAsync($"/api/clients/{created.Id}");
        var getAfterDeleteResponse = await client.GetAsync($"/api/clients/{created.Id}");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.DoesNotContain(email, createBody, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(email, fetched.MaskedEmail);
        Assert.Contains("***", fetched.MaskedEmail, StringComparison.Ordinal);
        Assert.Equal(fetched.MaskedEmail, updated.MaskedEmail);
        Assert.Equal(PlanStage.Premium, updated.PlanStage);
        Assert.Equal(DataSource.Imported, updated.DataSource);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task PlansCrud_AsAdmin_PersistsToggleAndDeletesPlan()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "/api/plans",
            new CreatePlanRequest(
                "Integration Free",
                PlanStage.Free,
                0,
                BillingCycle.None,
                true,
                null),
            JsonOptions);
        var created = await ReadAsync<PlanResponse>(createResponse);

        var getResponse = await client.GetAsync($"/api/plans/{created.Id}");
        var fetched = await ReadAsync<PlanResponse>(getResponse);
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/plans/{created.Id}",
            new UpdatePlanRequest(
                "Integration Plus",
                PlanStage.Plus,
                29.90m,
                BillingCycle.Monthly,
                true,
                DateTimeOffset.UtcNow.AddMonths(1)),
            JsonOptions);
        var updated = await ReadAsync<PlanResponse>(updateResponse);
        var toggleResponse = await client.PatchAsJsonAsync(
            $"/api/plans/{created.Id}/active",
            new SetPlanActiveRequest(false),
            JsonOptions);
        var toggled = await ReadAsync<PlanResponse>(toggleResponse);
        var deleteResponse = await client.DeleteAsync($"/api/plans/{created.Id}");
        var getAfterDeleteResponse = await client.GetAsync($"/api/plans/{created.Id}");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(PlanStage.Free, fetched.Stage);
        Assert.Equal(PlanStage.Plus, updated.Stage);
        Assert.Equal(29.90m, updated.Price);
        Assert.False(toggled.IsActive);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task OrdersCrud_AsAdmin_PersistsStatusAndDeletesOrder()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);
        var customer = await CreateClientAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(
                customer.Id,
                PlanStage.Plus,
                OrderStatus.Pending,
                49.90m),
            JsonOptions);
        var created = await ReadAsync<OrderResponse>(createResponse);

        var getResponse = await client.GetAsync($"/api/orders/{created.Id}");
        var fetched = await ReadAsync<OrderResponse>(getResponse);
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/orders/{created.Id}",
            new UpdateOrderRequest(
                customer.Id,
                PlanStage.Premium,
                OrderStatus.Processing,
                79.90m),
            JsonOptions);
        var updated = await ReadAsync<OrderResponse>(updateResponse);
        var statusResponse = await client.PatchAsJsonAsync(
            $"/api/orders/{created.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Completed),
            JsonOptions);
        var completed = await ReadAsync<OrderResponse>(statusResponse);
        var deleteResponse = await client.DeleteAsync($"/api/orders/{created.Id}");
        var getAfterDeleteResponse = await client.GetAsync($"/api/orders/{created.Id}");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(OrderStatus.Pending, fetched.Status);
        Assert.Equal(PlanStage.Premium, updated.PlanStage);
        Assert.Equal(79.90m, updated.FinalAmount);
        Assert.Equal(OrderStatus.Completed, completed.Status);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task Dashboard_AsViewer_ReturnsAuthorizedOperationalAggregates()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);
        var nextBillingAt = DateTimeOffset.UtcNow.AddDays(3);
        var createClientResponse = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest(
                "Dashboard Client",
                $"dashboard-client-{Guid.NewGuid():N}@runbase.local",
                ClientStatus.Active,
                PlanStage.Plus,
                nextBillingAt,
                DataSource.Manual),
            JsonOptions);
        var customer = await ReadAsync<ClientResponse>(createClientResponse);
        var createPlanResponse = await client.PostAsJsonAsync(
            "/api/plans",
            new CreatePlanRequest(
                "Dashboard Plus",
                PlanStage.Plus,
                79.90m,
                BillingCycle.Monthly,
                true,
                nextBillingAt),
            JsonOptions);
        createPlanResponse.EnsureSuccessStatusCode();
        var createOrderResponse = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(
                customer.Id,
                PlanStage.Plus,
                OrderStatus.Completed,
                79.90m),
            JsonOptions);
        createOrderResponse.EnsureSuccessStatusCode();
        var viewerEmail = $"dashboard-viewer-{Guid.NewGuid():N}@runbase.local";
        const string viewerPassword = "DashboardViewer123!";
        var createViewerResponse = await client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "Dashboard Viewer",
                viewerEmail,
                viewerPassword,
                UserRole.Viewer,
                UserStatus.Active),
            JsonOptions);
        createViewerResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = null;
        var viewerToken = await LoginAsync(client, viewerEmail, viewerPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            viewerToken.AccessToken);
        var dashboardResponse = await client.GetAsync("/api/dashboard");
        var dashboard = await ReadAsync<DashboardResponse>(dashboardResponse);

        Assert.Equal(1, dashboard.ActiveClientCount);
        Assert.Equal(1, dashboard.TotalClientCount);
        Assert.Equal(1, dashboard.ActivePlanCount);
        Assert.Equal(1, dashboard.TotalPlanCount);
        Assert.Equal(0, dashboard.OpenOrderCount);
        Assert.Equal(1, dashboard.TotalOrderCount);
        Assert.Equal(79.90m, dashboard.CompletedRevenue);
        Assert.Equal(0, dashboard.OverdueBillingCount);
        Assert.Equal(1, dashboard.UpcomingBillingCount);
        Assert.Equal("Dashboard Client", Assert.Single(dashboard.RecentOrders).ClientName);
        Assert.Equal(
            1,
            Assert.Single(
                dashboard.PlanDistribution,
                item => item.Stage == PlanStage.Plus).Count);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UsersEndpoint_WithSupportRole_ReturnsForbidden()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var adminToken = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken.AccessToken);
        var supportEmail = $"support-{Guid.NewGuid():N}@runbase.local";
        const string supportPassword = "Support123!";
        var createResponse = await client.PostAsJsonAsync(
            "/api/users",
            new
            {
                name = "RunBase Support",
                email = supportEmail,
                password = supportPassword,
                role = "Support",
                status = "Active"
            });
        createResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = null;
        var supportToken = await LoginAsync(client, supportEmail, supportPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supportToken.AccessToken);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrdersEndpoints_WithSupportRole_AllowReadsAndStatusOnly()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await AuthorizeAsAdminAsync(client);
        var customer = await CreateClientAsync(client);
        var createOrderResponse = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(
                customer.Id,
                PlanStage.Plus,
                OrderStatus.Pending,
                49.90m),
            JsonOptions);
        var order = await ReadAsync<OrderResponse>(createOrderResponse);
        var supportEmail = $"orders-support-{Guid.NewGuid():N}@runbase.local";
        const string supportPassword = "Support123!Secure";
        var createSupportResponse = await client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "Orders Support",
                supportEmail,
                supportPassword,
                UserRole.Support,
                UserStatus.Active),
            JsonOptions);
        createSupportResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = null;
        var supportToken = await LoginAsync(client, supportEmail, supportPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            supportToken.AccessToken);

        var listClientsResponse = await client.GetAsync("/api/clients");
        var getClientResponse = await client.GetAsync($"/api/clients/{customer.Id}");
        var listOrdersResponse = await client.GetAsync("/api/orders");
        var getOrderResponse = await client.GetAsync($"/api/orders/{order.Id}");
        var updateStatusResponse = await client.PatchAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Processing),
            JsonOptions);
        var createResponse = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(
                customer.Id,
                PlanStage.Premium,
                OrderStatus.Pending,
                99.90m),
            JsonOptions);
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/orders/{order.Id}",
            new UpdateOrderRequest(
                customer.Id,
                PlanStage.Premium,
                OrderStatus.Processing,
                1m),
            JsonOptions);
        var deleteResponse = await client.DeleteAsync($"/api/orders/{order.Id}");
        var orderAfterDeniedDeleteResponse = await client.GetAsync($"/api/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.OK, listClientsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getClientResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listOrdersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getOrderResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateStatusResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, orderAfterDeniedDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeletedUser_WithPreviouslyIssuedToken_ReturnsUnauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var adminToken = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            adminToken.AccessToken);
        var supportEmail = $"deleted-support-{Guid.NewGuid():N}@runbase.local";
        const string supportPassword = "Support123!Secure";
        var createResponse = await client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "Deleted Support",
                supportEmail,
                supportPassword,
                UserRole.Support,
                UserStatus.Active),
            JsonOptions);
        var support = await ReadAsync<UserResponse>(createResponse);

        client.DefaultRequestHeaders.Authorization = null;
        var supportToken = await LoginAsync(client, supportEmail, supportPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            adminToken.AccessToken);
        var deleteResponse = await client.DeleteAsync($"/api/users/{support.Id}");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            supportToken.AccessToken);
        var staleSessionResponse = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, staleSessionResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPayload_ReturnsBadRequest()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "not-an-email",
                password = "short"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SensitiveClientDataEndpoint_AlwaysDeniesAndEscalatesRepeatedAttempts()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var clientId = Guid.NewGuid();

        var firstResponse = await client.GetAsync($"/api/clients/{clientId}/sensitive");
        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        var secondResponse = await client.GetAsync($"/api/clients/{clientId}/sensitive");
        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        var thirdResponse = await client.GetAsync($"/api/clients/{clientId}/sensitive");

        Assert.Equal(HttpStatusCode.Forbidden, firstResponse.StatusCode);
        Assert.Contains("Denied", firstBody, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Forbidden, secondResponse.StatusCode);
        Assert.Contains("Blocked", secondBody, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.TooManyRequests, thirdResponse.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
            });
    }

    private static async Task AuthorizeAsAdminAsync(HttpClient client)
    {
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token.AccessToken);
    }

    private static async Task<ClientResponse> CreateClientAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest(
                "Order Integration Client",
                $"order-client-{Guid.NewGuid():N}@runbase.local",
                ClientStatus.Active,
                PlanStage.Free,
                null,
                DataSource.Manual),
            JsonOptions);

        return await ReadAsync<ClientResponse>(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);

        Assert.NotNull(value);

        return value;
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
            new
            {
                email,
                password
            });

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
            CreateInitialAccountRequest(),
            JsonOptions);

        response.EnsureSuccessStatusCode();
    }

    private static InitialAccountRequest CreateInitialAccountRequest()
    {
        return new InitialAccountRequest(
            "RunBase Admin",
            AdminEmail,
            AdminPassword,
            SetupKey);
    }
}
