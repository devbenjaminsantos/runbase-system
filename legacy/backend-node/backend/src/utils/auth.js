import jwt from "jsonwebtoken";
import bcryptjs from "bcryptjs";
import { config } from "../config/config.js";

// Hash de senha
export async function hashPassword(password) {
  return await bcryptjs.hash(password, 10);
}

// Verificar senha
export async function verifyPassword(password, hash) {
  return await bcryptjs.compare(password, hash);
}

// Criar JWT
export function createToken(user) {
  return jwt.sign(
    { id: user.id, email: user.email, role: user.role },
    config.JWT_SECRET,
    { expiresIn: config.JWT_EXPIRES_IN },
  );
}

// Verificar JWT
export function verifyToken(token) {
  try {
    return jwt.verify(token, config.JWT_SECRET);
  } catch (error) {
    return null;
  }
}

// Refresh token
export function createRefreshToken(user) {
  return jwt.sign({ id: user.id }, config.JWT_SECRET, { expiresIn: "30d" });
}

export function verifyRefreshToken(token) {
  try {
    return jwt.verify(token, config.JWT_SECRET);
  } catch (error) {
    return null;
  }
}
