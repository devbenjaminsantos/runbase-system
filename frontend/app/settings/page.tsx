"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Clock3, LogOut, RefreshCcw, ShieldCheck } from "lucide-react";
import { ProtectedPage } from "../../components/ProtectedPage";
import { apiFetch, logout } from "../../lib/api";
import { formatDateTime } from "../../lib/format";
import { readSession } from "../../lib/session";
import type { UserProfile } from "../../lib/types";

export default function SettingsPage() {
  return (
    <ProtectedPage
      roles={["Admin", "Manager", "Support", "Viewer"]}
      subtitle="Current profile and session"
      title="Settings"
    >
      {(user) => <SettingsContent initialUser={user} />}
    </ProtectedPage>
  );
}

function SettingsContent({ initialUser }: { initialUser: UserProfile }) {
  const router = useRouter();
  const [user, setUser] = useState(initialUser);
  const [expiresAt, setExpiresAt] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);

  useEffect(() => {
    setExpiresAt(readSession()?.expiresAtUtc ?? null);
  }, []);

  async function refreshProfile() {
    setIsRefreshing(true);
    setMessage(null);

    try {
      const profile = await apiFetch<UserProfile>("/api/auth/me");
      setUser(profile);
      setExpiresAt(readSession()?.expiresAtUtc ?? null);
      setMessage("Profile refreshed.");
    } catch {
      setMessage("Unable to refresh profile.");
    } finally {
      setIsRefreshing(false);
    }
  }

  async function handleLogout() {
    const result = await logout();
    router.replace(result.remoteRevocation === "failed" ? "/login?logout=local-only" : "/login");
  }

  return (
    <div className="settings-grid">
      <section className="settings-panel settings-profile-panel">
        <div className="settings-panel-header">
          <div>
            <span className="dashboard-kicker">Signed-in identity</span>
            <h2>Profile</h2>
          </div>
          <button aria-label="Refresh profile" className="icon-button" disabled={isRefreshing} onClick={() => void refreshProfile()} title="Refresh profile" type="button">
            <RefreshCcw aria-hidden size={16} />
          </button>
        </div>
        <div className="settings-identity">
          <span aria-hidden className="settings-avatar">{getInitials(user.name)}</span>
          <div>
            <strong>{user.name}</strong>
            <span>{user.email}</span>
          </div>
        </div>
        <div className="profile-facts">
          <div>
            <span>Role</span>
            <strong className={`role-badge role-badge-${user.role.toLowerCase()}`}>{user.role}</strong>
          </div>
          <div>
            <span>Account status</span>
            <strong className={`status-pill user-status-${user.status.toLowerCase()}`}>{user.status}</strong>
          </div>
        </div>
        {message ? <div className="alert alert-info">{message}</div> : null}
      </section>

      <section className="settings-panel settings-session-panel">
        <div className="settings-panel-header">
          <div>
            <span className="dashboard-kicker">Current access</span>
            <h2>Session</h2>
          </div>
          <ShieldCheck aria-hidden className="settings-panel-icon" size={21} />
        </div>
        <div className="session-summary">
          <div>
            <span><Clock3 aria-hidden size={15} /> Access token expires</span>
            <strong>{expiresAt ? formatDateTime(expiresAt) : "-"}</strong>
          </div>
          <button className="button button-danger" onClick={() => void handleLogout()} type="button">
            <LogOut aria-hidden size={16} />
            <span>Logout</span>
          </button>
        </div>
      </section>
    </div>
  );
}

function getInitials(name: string): string {
  return name.split(" ").filter(Boolean).slice(0, 2).map((part) => part[0]).join("").toUpperCase();
}
