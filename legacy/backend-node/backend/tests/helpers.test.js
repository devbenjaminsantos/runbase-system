import test from "node:test";
import assert from "node:assert/strict";
import {
  formatDate,
  generateId,
  generateOrderId,
  getCurrentTimestamp,
} from "../src/utils/helpers.js";

test("generateId returns a non-empty uuid-like string", () => {
  const id = generateId();

  assert.equal(typeof id, "string");
  assert.ok(id.length >= 32);
  assert.match(id, /^[0-9a-f-]+$/i);
});

test("generateOrderId returns a hash-prefixed order identifier", () => {
  const orderId = generateOrderId();

  assert.match(orderId, /^#\d{4,5}$/);
});

test("formatDate returns YYYY-MM-DD", () => {
  const result = formatDate("2026-04-14T10:15:30.000Z");

  assert.equal(result, "2026-04-14");
});

test("getCurrentTimestamp returns an ISO-8601 string", () => {
  const timestamp = getCurrentTimestamp();

  assert.match(
    timestamp,
    /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}Z$/,
  );
});
