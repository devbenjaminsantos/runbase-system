using RunBase.Application.Auth;
using RunBase.Domain.Users;

namespace RunBase.Application.Tests.Auth;

public sealed class AuthPoliciesTests
{
    [Fact]
    public void All_DefinesExpectedRoleAccess()
    {
        AssertPolicy(AuthPolicies.ManageUsers, UserRole.Admin);
        AssertPolicy(AuthPolicies.ManageClients, UserRole.Admin, UserRole.Manager);
        AssertPolicy(AuthPolicies.ManagePlans, UserRole.Admin, UserRole.Manager);
        AssertPolicy(AuthPolicies.ManageOrders, UserRole.Admin, UserRole.Manager, UserRole.Support);
        AssertPolicy(
            AuthPolicies.ViewDashboard,
            UserRole.Admin,
            UserRole.Manager,
            UserRole.Support,
            UserRole.Viewer);
        AssertPolicy(AuthPolicies.ManageSettings, UserRole.Admin);
    }

    [Fact]
    public void All_DoesNotGrantSensitiveDataViewByRole()
    {
        Assert.False(AuthPolicies.All.ContainsKey(AuthPolicies.ViewSensitiveData));
    }

    private static void AssertPolicy(string policyName, params UserRole[] expectedRoles)
    {
        Assert.True(AuthPolicies.All.TryGetValue(policyName, out var actualRoles));
        Assert.Equal(expectedRoles, actualRoles);
    }
}
