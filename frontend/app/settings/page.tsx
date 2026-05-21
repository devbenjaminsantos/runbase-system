"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { LogOut, RefreshCcw } from "lucide-react";
import { ProtectedPage } from "../../components/ProtectedPage";
import { apiFetch, logout } from "../../lib/api";
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
    await logout();
    router.replace("/login");
  }

  return (
    <div className="settings-grid">
      <section className="settings-panel">
        <div className="form-panel-header">
          <strong>Profile</strong>
          <button className="button button-secondary" disabled={isRefreshing} onClick={() => void refreshProfile()} type="button">
            <RefreshCcw aria-hidden size={16} />
            <span>{isRefreshing ? "Refreshing" : "Refresh"}</span>
          </button>
        </div>
        <div className="table-wrap">
          <table className="table">
            <tbody>
              <tr>
                <th>Name</th>
                <td>{user.name}</td>
              </tr>
              <tr>
                <th>Email</th>
                <td>{user.email}</td>
              </tr>
              <tr>
                <th>Role</th>
                <td><span className="badge">{user.role}</span></td>
              </tr>
              <tr>
                <th>Status</th>
                <td>{user.status}</td>
              </tr>
            </tbody>
          </table>
        </div>
        {message ? <div className="alert alert-info">{message}</div> : null}
      </section>

      <section className="settings-panel">
        <div className="form-panel-header">
          <strong>Session</strong>
        </div>
        <div className="session-summary">
          <div>
            <span>Access token expires</span>
            <strong>{expiresAt ? formatDateTime(expiresAt) : "-"}</strong>
          </div>
          <button className="button" onClick={() => void handleLogout()} type="button">
            <LogOut aria-hidden size={16} />
            <span>Logout</span>
          </button>
        </div>
      </section>
    </div>
  );
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}
