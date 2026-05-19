"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AppShell } from "./AppShell";
import { apiFetch } from "../lib/api";
import { readSession, writeSession } from "../lib/session";
import type { UserProfile, UserRole } from "../lib/types";

export function ProtectedPage({
  title,
  subtitle,
  roles,
  children
}: {
  title: string;
  subtitle: string;
  roles: UserRole[];
  children: (user: UserProfile) => React.ReactNode;
}) {
  const router = useRouter();
  const [user, setUser] = useState<UserProfile | null>(null);
  const [status, setStatus] = useState<"loading" | "ready" | "denied">("loading");

  useEffect(() => {
    let active = true;
    const session = readSession();

    if (!session) {
      router.replace("/login");
      return;
    }

    apiFetch<UserProfile>("/api/auth/me")
      .then((profile) => {
        if (!active) {
          return;
        }

        writeSession({ ...session, user: profile });
        setUser(profile);
        setStatus(roles.includes(profile.role) ? "ready" : "denied");
      })
      .catch(() => {
        if (active) {
          router.replace("/login");
        }
      });

    return () => {
      active = false;
    };
  }, [roles, router]);

  if (status === "loading" || !user) {
    return <div className="state">Loading</div>;
  }

  if (status === "denied") {
    return (
      <AppShell title="Access denied" subtitle="RunBase" user={user}>
        <div className="state state-error">Permission denied</div>
      </AppShell>
    );
  }

  return (
    <AppShell title={title} subtitle={subtitle} user={user}>
      {children(user)}
    </AppShell>
  );
}
