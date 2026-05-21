import type { NavItem, UserRole } from "./types";

export const navItems: NavItem[] = [
  { href: "/dashboard", label: "Dashboard", roles: ["Admin", "Manager", "Support", "Viewer"] },
  { href: "/users", label: "Users", roles: ["Admin"] },
  { href: "/clients", label: "Clients", roles: ["Admin", "Manager"] },
  { href: "/plans", label: "Plans", roles: ["Admin", "Manager"] },
  { href: "/orders", label: "Orders", roles: ["Admin", "Manager", "Support"] },
  { href: "/settings", label: "Settings", roles: ["Admin", "Manager", "Support", "Viewer"] }
];

export function canAccess(role: UserRole, item: NavItem): boolean {
  return item.roles.includes(role);
}
