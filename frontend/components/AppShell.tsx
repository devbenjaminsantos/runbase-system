"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { BarChart3, ClipboardList, CreditCard, Home, LogOut, Settings, Shield, Users } from "lucide-react";
import { logout } from "../lib/api";
import { canAccess, navItems } from "../lib/navigation";
import type { UserProfile } from "../lib/types";

const icons = {
  "/dashboard": Home,
  "/users": Users,
  "/clients": Shield,
  "/plans": CreditCard,
  "/orders": ClipboardList,
  "/settings": Settings
};

export function AppShell({
  user,
  title,
  subtitle,
  children
}: {
  user: UserProfile;
  title: string;
  subtitle: string;
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const links = navItems.filter((item) => canAccess(user.role, item));

  async function handleLogout() {
    await logout();
    router.replace("/login");
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <Image src="/logo.png" alt="RunBase" width={220} height={88} priority />
        </div>
        <nav className="nav" aria-label="RunBase">
          {links.map((item) => {
            const Icon = icons[item.href as keyof typeof icons] ?? BarChart3;
            const active = pathname === item.href;

            return (
              <Link className={`nav-link ${active ? "nav-link-active" : ""}`} href={item.href} key={item.href}>
                <Icon aria-hidden size={18} />
                <span>{item.label}</span>
              </Link>
            );
          })}
        </nav>
        <div className="sidebar-footer">
          <div className="user-chip">
            <strong>{user.name}</strong>
            <span>{user.role}</span>
          </div>
          <button className="icon-button" onClick={handleLogout} title="Logout" type="button">
            <LogOut aria-hidden size={18} />
          </button>
        </div>
      </aside>
      <main className="main">
        <header className="topbar">
          <div>
            <h1>{title}</h1>
            <p>{subtitle}</p>
          </div>
          <span className="badge">{user.role}</span>
        </header>
        <div className="content">{children}</div>
      </main>
    </div>
  );
}
