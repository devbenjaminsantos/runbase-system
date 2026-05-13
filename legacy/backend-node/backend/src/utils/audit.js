import db from "../db/database.js";
import { generateId } from "./helpers.js";

export async function logAuditAction({
  userId = null,
  action,
  resourceType,
  resourceId,
  changes = null,
}) {
  if (!action || !resourceType || !resourceId) return;

  const serializedChanges =
    changes === null || changes === undefined ? null : JSON.stringify(changes);

  await db
    .prepare(
      `
      INSERT INTO audit_logs (id, user_id, action, resource_type, resource_id, changes)
      VALUES (?, ?, ?, ?, ?, ?)
    `,
    )
    .run(
      generateId(),
      userId,
      action,
      resourceType,
      resourceId,
      serializedChanges,
    );
}
