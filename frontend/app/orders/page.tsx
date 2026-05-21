"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { Edit2, Plus, RefreshCcw, Trash2, X } from "lucide-react";
import { ProtectedPage } from "../../components/ProtectedPage";
import { ApiError, apiFetch } from "../../lib/api";

type PlanStage = "Trial" | "Free" | "Plus" | "Premium";
type OrderStatus = "Pending" | "Processing" | "Completed" | "Cancelled";

type OrderRow = {
  id: string;
  clientId: string;
  planStage: PlanStage;
  status: OrderStatus;
  finalAmount: number;
};

type ClientRow = {
  id: string;
  name: string;
  maskedEmail: string;
};

type OrderForm = {
  clientId: string;
  planStage: PlanStage;
  status: OrderStatus;
  finalAmount: string;
};

const emptyForm: OrderForm = {
  clientId: "",
  planStage: "Free",
  status: "Pending",
  finalAmount: "0"
};

export default function OrdersPage() {
  return (
    <ProtectedPage roles={["Admin", "Manager", "Support"]} subtitle="Commercial activity" title="Orders">
      {() => <OrdersTable />}
    </ProtectedPage>
  );
}

function OrdersTable() {
  const [orders, setOrders] = useState<OrderRow[]>([]);
  const [clients, setClients] = useState<ClientRow[]>([]);
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");
  const [query, setQuery] = useState("");
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingOrder, setEditingOrder] = useState<OrderRow | null>(null);
  const [form, setForm] = useState<OrderForm>(emptyForm);
  const [message, setMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    void loadData();
  }, []);

  const clientById = useMemo(() => {
    return new Map(clients.map((client) => [client.id, client]));
  }, [clients]);

  const filteredOrders = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();

    if (!normalizedQuery) {
      return orders;
    }

    return orders.filter((order) => {
      const client = clientById.get(order.clientId);
      return [
        client?.name ?? order.clientId,
        client?.maskedEmail ?? "",
        order.planStage,
        order.status,
        String(order.finalAmount)
      ]
        .join(" ")
        .toLowerCase()
        .includes(normalizedQuery);
    });
  }, [clientById, orders, query]);

  async function loadData() {
    setState("loading");

    try {
      const [ordersResult, clientsResult] = await Promise.all([
        apiFetch<OrderRow[]>("/api/orders"),
        apiFetch<ClientRow[]>("/api/clients")
      ]);
      setOrders(ordersResult);
      setClients(clientsResult);
      setState("ready");
    } catch {
      setState("error");
    }
  }

  function openCreateForm() {
    setEditingOrder(null);
    setForm({
      ...emptyForm,
      clientId: clients[0]?.id ?? ""
    });
    setMessage(null);
    setIsFormOpen(true);
  }

  function openEditForm(order: OrderRow) {
    setEditingOrder(order);
    setForm({
      clientId: order.clientId,
      planStage: order.planStage,
      status: order.status,
      finalAmount: String(order.finalAmount)
    });
    setMessage(null);
    setIsFormOpen(true);
  }

  function closeForm() {
    setIsFormOpen(false);
    setEditingOrder(null);
    setForm(emptyForm);
    setMessage(null);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSaving(true);
    setMessage(null);

    try {
      const payload = {
        clientId: form.clientId,
        planStage: form.planStage,
        status: form.status,
        finalAmount: Number(form.finalAmount)
      };

      if (editingOrder) {
        await apiFetch<OrderRow>(`/api/orders/${editingOrder.id}`, {
          method: "PUT",
          headers: {
            "content-type": "application/json"
          },
          body: JSON.stringify(payload)
        });
      } else {
        await apiFetch<OrderRow>("/api/orders", {
          method: "POST",
          headers: {
            "content-type": "application/json"
          },
          body: JSON.stringify(payload)
        });
      }

      await loadData();
      closeForm();
    } catch (error) {
      setMessage(getOrderErrorMessage(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function handleStatusChange(order: OrderRow, status: OrderStatus) {
    setMessage(null);

    try {
      await apiFetch<OrderRow>(`/api/orders/${order.id}/status`, {
        method: "PATCH",
        headers: {
          "content-type": "application/json"
        },
        body: JSON.stringify({ status })
      });
      await loadData();
    } catch (error) {
      setMessage(getOrderErrorMessage(error));
    }
  }

  async function handleDelete(order: OrderRow) {
    setMessage(null);

    try {
      await apiFetch<void>(`/api/orders/${order.id}`, {
        method: "DELETE"
      });
      await loadData();
    } catch (error) {
      setMessage(getOrderErrorMessage(error));
    }
  }

  if (state === "loading") {
    return <div className="state">Loading</div>;
  }

  if (state === "error") {
    return <div className="state state-error">Unable to load orders</div>;
  }

  return (
    <div className="stack">
      <div className="toolbar">
        <input
          className="input toolbar-search"
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Search orders"
          type="search"
          value={query}
        />
        <button className="button button-secondary" onClick={() => void loadData()} type="button">
          <RefreshCcw aria-hidden size={16} />
        </button>
        <button className="button" disabled={clients.length === 0} onClick={openCreateForm} type="button">
          <Plus aria-hidden size={16} />
          <span>New order</span>
        </button>
      </div>

      {clients.length === 0 ? <div className="state">Create a client before creating orders.</div> : null}
      {message ? <div className="alert alert-error">{message}</div> : null}

      {isFormOpen ? (
        <form className="form-panel" onSubmit={handleSubmit}>
          <div className="form-panel-header">
            <strong>{editingOrder ? "Edit order" : "Create order"}</strong>
            <button className="icon-button" onClick={closeForm} title="Close" type="button">
              <X aria-hidden size={16} />
            </button>
          </div>
          <div className="form-grid">
            <div className="field">
              <label htmlFor="order-client">Client</label>
              <select
                className="input"
                id="order-client"
                onChange={(event) => setForm({ ...form, clientId: event.target.value })}
                required
                value={form.clientId}
              >
                {clients.map((client) => (
                  <option key={client.id} value={client.id}>
                    {client.name} ({client.maskedEmail})
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label htmlFor="order-plan">Plan</label>
              <select
                className="input"
                id="order-plan"
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
              <label htmlFor="order-status">Status</label>
              <select
                className="input"
                id="order-status"
                onChange={(event) => setForm({ ...form, status: event.target.value as OrderStatus })}
                value={form.status}
              >
                <option value="Pending">Pending</option>
                <option value="Processing">Processing</option>
                <option value="Completed">Completed</option>
                <option value="Cancelled">Cancelled</option>
              </select>
            </div>
            <div className="field">
              <label htmlFor="order-amount">Final amount</label>
              <input
                className="input"
                id="order-amount"
                min={0}
                onChange={(event) => setForm({ ...form, finalAmount: event.target.value })}
                required
                step="0.01"
                type="number"
                value={form.finalAmount}
              />
            </div>
          </div>
          <div className="form-actions">
            <button className="button button-secondary" onClick={closeForm} type="button">
              Cancel
            </button>
            <button className="button" disabled={isSaving} type="submit">
              {isSaving ? "Saving" : "Save order"}
            </button>
          </div>
        </form>
      ) : null}

      {orders.length === 0 ? (
        <div className="state">No orders</div>
      ) : filteredOrders.length === 0 ? (
        <div className="state">No results</div>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Client</th>
                <th>Plan</th>
                <th>Status</th>
                <th>Amount</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredOrders.map((order) => {
                const client = clientById.get(order.clientId);

                return (
                  <tr key={order.id}>
                    <td>{client ? `${client.name} (${client.maskedEmail})` : order.clientId}</td>
                    <td><span className="badge">{order.planStage}</span></td>
                    <td>
                      <select
                        className="input table-select"
                        onChange={(event) => void handleStatusChange(order, event.target.value as OrderStatus)}
                        value={order.status}
                      >
                        <option value="Pending">Pending</option>
                        <option value="Processing">Processing</option>
                        <option value="Completed">Completed</option>
                        <option value="Cancelled">Cancelled</option>
                      </select>
                    </td>
                    <td>{formatCurrency(order.finalAmount)}</td>
                    <td>
                      <div className="row-actions">
                        <button className="icon-button" onClick={() => openEditForm(order)} title="Edit order" type="button">
                          <Edit2 aria-hidden size={16} />
                        </button>
                        <button className="icon-button danger-button" onClick={() => void handleDelete(order)} title="Delete order" type="button">
                          <Trash2 aria-hidden size={16} />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD"
  }).format(value);
}

function getOrderErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 400) {
      return "Invalid order amount or status transition.";
    }

    if (error.status === 403) {
      return "Permission denied.";
    }

    if (error.status === 404) {
      return "Client or order was not found.";
    }
  }

  return "Unable to save order.";
}
