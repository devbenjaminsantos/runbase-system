import test from "node:test";
import assert from "node:assert/strict";
import {
  createRefreshToken,
  createToken,
  hashPassword,
  verifyPassword,
  verifyRefreshToken,
  verifyToken,
} from "../src/utils/auth.js";

const sampleUser = {
  id: "user-123",
  email: "tester@example.com",
  role: "admin",
};

test("hashPassword stores a non-plain-text value", async () => {
  const password = "Admin123!";
  const hash = await hashPassword(password);

  assert.notEqual(hash, password);
  assert.ok(hash.length > password.length);
});

test("verifyPassword accepts the correct password", async () => {
  const password = "Admin123!";
  const hash = await hashPassword(password);

  const valid = await verifyPassword(password, hash);

  assert.equal(valid, true);
});

test("verifyPassword rejects an invalid password", async () => {
  const hash = await hashPassword("Admin123!");

  const valid = await verifyPassword("WrongPassword!", hash);

  assert.equal(valid, false);
});

test("createToken and verifyToken round-trip the user payload", () => {
  const token = createToken(sampleUser);
  const decoded = verifyToken(token);

  assert.ok(decoded);
  assert.equal(decoded.id, sampleUser.id);
  assert.equal(decoded.email, sampleUser.email);
  assert.equal(decoded.role, sampleUser.role);
});

test("verifyToken returns null for invalid token input", () => {
  const decoded = verifyToken("not-a-valid-token");

  assert.equal(decoded, null);
});

test("createRefreshToken and verifyRefreshToken round-trip the user id", () => {
  const token = createRefreshToken(sampleUser);
  const decoded = verifyRefreshToken(token);

  assert.ok(decoded);
  assert.equal(decoded.id, sampleUser.id);
});

test("verifyRefreshToken returns null for invalid token input", () => {
  const decoded = verifyRefreshToken("not-a-valid-refresh-token");

  assert.equal(decoded, null);
});
