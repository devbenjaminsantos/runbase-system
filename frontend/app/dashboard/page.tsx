"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  Activity,
  ArrowUpRight,
  CircleAlert,
  Clock3,
  CreditCard,
  ReceiptText,
  UsersRound
} from "lucide-react";
import { ProtectedPage } from "../../components/ProtectedPage";
import { apiFetch } from "../../lib/api";
import { formatCurrency, formatDate } from "../../lib/format";
import { canAccess, navItems } from "../../lib/navigation";
import type { UserProfile } from "../../lib/types";

type PlanStage = "Trial" | "Free" | "Plus" | "Premium";
type OrderStatus = "Pending" | "Processing" | "Completed" | "Cancelled";

type RecentOrder = {
  id: string;
  clientName: string | null;
  planStage: PlanStage;
  status: OrderStatus;
  finalAmount: number;
  updatedAt: string;
};

type DashboardData = {
  activeClientCount: number;
  totalClientCount: number;
  activePlanCount: number;
  totalPlanCount: number;
  openOrderCount: number;
  totalOrderCount: number;
  completedRevenue: number;
  overdueBillingCount: number;
  upcomingBillingCount: number;
  recentOrders: RecentOrder[];
  planDistribution: Array<{ stage: PlanStage; count: number }>;
};

export default function DashboardPage() {
  return (
    <ProtectedPage
      roles={["Admin", "Manager", "Support", "Viewer"]}
      subtitle="Operational overview"
      title="Dashboard"
    >
      {(user) => <DashboardContent user={user} />}
    </ProtectedPage>
  );
}

function DashboardContent({ user }: { user: UserProfile }) {
  const [data, setData] = useState<DashboardData | null>(null);
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");

  useEffect(() => {
    void loadDashboard();
  }, []);

  async function loadDashboard() {
    setState("loading");

    try {
      const result = await apiFetch<DashboardData>("/api/dashboard");
      setData(result);
      setState("ready");
    } catch {
      setData(null);
      setState("error");
    }
  }

  if (state === "loading" || (state === "ready" && !data)) {
    return <div className="state">Loading operational data</div>;
  }

  if (state === "error" || !data) {
    return (
      <div className="state state-error">
        <span>Unable to load operational data.</span>
        <button className="button button-secondary" onClick={() => void loadDashboard()} type="button">Try again</button>
      </div>
    );
  }

  const largestPlanCount = Math.max(...data.planDistribution.map((item) => item.count), 1);
  const canViewClients = canAccess(user.role, navItems[2]);
  const canViewOrders = canAccess(user.role, navItems[4]);

  return (
    <div className="dashboard-layout">
      <section className="dashboard-overview" aria-labelledby="operations-snapshot">
        <div className="dashboard-section-heading">
          <div>
            <span className="dashboard-kicker">Live workspace</span>
            <h2 id="operations-snapshot">Operations snapshot</h2>
          </div>
          <button className="button button-secondary dashboard-refresh" onClick={() => void loadDashboard()} type="button">
            <Activity aria-hidden size={16} />
            <span>Refresh</span>
          </button>
        </div>
        <div className="dashboard-metrics">
          <Metric icon={UsersRound} label="Active clients" value={data.activeClientCount.toString()} detail={`${data.totalClientCount} total records`} />
          <Metric icon={CreditCard} label="Active plans" value={data.activePlanCount.toString()} detail={`${data.totalPlanCount} configured`} />
          <Metric icon={ReceiptText} label="Open orders" value={data.openOrderCount.toString()} detail={`${data.totalOrderCount} total orders`} tone={data.openOrderCount > 0 ? "warning" : "default"} />
          <Metric icon={Activity} label="Completed revenue" value={formatCurrency(data.completedRevenue)} detail="Completed orders only" tone="success" />
        </div>
      </section>

      <div className="dashboard-workspace">
        <section className="dashboard-panel dashboard-attention" aria-labelledby="attention-title">
          <div className="dashboard-panel-heading">
            <div>
              <span className="dashboard-kicker">Priority queue</span>
              <h2 id="attention-title">Requires attention</h2>
            </div>
            <CircleAlert aria-hidden className="attention-icon" size={20} />
          </div>
          <div className="attention-list">
            <AttentionItem
              count={data.overdueBillingCount}
              description="Active subscriptions with a past billing date"
              href={canViewClients ? "/clients" : undefined}
              label="Overdue billing"
              tone={data.overdueBillingCount > 0 ? "danger" : "neutral"}
            />
            <AttentionItem
              count={data.upcomingBillingCount}
              description="Billing dates arriving in the next seven days"
              href={canViewClients ? "/clients" : undefined}
              label="Upcoming billing"
              tone={data.upcomingBillingCount > 0 ? "warning" : "neutral"}
            />
            <AttentionItem
              count={data.openOrderCount}
              description="Orders waiting for resolution or completion"
              href={canViewOrders ? "/orders" : undefined}
              label="Open orders"
              tone={data.openOrderCount > 0 ? "warning" : "neutral"}
            />
          </div>
        </section>

        <section className="dashboard-panel dashboard-orders" aria-labelledby="recent-orders-title">
          <div className="dashboard-panel-heading">
            <div>
              <span className="dashboard-kicker">Order activity</span>
              <h2 id="recent-orders-title">Recent orders</h2>
            </div>
            {canViewOrders ? (
              <Link className="dashboard-text-link" href="/orders">View orders <ArrowUpRight aria-hidden size={14} /></Link>
            ) : null}
          </div>
          {data.recentOrders.length === 0 ? (
            <div className="dashboard-empty">No orders have been recorded yet.</div>
          ) : (
            <div className="recent-orders-list">
              {data.recentOrders.map((order) => (
                <div className="recent-order" key={order.id}>
                  <div className="recent-order-main">
                    <strong>{order.clientName ?? "Client unavailable"}</strong>
                    <span>{order.planStage} plan · {formatDate(order.updatedAt)}</span>
                  </div>
                  <div className="recent-order-meta">
                    <span className={`status-pill status-${order.status.toLowerCase()}`}>{order.status}</span>
                    <strong>{formatCurrency(order.finalAmount)}</strong>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>

      <section className="dashboard-panel dashboard-distribution" aria-labelledby="subscription-title">
        <div className="dashboard-panel-heading">
          <div>
            <span className="dashboard-kicker">Client base</span>
            <h2 id="subscription-title">Subscription distribution</h2>
          </div>
          <Clock3 aria-hidden className="dashboard-panel-icon" size={19} />
        </div>
        <div className="distribution-list">
          {data.planDistribution.map((item) => (
            <div className="distribution-row" key={item.stage}>
              <span>{item.stage}</span>
              <div aria-label={`${item.stage}: ${item.count} clients`} className="distribution-track" role="img">
                <span style={{ width: `${(item.count / largestPlanCount) * 100}%` }} />
              </div>
              <strong>{item.count}</strong>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}

function Metric({
  icon: Icon,
  label,
  value,
  detail,
  tone = "default"
}: {
  icon: typeof Activity;
  label: string;
  value: string;
  detail: string;
  tone?: "default" | "success" | "warning";
}) {
  return (
    <article className={`dashboard-metric dashboard-metric-${tone}`}>
      <div className="dashboard-metric-top">
        <span>{label}</span>
        <Icon aria-hidden size={17} />
      </div>
      <strong>{value}</strong>
      <small>{detail}</small>
    </article>
  );
}

function AttentionItem({
  count,
  description,
  href,
  label,
  tone
}: {
  count: number;
  description: string;
  href?: string;
  label: string;
  tone: "danger" | "warning" | "neutral";
}) {
  const content = (
    <>
      <span className={`attention-count attention-count-${tone}`}>{count}</span>
      <span className="attention-copy">
        <strong>{label}</strong>
        <span>{description}</span>
      </span>
      <ArrowUpRight aria-hidden className="attention-arrow" size={17} />
    </>
  );

  return href ? (
    <Link className="attention-item" href={href}>{content}</Link>
  ) : (
    <div className="attention-item attention-item-static">{content}</div>
  );
}
