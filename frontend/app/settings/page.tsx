"use client";

import { ProtectedPage } from "../../components/ProtectedPage";

export default function SettingsPage() {
  return (
    <ProtectedPage roles={["Admin"]} subtitle="Current profile" title="Settings">
      {(user) => (
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
      )}
    </ProtectedPage>
  );
}
