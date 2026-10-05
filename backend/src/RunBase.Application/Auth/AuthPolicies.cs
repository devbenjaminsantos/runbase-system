using RunBase.Domain.Users;

namespace RunBase.Application.Auth;

public static class AuthPolicies
{
    public const string ManageUsers = "RunBase.Users.Manage";
    public const string ViewClients = "RunBase.Clients.View";
    public const string ManageClients = "RunBase.Clients.Manage";
    public const string ManagePlans = "RunBase.Plans.Manage";
    public const string ViewOrders = "RunBase.Orders.View";
    public const string CreateOrders = "RunBase.Orders.Create";
    public const string EditOrders = "RunBase.Orders.Edit";
    public const string UpdateOrderStatus = "RunBase.Orders.UpdateStatus";
    public const string DeleteOrders = "RunBase.Orders.Delete";
    public const string ViewDashboard = "RunBase.Dashboard.View";
    public const string ManageSettings = "RunBase.Settings.Manage";
    public static IReadOnlyDictionary<string, IReadOnlyCollection<UserRole>> All { get; } =
        new Dictionary<string, IReadOnlyCollection<UserRole>>
        {
            [ManageUsers] = new[] { UserRole.Admin },
            [ViewClients] = new[] { UserRole.Admin, UserRole.Manager, UserRole.Support },
            [ManageClients] = new[] { UserRole.Admin, UserRole.Manager },
            [ManagePlans] = new[] { UserRole.Admin, UserRole.Manager },
            [ViewOrders] = new[] { UserRole.Admin, UserRole.Manager, UserRole.Support },
            [CreateOrders] = new[] { UserRole.Admin, UserRole.Manager },
            [EditOrders] = new[] { UserRole.Admin, UserRole.Manager },
            [UpdateOrderStatus] = new[] { UserRole.Admin, UserRole.Manager, UserRole.Support },
            [DeleteOrders] = new[] { UserRole.Admin, UserRole.Manager },
            [ViewDashboard] = new[] { UserRole.Admin, UserRole.Manager, UserRole.Support, UserRole.Viewer },
            [ManageSettings] = new[] { UserRole.Admin }
        };
}
