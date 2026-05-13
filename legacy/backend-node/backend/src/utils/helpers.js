import { v4 as uuidv4 } from "uuid";

export function generateId() {
  return uuidv4();
}

export function generateOrderId() {
  const prefix = "#";
  const random = Math.floor(Math.random() * 10000);
  return `${prefix}${1000 + random}`;
}

export function formatDate(date) {
  return new Date(date).toISOString().split("T")[0];
}

export function getCurrentTimestamp() {
  return new Date().toISOString();
}
