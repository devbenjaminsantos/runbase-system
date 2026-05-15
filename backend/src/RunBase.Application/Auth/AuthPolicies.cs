using RunBase.Domain.Users;

namespace RunBase.Application.Auth;

public static class AuthPolicies
{
    public const string ManageUsers = "RunBase.Users.Manage";
    public const string ManageClients = "RunBase.Clients.Manage";
    public const string ManagePlans = "RunBase.Plans.Manage";
    public const string ManageOrders = "RunBase.Orders.Manage";
    public const string ViewDashboard = "RunBase.Dashboard.View";
    public const string ManageSettings = "RunBase.Settings.Manage";

    public static IReadOnlyDictionary<string, IReadOnlyCollection<UserRole>> All { get; } =
        new Dictionary<string, IReadOnlyCollection<UserRole>>
        {
            [ManageUsers] = new[] { UserRole.Admin },
            [ManageClients] = new[] { UserRole.Admin, UserRole.Manager },
            [ManagePlans] = new[] { UserRole.Admin, UserRole.Manager },
            [ManageOrders] = new[] { UserRole.Admin, UserRole.Manager, UserRole.Support },
            [ViewDashboard] = new[] { UserRole.Admin, UserRole.Manager, UserRole.Support, UserRole.Viewer },
            [ManageSettings] = new[] { UserRole.Admin }
        };
}
