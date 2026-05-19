"use client";

import { ResourceTable } from "../../components/ResourceTable";

export default function ClientsPage() {
  return (
    <ResourceTable
      columns={[
        ["name", "Name"],
        ["maskedEmail", "Email"],
        ["status", "Status"],
        ["planStage", "Plan"]
      ]}
      path="/api/clients"
      roles={["Admin", "Manager"]}
      subtitle="Accounts and subscriptions"
      title="Clients"
    />
  );
}
