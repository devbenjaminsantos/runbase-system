import db from "../db/database.js";
import { generateId } from "../utils/helpers.js";
import { hashPassword } from "../utils/auth.js";
import { logAuditAction } from "../utils/audit.js";

export async function getAllUsers(req, res) {
  try {
    const users = await db
      .prepare(
        `
      SELECT id, name, email, role, status, created_at, updated_at FROM users
    `,
      )
      .all();

    res.json(users);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

export async function getUserById(req, res) {
  try {
    const { id } = req.params;
    const user = await db
      .prepare(
        `
      SELECT id, name, email, role, status, created_at, updated_at FROM users WHERE id = ?
    `,
      )
      .get(id);

    if (!user) {
      return res.status(404).json({ error: "User not found" });
    }

    res.json(user);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

export async function createUser(req, res) {
  try {
    const { name, email, password, role, status } = req.body;

    if (!name || !email || !password) {
      return res.status(400).json({ error: "Missing required fields" });
    }

    const id = generateId();
    const hashedPassword = await hashPassword(password);

    const stmt = db.prepare(`
      INSERT INTO users (id, name, email, password, role, status)
      VALUES (?, ?, ?, ?, ?, ?)
    `);

    await stmt.run(
      id,
      name,
      email,
      hashedPassword,
      role || "user",
      status || "active",
    );

    const user = await db
      .prepare("SELECT id, name, email, role, status FROM users WHERE id = ?")
      .get(id);

    await logAuditAction({
      userId: req.user?.id || null,
      action: "create",
      resourceType: "user",
      resourceId: id,
      changes: user,
    });

    res.status(201).json({ message: "User created", user });
  } catch (error) {
    if (
      error.message.includes("UNIQUE constraint failed") ||
      error.message.includes("Duplicate entry")
    ) {
      return res.status(409).json({ error: "Email already exists" });
    }
    res.status(500).json({ error: error.message });
  }
}

export async function updateUser(req, res) {
  try {
    const { id } = req.params;
    const { name, email, role, status, password } = req.body;

    const user = await db.prepare("SELECT * FROM users WHERE id = ?").get(id);
    if (!user) {
      return res.status(404).json({ error: "User not found" });
    }

    let hashedPassword = user.password;
    if (password) {
      hashedPassword = await hashPassword(password);
    }

    const stmt = db.prepare(`
      UPDATE users SET 
        name = ?, 
        email = ?, 
        role = ?, 
        status = ?, 
        password = ?,
        updated_at = CURRENT_TIMESTAMP
      WHERE id = ?
    `);

    await stmt.run(
      name || user.name,
      email || user.email,
      role || user.role,
      status || user.status,
      hashedPassword,
      id,
    );

    const updated = await db
      .prepare("SELECT id, name, email, role, status FROM users WHERE id = ?")
      .get(id);

    await logAuditAction({
      userId: req.user?.id || null,
      action: "update",
      resourceType: "user",
      resourceId: id,
      changes: { before: { ...user, password: undefined }, after: updated },
    });

    res.json({ message: "User updated", user: updated });
  } catch (error) {
    if (error.message.includes("Duplicate entry")) {
      return res.status(409).json({ error: "Email already exists" });
    }
    res.status(500).json({ error: error.message });
  }
}

export async function deleteUser(req, res) {
  try {
    const { id } = req.params;

    const stmt = db.prepare("DELETE FROM users WHERE id = ?");
    const result = await stmt.run(id);

    if (result.changes === 0) {
      return res.status(404).json({ error: "User not found" });
    }

    await logAuditAction({
      userId: req.user?.id || null,
      action: "delete",
      resourceType: "user",
      resourceId: id,
    });

    res.json({ message: "User deleted" });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

export async function getUserStats(req, res) {
  try {
    const stats = {
      total: (await db.prepare("SELECT COUNT(*) as count FROM users").get())
        .count,
      active: (
        await db
        .prepare("SELECT COUNT(*) as count FROM users WHERE status = 'active'")
        .get()
      ).count,
      inactive: (
        await db
        .prepare(
          "SELECT COUNT(*) as count FROM users WHERE status = 'inactive'",
        )
        .get()
      ).count,
      byRole: await db
        .prepare(`SELECT role, COUNT(*) as count FROM users GROUP BY role`)
        .all(),
    };

    res.json(stats);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}
