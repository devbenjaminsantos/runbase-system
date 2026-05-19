"use client";

import { ResourceTable } from "../../components/ResourceTable";

export default function OrdersPage() {
  return (
    <ResourceTable
      columns={[
        ["clientId", "Client"],
        ["planStage", "Plan"],
        ["status", "Status"],
        ["finalAmount", "Amount"]
      ]}
      path="/api/orders"
      roles={["Admin", "Manager", "Support"]}
      subtitle="Commercial activity"
      title="Orders"
    />
  );
}
