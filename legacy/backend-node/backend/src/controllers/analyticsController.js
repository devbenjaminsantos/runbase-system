import db from "../db/database.js";
import { generateId } from "../utils/helpers.js";

export async function trackAction(req, res) {
  try {
    const { action, resource, details } = req.body;
    const userId = req.user?.id || null;

    if (!action || !resource) {
      return res.status(400).json({ error: "Missing required fields" });
    }

    const id = generateId();
    const ipAddress = req.ip || req.connection.remoteAddress;

    const stmt = db.prepare(`
      INSERT INTO analytics (id, user_id, action, resource, details, ip_address)
      VALUES (?, ?, ?, ?, ?, ?)
    `);

    await stmt.run(
      id,
      userId,
      action,
      resource,
      JSON.stringify(details),
      ipAddress,
    );

    res.status(201).json({ message: "Action tracked" });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

export async function getAnalytics(req, res) {
  try {
    const { start_date, end_date } = req.query;

    let query = "SELECT * FROM analytics";
    const params = [];

    if (start_date) {
      query += " WHERE created_at >= ?";
      params.push(start_date);
    }

    if (end_date) {
      query += (start_date ? " AND" : " WHERE") + " created_at <= ?";
      params.push(end_date);
    }

    query += " ORDER BY created_at DESC LIMIT 1000";

    const analytics = await db.prepare(query).all(...params);

    res.json(analytics);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

export async function getAnalyticsSummary(req, res) {
  try {
    const summary = {
      totalActions: (
        await db.prepare("SELECT COUNT(*) as count FROM analytics").get()
      ).count,
      actionsByType: await db
        .prepare(
          `
        SELECT action, COUNT(*) as count FROM analytics
        GROUP BY action
      `,
        )
        .all(),
      actionsByResource: await db
        .prepare(
          `
        SELECT resource, COUNT(*) as count FROM analytics
        GROUP BY resource
      `,
        )
        .all(),
      activeUsers: (
        await db
        .prepare(
          `
        SELECT COUNT(DISTINCT user_id) as count FROM analytics
        WHERE user_id IS NOT NULL
      `,
        )
        .get()
      ).count,
      lastActions: await db
        .prepare(
          `
        SELECT 
          a.id, a.action, a.resource, a.created_at,
          u.name as user_name
        FROM analytics a
        LEFT JOIN users u ON a.user_id = u.id
        ORDER BY a.created_at DESC
        LIMIT 20
      `,
        )
        .all(),
    };

    res.json(summary);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

export async function getAuditLog(req, res) {
  try {
    const logs = await db
      .prepare(
        `
      SELECT 
        al.id, al.user_id, u.name as user_name, 
        al.action, al.resource_type, al.resource_id, 
        al.changes, al.timestamp
      FROM audit_logs al
      LEFT JOIN users u ON al.user_id = u.id
      ORDER BY al.timestamp DESC
      LIMIT 100
    `,
      )
      .all();

    res.json(logs);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}
