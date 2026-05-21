"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { Edit2, Plus, Power, RefreshCcw, Trash2, X } from "lucide-react";
import { ProtectedPage } from "../../components/ProtectedPage";
import { ApiError, apiFetch } from "../../lib/api";

type PlanStage = "Trial" | "Free" | "Plus" | "Premium";
type BillingCycle = "None" | "Monthly" | "Yearly";

type PlanRow = {
  id: string;
  name: string;
  stage: PlanStage;
  price: number;
  billingCycle: BillingCycle;
  isActive: boolean;
  nextBillingAt: string | null;
};

type PlanForm = {
  name: string;
  stage: PlanStage;
  price: string;
  billingCycle: BillingCycle;
  isActive: boolean;
  nextBillingAt: string;
};

const emptyForm: PlanForm = {
  name: "",
  stage: "Free",
  price: "0",
  billingCycle: "None",
  isActive: true,
  nextBillingAt: ""
};

export default function PlansPage() {
  return (
    <ProtectedPage roles={["Admin", "Manager"]} subtitle="Catalog and billing" title="Plans">
      {() => <PlansTable />}
    </ProtectedPage>
  );
}

function PlansTable() {
  const [plans, setPlans] = useState<PlanRow[]>([]);
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");
  const [query, setQuery] = useState("");
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingPlan, setEditingPlan] = useState<PlanRow | null>(null);
  const [form, setForm] = useState<PlanForm>(emptyForm);
  const [message, setMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    void loadPlans();
  }, []);

  const filteredPlans = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();

    if (!normalizedQuery) {
      return plans;
    }

    return plans.filter((plan) =>
      [plan.name, plan.stage, plan.billingCycle, plan.isActive ? "active" : "inactive"]
        .join(" ")
        .toLowerCase()
        .includes(normalizedQuery));
  }, [plans, query]);

  async function loadPlans() {
    setState("loading");

    try {
      const result = await apiFetch<PlanRow[]>("/api/plans");
      setPlans(result);
      setState("ready");
    } catch {
      setState("error");
    }
  }

  function openCreateForm() {
    setEditingPlan(null);
    setForm(emptyForm);
    setMessage(null);
    setIsFormOpen(true);
  }

  function openEditForm(plan: PlanRow) {
    setEditingPlan(plan);
    setForm({
      name: plan.name,
      stage: plan.stage,
      price: String(plan.price),
      billingCycle: plan.billingCycle,
      isActive: plan.isActive,
      nextBillingAt: toDateInputValue(plan.nextBillingAt)
    });
    setMessage(null);
    setIsFormOpen(true);
  }

  function closeForm() {
    setIsFormOpen(false);
    setEditingPlan(null);
    setForm(emptyForm);
    setMessage(null);
  }

  function updateStage(stage: PlanStage) {
    const paid = isPaidStage(stage);
    setForm({
      ...form,
      stage,
      price: paid && form.price === "0" ? "" : form.price,
      billingCycle: paid && form.billingCycle === "None" ? "Monthly" : form.billingCycle,
      nextBillingAt: paid ? form.nextBillingAt : ""
    });
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSaving(true);
    setMessage(null);

    try {
      const payload = {
        name: form.name,
        stage: form.stage,
        price: Number(form.price),
        billingCycle: form.billingCycle,
        isActive: form.isActive,
        nextBillingAt: toApiDate(form.nextBillingAt)
      };

      if (editingPlan) {
        await apiFetch<PlanRow>(`/api/plans/${editingPlan.id}`, {
          method: "PUT",
          headers: {
            "content-type": "application/json"
          },
          body: JSON.stringify(payload)
        });
      } else {
        await apiFetch<PlanRow>("/api/plans", {
          method: "POST",
          headers: {
            "content-type": "application/json"
          },
          body: JSON.stringify(payload)
        });
      }

      await loadPlans();
      closeForm();
    } catch (error) {
      setMessage(getPlanErrorMessage(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function handleToggleActive(plan: PlanRow) {
    setMessage(null);

    try {
      await apiFetch<PlanRow>(`/api/plans/${plan.id}/active`, {
        method: "PATCH",
        headers: {
          "content-type": "application/json"
        },
        body: JSON.stringify({ isActive: !plan.isActive })
      });
      await loadPlans();
    } catch (error) {
      setMessage(getPlanErrorMessage(error));
    }
  }

  async function handleDelete(plan: PlanRow) {
    setMessage(null);

    try {
      await apiFetch<void>(`/api/plans/${plan.id}`, {
        method: "DELETE"
      });
      await loadPlans();
    } catch (error) {
      setMessage(getPlanErrorMessage(error));
    }
  }

  const paidStage = isPaidStage(form.stage);

  if (state === "loading") {
    return <div className="state">Loading</div>;
  }

  if (state === "error") {
    return <div className="state state-error">Unable to load plans</div>;
  }

  return (
    <div className="stack">
      <div className="toolbar">
        <input
          className="input toolbar-search"
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Search plans"
          type="search"
          value={query}
        />
        <button className="button button-secondary" onClick={() => void loadPlans()} type="button">
          <RefreshCcw aria-hidden size={16} />
        </button>
        <button className="button" onClick={openCreateForm} type="button">
          <Plus aria-hidden size={16} />
          <span>New plan</span>
        </button>
      </div>

      {message ? <div className="alert alert-error">{message}</div> : null}

      {isFormOpen ? (
        <form className="form-panel" onSubmit={handleSubmit}>
          <div className="form-panel-header">
            <strong>{editingPlan ? "Edit plan" : "Create plan"}</strong>
            <button className="icon-button" onClick={closeForm} title="Close" type="button">
              <X aria-hidden size={16} />
            </button>
          </div>
          <div className="form-grid">
            <div className="field">
              <label htmlFor="plan-name">Name</label>
              <input
                className="input"
                id="plan-name"
                minLength={2}
                onChange={(event) => setForm({ ...form, name: event.target.value })}
                required
                value={form.name}
              />
            </div>
            <div className="field">
              <label htmlFor="plan-stage">Stage</label>
              <select
                className="input"
                id="plan-stage"
                onChange={(event) => updateStage(event.target.value as PlanStage)}
                value={form.stage}
              >
                <option value="Trial">Trial</option>
                <option value="Free">Free</option>
                <option value="Plus">Plus</option>
                <option value="Premium">Premium</option>
              </select>
            </div>
            <div className="field">
              <label htmlFor="plan-price">Price</label>
              <input
                className="input"
                id="plan-price"
                min={0}
                onChange={(event) => setForm({ ...form, price: event.target.value })}
                required
                step="0.01"
                type="number"
                value={form.price}
              />
            </div>
            <div className="field">
              <label htmlFor="plan-cycle">Billing cycle</label>
              <select
                className="input"
                id="plan-cycle"
                onChange={(event) => setForm({ ...form, billingCycle: event.target.value as BillingCycle })}
                value={form.billingCycle}
              >
                <option value="None">None</option>
                <option value="Monthly">Monthly</option>
                <option value="Yearly">Yearly</option>
              </select>
            </div>
            <div className="field">
              <label htmlFor="plan-billing">Next billing</label>
              <input
                className="input"
                id="plan-billing"
                onChange={(event) => setForm({ ...form, nextBillingAt: event.target.value })}
                required={paidStage}
                type="date"
                value={form.nextBillingAt}
              />
            </div>
            <label className="check-field">
              <input
                checked={form.isActive}
                onChange={(event) => setForm({ ...form, isActive: event.target.checked })}
                type="checkbox"
              />
              <span>Active</span>
            </label>
          </div>
          <div className="form-actions">
            <button className="button button-secondary" onClick={closeForm} type="button">
              Cancel
            </button>
            <button className="button" disabled={isSaving} type="submit">
              {isSaving ? "Saving" : "Save plan"}
            </button>
          </div>
        </form>
      ) : null}

      {plans.length === 0 ? (
        <div className="state">No plans</div>
      ) : filteredPlans.length === 0 ? (
        <div className="state">No results</div>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Stage</th>
                <th>Price</th>
                <th>Cycle</th>
                <th>Active</th>
                <th>Next billing</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredPlans.map((plan) => (
                <tr key={plan.id}>
                  <td>{plan.name}</td>
                  <td><span className="badge">{plan.stage}</span></td>
                  <td>{formatCurrency(plan.price)}</td>
                  <td>{plan.billingCycle}</td>
                  <td>{plan.isActive ? "Yes" : "No"}</td>
                  <td>{formatDate(plan.nextBillingAt)}</td>
                  <td>
                    <div className="row-actions">
                      <button className="icon-button" onClick={() => void handleToggleActive(plan)} title="Toggle active" type="button">
                        <Power aria-hidden size={16} />
                      </button>
                      <button className="icon-button" onClick={() => openEditForm(plan)} title="Edit plan" type="button">
                        <Edit2 aria-hidden size={16} />
                      </button>
                      <button className="icon-button danger-button" onClick={() => void handleDelete(plan)} title="Delete plan" type="button">
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

function isPaidStage(stage: PlanStage): boolean {
  return stage === "Plus" || stage === "Premium";
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

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD"
  }).format(value);
}

function getPlanErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 409) {
      return "A plan already exists for this stage.";
    }

    if (error.status === 400) {
      return "Plus and Premium plans require price, billing cycle, and next billing date.";
    }

    if (error.status === 403) {
      return "Permission denied.";
    }
  }

  return "Unable to save plan.";
}
