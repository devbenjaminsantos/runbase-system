using RunBase.Application.Auth;
using RunBase.Domain.Users;

namespace RunBase.Application.Tests.Auth;

public sealed class AuthPoliciesTests
{
    [Fact]
    public void All_DefinesExpectedRoleAccess()
    {
        AssertPolicy(AuthPolicies.ManageUsers, UserRole.Admin);
        AssertPolicy(AuthPolicies.ViewClients, UserRole.Admin, UserRole.Manager, UserRole.Support);
        AssertPolicy(AuthPolicies.ManageClients, UserRole.Admin, UserRole.Manager);
        AssertPolicy(AuthPolicies.ManagePlans, UserRole.Admin, UserRole.Manager);
        AssertPolicy(AuthPolicies.ViewOrders, UserRole.Admin, UserRole.Manager, UserRole.Support);
        AssertPolicy(AuthPolicies.CreateOrders, UserRole.Admin, UserRole.Manager);
        AssertPolicy(AuthPolicies.EditOrders, UserRole.Admin, UserRole.Manager);
        AssertPolicy(AuthPolicies.UpdateOrderStatus, UserRole.Admin, UserRole.Manager, UserRole.Support);
        AssertPolicy(AuthPolicies.DeleteOrders, UserRole.Admin, UserRole.Manager);
        AssertPolicy(
            AuthPolicies.ViewDashboard,
            UserRole.Admin,
            UserRole.Manager,
            UserRole.Support,
            UserRole.Viewer);
        AssertPolicy(AuthPolicies.ManageSettings, UserRole.Admin);
    }

    private static void AssertPolicy(string policyName, params UserRole[] expectedRoles)
    {
        Assert.True(AuthPolicies.All.TryGetValue(policyName, out var actualRoles));
        Assert.Equal(expectedRoles, actualRoles);
    }
}
