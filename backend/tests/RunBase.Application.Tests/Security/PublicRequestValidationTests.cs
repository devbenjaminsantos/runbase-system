using System.ComponentModel.DataAnnotations;
using RunBase.Application.Clients;
using RunBase.Application.Orders;
using RunBase.Application.Plans;
using RunBase.Application.Users;
using RunBase.Domain.Clients;
using RunBase.Domain.Orders;
using RunBase.Domain.Plans;
using RunBase.Domain.Users;

namespace RunBase.Application.Tests.Security;

public sealed class PublicRequestValidationTests
{
    [Fact]
    public void CreateUserRequest_WithInvalidEmail_IsInvalid()
    {
        var request = new CreateUserRequest(
            "RunBase User",
            "not-an-email",
            "Admin123!",
            UserRole.Admin,
            UserStatus.Active);

        Assert.False(IsValid(request));
    }

    [Fact]
    public void CreateClientRequest_WithEmptyName_IsInvalid()
    {
        var request = new CreateClientRequest(
            string.Empty,
            "client@runbase.local",
            ClientStatus.Active,
            PlanStage.Free,
            null);

        Assert.False(IsValid(request));
    }

    [Fact]
    public void CreatePlanRequest_WithNegativePrice_IsInvalid()
    {
        var request = new CreatePlanRequest(
            "Plus",
            PlanStage.Plus,
            -1,
            BillingCycle.Monthly,
            true,
            DateTimeOffset.UtcNow.AddMonths(1));

        Assert.False(IsValid(request));
    }

    [Fact]
    public void CreateOrderRequest_WithNegativeFinalAmount_IsInvalid()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            PlanStage.Plus,
            OrderStatus.Pending,
            -1);

        Assert.False(IsValid(request));
    }

    private static bool IsValid(object request)
    {
        return Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            [],
            validateAllProperties: true);
    }
}
