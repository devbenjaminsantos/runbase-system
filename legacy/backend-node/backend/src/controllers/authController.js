import db from "../db/database.js";
import {
  hashPassword,
  verifyPassword,
  createToken,
  createRefreshToken,
  verifyRefreshToken,
} from "../utils/auth.js";
import { generateId } from "../utils/helpers.js";

export async function register(req, res) {
  try {
    const { name, email, password } = req.body;

    if (!name || !email || !password) {
      return res.status(400).json({ error: "Missing required fields" });
    }

    const hashedPassword = await hashPassword(password);
    const id = generateId();

    const stmt = db.prepare(`
      INSERT INTO users (id, name, email, password, role, status)
      VALUES (?, ?, ?, ?, 'user', 'active')
    `);

    await stmt.run(id, name, email, hashedPassword);

    const user = await db
      .prepare("SELECT id, name, email, role FROM users WHERE id = ?")
      .get(id);
    const token = createToken(user);
    const refreshToken = createRefreshToken(user);

    res.status(201).json({
      message: "User registered successfully",
      user,
      token,
      refreshToken,
    });
  } catch (error) {
    if (
      error.message.includes("UNIQUE constraint failed") ||
      error.message.includes("Duplicate entry")
    ) {
      return res.status(409).json({ error: "Email already registered" });
    }
    res.status(500).json({ error: error.message });
  }
}

export async function login(req, res) {
  try {
    const { email, password } = req.body;

    if (!email || !password) {
      return res.status(400).json({ error: "Email and password required" });
    }

    const user = await db.prepare("SELECT * FROM users WHERE email = ?").get(email);

    if (!user) {
      return res.status(401).json({ error: "Invalid credentials" });
    }

    const validPassword = await verifyPassword(password, user.password);

    if (!validPassword) {
      return res.status(401).json({ error: "Invalid credentials" });
    }

    if (user.status === "inactive") {
      return res.status(403).json({ error: "User account is inactive" });
    }

    const token = createToken(user);
    const refreshToken = createRefreshToken(user);

    res.json({
      message: "Login successful",
      user: {
        id: user.id,
        name: user.name,
        email: user.email,
        role: user.role,
      },
      token,
      refreshToken,
    });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

export function logout(req, res) {
  // Token é invalidado no frontend removendo do localStorage
  // Opcionalmente, manter um blacklist no backend
  res.json({ message: "Logout successful" });
}

export async function getCurrentUser(req, res) {
  try {
    const user = await db
      .prepare(
        `
      SELECT id, name, email, role, status, created_at FROM users WHERE id = ?
    `,
      )
      .get(req.user.id);

    if (!user) {
      return res.status(404).json({ error: "User not found" });
    }

    res.json(user);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

export async function refreshToken(req, res) {
  try {
    const { refreshToken } = req.body;

    if (!refreshToken) {
      return res.status(400).json({ error: "Refresh token required" });
    }

    const decoded = verifyRefreshToken(refreshToken);
    if (!decoded?.id) {
      return res.status(401).json({ error: "Invalid refresh token" });
    }

    const user = await db
      .prepare("SELECT id, name, email, role FROM users WHERE id = ?")
      .get(decoded.id);

    if (!user) {
      return res.status(404).json({ error: "User not found" });
    }

    const newToken = createToken(user);
    const nextRefreshToken = createRefreshToken(user);

    res.json({ token: newToken, refreshToken: nextRefreshToken });
  } catch (error) {
    res.status(401).json({ error: "Invalid refresh token" });
  }
}
