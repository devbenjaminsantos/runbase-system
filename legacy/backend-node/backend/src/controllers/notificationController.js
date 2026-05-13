import db from "../db/database.js";
import { generateId } from "../utils/helpers.js";

export async function getUserNotifications(req, res) {
  try {
    const notifications = await db
      .prepare(
        `
      SELECT id, type, title, message, read, created_at
      FROM notifications
      WHERE user_id = ?
      ORDER BY created_at DESC
      LIMIT 50
    `,
      )
      .all(req.user.id);

    res.json(notifications);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

export async function createNotification(req, res) {
  try {
    const { user_id, type, title, message } = req.body;

    if (!user_id || !type || !title || !message) {
      return res.status(400).json({ error: "Missing required fields" });
    }

    const id = generateId();

    const stmt = db.prepare(`
      INSERT INTO notifications (id, user_id, type, title, message, read)
      VALUES (?, ?, ?, ?, ?, 0)
    `);

    await stmt.run(id, user_id, type, title, message);

    // Broadcast via Socket.io
    // io.to(user_id).emit('notification', { id, type, title, message });

    res.status(201).json({ message: "Notification created", id });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

export async function markAsRead(req, res) {
  try {
    const { id } = req.params;

    const stmt = db.prepare(`
      UPDATE notifications SET read = 1 WHERE id = ? AND user_id = ?
    `);

    const result = await stmt.run(id, req.user.id);

    if (result.changes === 0) {
      return res.status(404).json({ error: "Notification not found" });
    }

    res.json({ message: "Notification marked as read" });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

export async function getUnreadCount(req, res) {
  try {
    const count = await db
      .prepare(
        `
      SELECT COUNT(*) as count FROM notifications
      WHERE user_id = ? AND read = 0
    `,
      );
    const result = await count.get(req.user.id);

    res.json({ unread: result.count });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}
