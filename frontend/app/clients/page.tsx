"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { Edit2, Plus, RefreshCcw, Trash2, X } from "lucide-react";
import { ProtectedPage } from "../../components/ProtectedPage";
import { ApiError, apiFetch } from "../../lib/api";

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

    if (!normalizedQuery) {
      return clients;
    }

    return clients.filter((client) =>
      [client.name, client.maskedEmail, client.status, client.planStage, client.dataSource]
        .join(" ")
        .toLowerCase()
        .includes(normalizedQuery));
  }, [clients, query]);

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
        email: form.email,
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
    <div className="stack">
      <div className="toolbar">
        <input
          className="input toolbar-search"
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Search clients"
          type="search"
          value={query}
        />
        <button className="button button-secondary" onClick={() => void loadClients()} type="button">
          <RefreshCcw aria-hidden size={16} />
        </button>
        <button className="button" onClick={openCreateForm} type="button">
          <Plus aria-hidden size={16} />
          <span>New client</span>
        </button>
      </div>

      {message ? <div className="alert alert-error">{message}</div> : null}

      {isFormOpen ? (
        <form className="form-panel" onSubmit={handleSubmit}>
          <div className="form-panel-header">
            <strong>{editingClient ? "Edit client" : "Create client"}</strong>
            <button className="icon-button" onClick={closeForm} title="Close" type="button">
              <X aria-hidden size={16} />
            </button>
          </div>
          {editingClient ? (
            <div className="state compact-state">
              Stored email is protected. Re-enter the client email to save changes.
            </div>
          ) : null}
          <div className="form-grid">
            <div className="field">
              <label htmlFor="client-name">Name</label>
              <input
                className="input"
                id="client-name"
                minLength={2}
                onChange={(event) => setForm({ ...form, name: event.target.value })}
                required
                value={form.name}
              />
            </div>
            <div className="field">
              <label htmlFor="client-email">Email</label>
              <input
                className="input"
                id="client-email"
                onChange={(event) => setForm({ ...form, email: event.target.value })}
                placeholder={editingClient ? editingClient.maskedEmail : undefined}
                required
                type="email"
                value={form.email}
              />
            </div>
            <div className="field">
              <label htmlFor="client-status">Status</label>
              <select
                className="input"
                id="client-status"
                onChange={(event) => setForm({ ...form, status: event.target.value as ClientStatus })}
                value={form.status}
              >
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
                <option value="Suspended">Suspended</option>
              </select>
            </div>
            <div className="field">
              <label htmlFor="client-plan">Plan</label>
              <select
                className="input"
                id="client-plan"
                onChange={(event) => setForm({ ...form, planStage: event.target.value as PlanStage })}
                value={form.planStage}
              >
                <option value="Trial">Trial</option>
                <option value="Free">Free</option>
                <option value="Plus">Plus</option>
                <option value="Premium">Premium</option>
              </select>
            </div>
            <div className="field">
              <label htmlFor="client-source">Source</label>
              <select
                className="input"
                id="client-source"
                onChange={(event) => setForm({ ...form, dataSource: event.target.value as DataSource })}
                value={form.dataSource}
              >
                <option value="Manual">Manual</option>
                <option value="Demo">Demo</option>
                <option value="Imported">Imported</option>
              </select>
            </div>
            <div className="field">
              <label htmlFor="client-billing">Next billing</label>
              <input
                className="input"
                id="client-billing"
                onChange={(event) => setForm({ ...form, nextBillingAt: event.target.value })}
                required={requiresBillingDate}
                type="date"
                value={form.nextBillingAt}
              />
            </div>
          </div>
          <div className="form-actions">
            <button className="button button-secondary" onClick={closeForm} type="button">
              Cancel
            </button>
            <button className="button" disabled={isSaving} type="submit">
              {isSaving ? "Saving" : "Save client"}
            </button>
          </div>
        </form>
      ) : null}

      {clients.length === 0 ? (
        <div className="state">No clients</div>
      ) : filteredClients.length === 0 ? (
        <div className="state">No results</div>
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
                  <td>{client.name}</td>
                  <td>{client.maskedEmail}</td>
                  <td>{client.status}</td>
                  <td><span className="badge">{client.planStage}</span></td>
                  <td>{client.dataSource}</td>
                  <td>{formatDate(client.nextBillingAt)}</td>
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
    </div>
  );
}

function toApiDate(value: string): string | null {
  return value ? `${value}T00:00:00Z` : null;
}

function toDateInputValue(value: string | null): string {
  return value ? value.slice(0, 10) : "";
}

function formatDate(value: string | null): string {
  return value ? new Intl.DateTimeFormat("en-US").format(new Date(value)) : "-";
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
