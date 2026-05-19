"use client";

import { useEffect, useState } from "react";
import { ProtectedPage } from "../../components/ProtectedPage";
import { apiFetch } from "../../lib/api";

type Client = { id: string };
type Plan = { id: string };
type Order = { id: string; status: string; finalAmount: number };
type User = { id: string };

export default function DashboardPage() {
  return (
    <ProtectedPage
      roles={["Admin", "Manager", "Support", "Viewer"]}
      subtitle="Operational overview"
      title="Dashboard"
    >
      {() => <DashboardContent />}
    </ProtectedPage>
  );
}

function DashboardContent() {
  const [summary, setSummary] = useState({
    users: 0,
    clients: 0,
    plans: 0,
    orders: 0,
    revenue: 0
  });
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");

  useEffect(() => {
    Promise.allSettled([
      apiFetch<User[]>("/api/users"),
      apiFetch<Client[]>("/api/clients"),
      apiFetch<Plan[]>("/api/plans"),
      apiFetch<Order[]>("/api/orders")
    ]).then((results) => {
      const users = getValue<User[]>(results[0]);
      const clients = getValue<Client[]>(results[1]);
      const plans = getValue<Plan[]>(results[2]);
      const orders = getValue<Order[]>(results[3]);

      setSummary({
        users: users.length,
        clients: clients.length,
        plans: plans.length,
        orders: orders.length,
        revenue: orders.reduce((total, order) => total + order.finalAmount, 0)
      });
      setState("ready");
    }).catch(() => setState("error"));
  }, []);

  if (state === "loading") {
    return <div className="state">Loading</div>;
  }

  if (state === "error") {
    return <div className="state state-error">Unable to load dashboard</div>;
  }

  return (
    <div className="metric-grid">
      <Metric label="Users" value={summary.users.toString()} />
      <Metric label="Clients" value={summary.clients.toString()} />
      <Metric label="Plans" value={summary.plans.toString()} />
      <Metric label="Orders" value={summary.orders.toString()} />
      <Metric label="Revenue" value={formatCurrency(summary.revenue)} />
    </div>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="metric">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function getValue<T>(result: PromiseSettledResult<T>): T {
  if (result.status === "fulfilled") {
    return result.value;
  }

  return [] as T;
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD"
  }).format(value);
}
