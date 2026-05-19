"use client";

import { useEffect, useState } from "react";
import { ProtectedPage } from "../../components/ProtectedPage";
import { apiFetch } from "../../lib/api";

type UserRow = {
  id: string;
  name: string;
  email: string;
  role: string;
  status: string;
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

  useEffect(() => {
    apiFetch<UserRow[]>("/api/users")
      .then((result) => {
        setUsers(result);
        setState("ready");
      })
      .catch(() => setState("error"));
  }, []);

  if (state === "loading") {
    return <div className="state">Loading</div>;
  }

  if (state === "error") {
    return <div className="state state-error">Unable to load users</div>;
  }

  if (users.length === 0) {
    return <div className="state">No users</div>;
  }

  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Role</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => (
            <tr key={user.id}>
              <td>{user.name}</td>
              <td>{user.email}</td>
              <td><span className="badge">{user.role}</span></td>
              <td>{user.status}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
