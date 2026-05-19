"use client";

import { ResourceTable } from "../../components/ResourceTable";

export default function PlansPage() {
  return (
    <ResourceTable
      columns={[
        ["name", "Name"],
        ["stage", "Stage"],
        ["price", "Price"],
        ["isActive", "Active"]
      ]}
      path="/api/plans"
      roles={["Admin", "Manager"]}
      subtitle="Catalog and billing"
      title="Plans"
    />
  );
}
