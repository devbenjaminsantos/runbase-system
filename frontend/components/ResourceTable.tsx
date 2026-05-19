"use client";

import { useEffect, useState } from "react";
import { ProtectedPage } from "./ProtectedPage";
import { apiFetch } from "../lib/api";
import type { UserRole } from "../lib/types";

type Row = Record<string, unknown> & { id: string };

export function ResourceTable({
  title,
  subtitle,
  path,
  roles,
  columns
}: {
  title: string;
  subtitle: string;
  path: string;
  roles: UserRole[];
  columns: Array<[string, string]>;
}) {
  return (
    <ProtectedPage roles={roles} subtitle={subtitle} title={title}>
      {() => <Table path={path} columns={columns} title={title} />}
    </ProtectedPage>
  );
}

function Table({
  path,
  columns,
  title
}: {
  path: string;
  columns: Array<[string, string]>;
  title: string;
}) {
  const [rows, setRows] = useState<Row[]>([]);
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");

  useEffect(() => {
    apiFetch<Row[]>(path)
      .then((result) => {
        setRows(result);
        setState("ready");
      })
      .catch(() => setState("error"));
  }, [path]);

  if (state === "loading") {
    return <div className="state">Loading</div>;
  }

  if (state === "error") {
    return <div className="state state-error">Unable to load {title.toLowerCase()}</div>;
  }

  if (rows.length === 0) {
    return <div className="state">No records</div>;
  }

  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            {columns.map(([key, label]) => (
              <th key={key}>{label}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.id}>
              {columns.map(([key]) => (
                <td key={key}>{formatValue(row[key])}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function formatValue(value: unknown): string {
  if (typeof value === "boolean") {
    return value ? "Yes" : "No";
  }

  if (typeof value === "number") {
    return new Intl.NumberFormat("en-US", {
      maximumFractionDigits: 2
    }).format(value);
  }

  if (value === null || value === undefined) {
    return "-";
  }

  return String(value);
}
