"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { Edit2, Plus, RefreshCcw, Trash2, X } from "lucide-react";
import { ProtectedPage } from "../../components/ProtectedPage";
import { ApiError, apiFetch } from "../../lib/api";

type UserRow = {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  status: UserStatus;
};

type UserRole = "Admin" | "Manager" | "Support" | "Viewer";
type UserStatus = "Active" | "Inactive";

type UserForm = {
  name: string;
  email: string;
  password: string;
  role: UserRole;
  status: UserStatus;
};

const emptyForm: UserForm = {
  name: "",
  email: "",
  password: "",
  role: "Viewer",
  status: "Active"
};

export default function UsersPage() {
  return (
    <ProtectedPage roles={["Admin"]} subtitle="Identity and access" title="Users">
      {() => <UsersTable />}
    </ProtectedPage>
  );
}

function UsersTable() {
  const [users, setUsers] = useState<UserRow[]>([]);
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");
  const [query, setQuery] = useState("");
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserRow | null>(null);
  const [form, setForm] = useState<UserForm>(emptyForm);
  const [message, setMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    void loadUsers();
  }, []);

  const filteredUsers = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();

    if (!normalizedQuery) {
      return users;
    }

    return users.filter((user) =>
      [user.name, user.email, user.role, user.status]
        .join(" ")
        .toLowerCase()
        .includes(normalizedQuery));
  }, [query, users]);

  async function loadUsers() {
    setState("loading");

    try {
      const result = await apiFetch<UserRow[]>("/api/users");
      setUsers(result);
      setState("ready");
    } catch {
      setState("error");
    }
  }

  function openCreateForm() {
    setEditingUser(null);
    setForm(emptyForm);
    setMessage(null);
    setIsFormOpen(true);
  }

  function openEditForm(user: UserRow) {
    setEditingUser(user);
    setForm({
      name: user.name,
      email: user.email,
      password: "",
      role: user.role,
      status: user.status
    });
    setMessage(null);
    setIsFormOpen(true);
  }

  function closeForm() {
    setIsFormOpen(false);
    setEditingUser(null);
    setForm(emptyForm);
    setMessage(null);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSaving(true);
    setMessage(null);

    try {
      if (editingUser) {
        await apiFetch<UserRow>(`/api/users/${editingUser.id}`, {
          method: "PUT",
          headers: {
            "content-type": "application/json"
          },
          body: JSON.stringify({
            name: form.name,
            email: form.email,
            role: form.role,
            status: form.status
          })
        });
      } else {
        await apiFetch<UserRow>("/api/users", {
          method: "POST",
          headers: {
            "content-type": "application/json"
          },
          body: JSON.stringify(form)
        });
      }

      await loadUsers();
      closeForm();
    } catch (error) {
      setMessage(getUserErrorMessage(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(user: UserRow) {
    setMessage(null);

    try {
      await apiFetch<void>(`/api/users/${user.id}`, {
        method: "DELETE"
      });
      await loadUsers();
    } catch (error) {
      setMessage(getUserErrorMessage(error));
    }
  }

  if (state === "loading") {
    return <div className="state">Loading</div>;
  }

  if (state === "error") {
    return <div className="state state-error">Unable to load users</div>;
  }

  return (
    <div className="stack">
      <div className="toolbar">
        <input
          className="input toolbar-search"
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Search users"
          type="search"
          value={query}
        />
        <button className="button button-secondary" onClick={() => void loadUsers()} type="button">
          <RefreshCcw aria-hidden size={16} />
        </button>
        <button className="button" onClick={openCreateForm} type="button">
          <Plus aria-hidden size={16} />
          <span>New user</span>
        </button>
      </div>

      {message ? <div className="alert alert-error">{message}</div> : null}

      {isFormOpen ? (
        <form className="form-panel" onSubmit={handleSubmit}>
          <div className="form-panel-header">
            <strong>{editingUser ? "Edit user" : "Create user"}</strong>
            <button className="icon-button" onClick={closeForm} title="Close" type="button">
              <X aria-hidden size={16} />
            </button>
          </div>
          <div className="form-grid">
            <div className="field">
              <label htmlFor="user-name">Name</label>
              <input
                className="input"
                id="user-name"
                minLength={2}
                onChange={(event) => setForm({ ...form, name: event.target.value })}
                required
                value={form.name}
              />
            </div>
            <div className="field">
              <label htmlFor="user-email">Email</label>
              <input
                className="input"
                id="user-email"
                onChange={(event) => setForm({ ...form, email: event.target.value })}
                required
                type="email"
                value={form.email}
              />
            </div>
            {!editingUser ? (
              <div className="field">
                <label htmlFor="user-password">Password</label>
                <input
                  className="input"
                  id="user-password"
                  minLength={8}
                  onChange={(event) => setForm({ ...form, password: event.target.value })}
                  required
                  type="password"
                  value={form.password}
                />
              </div>
            ) : null}
            <div className="field">
              <label htmlFor="user-role">Role</label>
              <select
                className="input"
                id="user-role"
                onChange={(event) => setForm({ ...form, role: event.target.value as UserRole })}
                value={form.role}
              >
                <option value="Admin">Admin</option>
                <option value="Manager">Manager</option>
                <option value="Support">Support</option>
                <option value="Viewer">Viewer</option>
              </select>
            </div>
            <div className="field">
              <label htmlFor="user-status">Status</label>
              <select
                className="input"
                id="user-status"
                onChange={(event) => setForm({ ...form, status: event.target.value as UserStatus })}
                value={form.status}
              >
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
              </select>
            </div>
          </div>
          <div className="form-actions">
            <button className="button button-secondary" onClick={closeForm} type="button">
              Cancel
            </button>
            <button className="button" disabled={isSaving} type="submit">
              {isSaving ? "Saving" : "Save user"}
            </button>
          </div>
        </form>
      ) : null}

      {users.length === 0 ? (
        <div className="state">No users</div>
      ) : filteredUsers.length === 0 ? (
        <div className="state">No results</div>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map((user) => (
                <tr key={user.id}>
                  <td>{user.name}</td>
                  <td>{user.email}</td>
                  <td><span className="badge">{user.role}</span></td>
                  <td>{user.status}</td>
                  <td>
                    <div className="row-actions">
                      <button className="icon-button" onClick={() => openEditForm(user)} title="Edit user" type="button">
                        <Edit2 aria-hidden size={16} />
                      </button>
                      <button className="icon-button danger-button" onClick={() => void handleDelete(user)} title="Delete user" type="button">
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

function getUserErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 409) {
      return "Email already exists.";
    }

    if (error.status === 400) {
      return "Operation rejected. Check role, status, or last active admin rule.";
    }

    if (error.status === 403) {
      return "Permission denied.";
    }
  }

  return "Unable to save user.";
}
