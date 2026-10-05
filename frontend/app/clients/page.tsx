"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { Edit2, Plus, RefreshCcw, Search, ShieldCheck, Trash2, X } from "lucide-react";
import { ProtectedPage } from "../../components/ProtectedPage";
import { ApiError, apiFetch } from "../../lib/api";
import { formatDate } from "../../lib/format";

type ClientStatus = "Active" | "Inactive" | "Suspended";
type PlanStage = "Trial" | "Free" | "Plus" | "Premium";
type DataSource = "Demo" | "Manual" | "Imported";

type ClientRow = {
  id: string;
  name: string;
  maskedEmail: string;
  status: ClientStatus;
  planStage: PlanStage;
  dataSource: DataSource;
  nextBillingAt: string | null;
};

type ClientForm = {
  name: string;
  email: string;
  status: ClientStatus;
  planStage: PlanStage;
  dataSource: DataSource;
  nextBillingAt: string;
};

const emptyForm: ClientForm = {
  name: "",
  email: "",
  status: "Active",
  planStage: "Free",
  dataSource: "Manual",
  nextBillingAt: ""
};

export default function ClientsPage() {
  return (
    <ProtectedPage roles={["Admin", "Manager"]} subtitle="Accounts and subscriptions" title="Clients">
      {() => <ClientsTable />}
    </ProtectedPage>
  );
}

function ClientsTable() {
  const [clients, setClients] = useState<ClientRow[]>([]);
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<"All" | ClientStatus>("All");
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingClient, setEditingClient] = useState<ClientRow | null>(null);
  const [form, setForm] = useState<ClientForm>(emptyForm);
  const [message, setMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    void loadClients();
  }, []);

  const filteredClients = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();

    return clients.filter((client) => {
      const matchesQuery = !normalizedQuery || [client.name, client.maskedEmail, client.status, client.planStage, client.dataSource]
        .join(" ")
        .toLowerCase()
        .includes(normalizedQuery);
      const matchesStatus = statusFilter === "All" || client.status === statusFilter;

      return matchesQuery && matchesStatus;
    });
  }, [clients, query, statusFilter]);

  async function loadClients() {
    setState("loading");

    try {
      const result = await apiFetch<ClientRow[]>("/api/clients");
      setClients(result);
      setState("ready");
    } catch {
      setState("error");
    }
  }

  function openCreateForm() {
    setEditingClient(null);
    setForm(emptyForm);
    setMessage(null);
    setIsFormOpen(true);
  }

  function openEditForm(client: ClientRow) {
    setEditingClient(client);
    setForm({
      name: client.name,
      email: "",
      status: client.status,
      planStage: client.planStage,
      dataSource: client.dataSource,
      nextBillingAt: toDateInputValue(client.nextBillingAt)
    });
    setMessage(null);
    setIsFormOpen(true);
  }

  function closeForm() {
    setIsFormOpen(false);
    setEditingClient(null);
    setForm(emptyForm);
    setMessage(null);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSaving(true);
    setMessage(null);

    try {
      const payload = {
        name: form.name,
        status: form.status,
        planStage: form.planStage,
        dataSource: form.dataSource,
        nextBillingAt: toApiDate(form.nextBillingAt)
      };

      if (editingClient) {
        await apiFetch<ClientRow>(`/api/clients/${editingClient.id}`, {
          method: "PUT",
          headers: {
            "content-type": "application/json"
          },
          body: JSON.stringify(payload)
        });
      } else {
        await apiFetch<ClientRow>("/api/clients", {
          method: "POST",
          headers: {
            "content-type": "application/json"
          },
          body: JSON.stringify({
            ...payload,
            email: form.email,
            dataSource: form.dataSource === "Manual" ? undefined : form.dataSource
          })
        });
      }

      await loadClients();
      closeForm();
    } catch (error) {
      setMessage(getClientErrorMessage(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(client: ClientRow) {
    setMessage(null);

    try {
      await apiFetch<void>(`/api/clients/${client.id}`, {
        method: "DELETE"
      });
      await loadClients();
    } catch (error) {
      setMessage(getClientErrorMessage(error));
    }
  }

  const requiresBillingDate = form.planStage === "Plus" || form.planStage === "Premium";

  if (state === "loading") {
    return <div className="state">Loading</div>;
  }

  if (state === "error") {
    return <div className="state state-error">Unable to load clients</div>;
  }

  return (
    <div className="clients-page">
      <div className="data-toolbar">
        <div className="data-toolbar-summary">
          <strong>{clients.length} clients</strong>
          <span>Subscription records and billing status</span>
        </div>
        <div className="data-toolbar-actions">
          <label className="search-field">
            <Search aria-hidden size={17} />
            <span className="sr-only">Search clients</span>
            <input
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search clients"
              type="search"
              value={query}
            />
          </label>
          <button aria-label="Refresh clients" className="icon-button" onClick={() => void loadClients()} title="Refresh clients" type="button">
            <RefreshCcw aria-hidden size={16} />
          </button>
          <button className="button" onClick={openCreateForm} type="button">
            <Plus aria-hidden size={16} />
            <span>New client</span>
          </button>
        </div>
      </div>

      <div className="segmented-control" aria-label="Filter clients by status">
        {(["All", "Active", "Inactive", "Suspended"] as const).map((status) => (
          <button
            aria-pressed={statusFilter === status}
            className={statusFilter === status ? "segmented-control-active" : ""}
            key={status}
            onClick={() => setStatusFilter(status)}
            type="button"
          >
            {status}
          </button>
        ))}
      </div>

      {message ? <div className="alert alert-error">{message}</div> : null}

      <div className={`clients-workspace ${isFormOpen ? "clients-workspace-panel-open" : ""}`}>
        <section className="data-table-panel" aria-label="Client records">
          {clients.length === 0 ? (
            <div className="state">No clients have been created yet.</div>
          ) : filteredClients.length === 0 ? (
            <div className="state">No clients match the current filters.</div>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Email</th>
                    <th>Status</th>
                    <th>Plan</th>
                    <th>Source</th>
                    <th>Next billing</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredClients.map((client) => (
                    <tr key={client.id}>
                      <td>
                        <div className="client-identity">
                          <span aria-hidden className="client-avatar">{getInitials(client.name)}</span>
                          <strong>{client.name}</strong>
                        </div>
                      </td>
                      <td><span className="protected-value"><ShieldCheck aria-hidden size={14} /> {client.maskedEmail}</span></td>
                      <td><span className={`status-pill client-status-${client.status.toLowerCase()}`}>{client.status}</span></td>
                      <td><span className={`plan-badge plan-badge-${client.planStage.toLowerCase()}`}>{client.planStage}</span></td>
                      <td><span className="source-label">{client.dataSource}</span></td>
                      <td><BillingDate value={client.nextBillingAt} /></td>
                      <td>
                        <div className="row-actions">
                          <button className="icon-button" onClick={() => openEditForm(client)} title="Edit client" type="button">
                            <Edit2 aria-hidden size={16} />
                          </button>
                          <button className="icon-button danger-button" onClick={() => void handleDelete(client)} title="Delete client" type="button">
                            <Trash2 aria-hidden size={16} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        {isFormOpen ? (
          <aside className="detail-panel client-detail-panel" aria-labelledby="client-panel-title">
            <form onSubmit={handleSubmit}>
              <div className="detail-panel-header">
                <div>
                  <span className="dashboard-kicker">Client record</span>
                  <h2 id="client-panel-title">{editingClient ? "Edit client" : "New client"}</h2>
                </div>
                <button aria-label="Close client panel" className="icon-button" onClick={closeForm} title="Close" type="button">
                  <X aria-hidden size={16} />
                </button>
              </div>
              {editingClient ? (
                <div className="protected-note">
                  <ShieldCheck aria-hidden size={16} />
                  <span>Stored email remains protected and unchanged: {editingClient.maskedEmail}</span>
                </div>
              ) : null}
              <div className="detail-panel-fields">
                <div className="field">
                  <label htmlFor="client-name">Name</label>
                  <input className="input" id="client-name" minLength={2} onChange={(event) => setForm({ ...form, name: event.target.value })} required value={form.name} />
                </div>
                {!editingClient ? (
                  <div className="field">
                    <label htmlFor="client-email">Email</label>
                    <input className="input" id="client-email" onChange={(event) => setForm({ ...form, email: event.target.value })} required type="email" value={form.email} />
                  </div>
                ) : null}
                <div className="field">
                  <label htmlFor="client-status">Status</label>
                  <select className="input" id="client-status" onChange={(event) => setForm({ ...form, status: event.target.value as ClientStatus })} value={form.status}>
                    <option value="Active">Active</option><option value="Inactive">Inactive</option><option value="Suspended">Suspended</option>
                  </select>
                </div>
                <div className="field">
                  <label htmlFor="client-plan">Plan</label>
                  <select className="input" id="client-plan" onChange={(event) => setForm({ ...form, planStage: event.target.value as PlanStage })} value={form.planStage}>
                    <option value="Trial">Trial</option><option value="Free">Free</option><option value="Plus">Plus</option><option value="Premium">Premium</option>
                  </select>
                </div>
                <div className="field">
                  <label htmlFor="client-source">Source</label>
                  <select className="input" id="client-source" onChange={(event) => setForm({ ...form, dataSource: event.target.value as DataSource })} value={form.dataSource}>
                    <option value="Manual">Manual</option><option value="Demo">Demo</option><option value="Imported">Imported</option>
                  </select>
                </div>
                <div className="field">
                  <label htmlFor="client-billing">Next billing</label>
                  <input className="input" id="client-billing" onChange={(event) => setForm({ ...form, nextBillingAt: event.target.value })} required={requiresBillingDate} type="date" value={form.nextBillingAt} />
                </div>
              </div>
              <div className="detail-panel-actions">
                <button className="button button-secondary" onClick={closeForm} type="button">Cancel</button>
                <button className="button" disabled={isSaving} type="submit">{isSaving ? "Saving" : "Save client"}</button>
              </div>
            </form>
          </aside>
        ) : null}
      </div>
    </div>
  );
}

function BillingDate({ value }: { value: string | null }) {
  if (!value) {
    return <span className="billing-date billing-date-empty">Not scheduled</span>;
  }

  const timestamp = Date.parse(value);
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  const dueSoon = timestamp >= now.getTime() && timestamp <= now.getTime() + (7 * 24 * 60 * 60 * 1000);

  return <span className={`billing-date ${timestamp < now.getTime() ? "billing-date-overdue" : dueSoon ? "billing-date-soon" : ""}`}>{formatDate(value)}</span>;
}

function getInitials(name: string): string {
  return name.split(" ").filter(Boolean).slice(0, 2).map((part) => part[0]).join("").toUpperCase();
}

function toApiDate(value: string): string | null {
  return value ? `${value}T00:00:00Z` : null;
}

function toDateInputValue(value: string | null): string {
  return value ? value.slice(0, 10) : "";
}

function getClientErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 409) {
      return "Email already exists.";
    }

    if (error.status === 400) {
      return "Plus and Premium clients require a next billing date.";
    }

    if (error.status === 403) {
      return "Permission denied.";
    }
  }

  return "Unable to save client.";
}
