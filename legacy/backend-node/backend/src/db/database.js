import initSqlJs from "sql.js";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { config } from "../config/config.js";
import { hashPassword } from "../utils/auth.js";
import { generateId } from "../utils/helpers.js";
import { getMysqlPool, initializeMysqlSchema } from "./mysql.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const dbPath = path.join(__dirname, "../../admin-panel.db");

let sqljsDb = null;
let SQL = null;
let driver = config.DATABASE_DRIVER;

async function initializeDatabase() {
  if (driver === "mysql") {
    await initializeMysqlSchema();
    await seedDefaultAdminMysql();
    await seedDefaultSettingsMysql();
    console.log("✅ Database initialized successfully (mysql)");
    return;
  }

  await initializeSqljsDatabase();
  console.log("✅ Database initialized successfully (sqljs)");
}

async function initializeSqljsDatabase() {
  SQL = await initSqlJs();

  if (fs.existsSync(dbPath)) {
    const filebuffer = fs.readFileSync(dbPath);
    sqljsDb = new SQL.Database(filebuffer);
  } else {
    sqljsDb = new SQL.Database();
  }

  sqljsDb.run(`
    CREATE TABLE IF NOT EXISTS users (
      id TEXT PRIMARY KEY,
      name TEXT NOT NULL,
      email TEXT UNIQUE NOT NULL,
      password TEXT NOT NULL,
      role TEXT DEFAULT 'user' CHECK(role IN ('admin', 'manager', 'user')),
      status TEXT DEFAULT 'active' CHECK(status IN ('active', 'inactive')),
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
    );
  `);

  sqljsDb.run(`
    CREATE TABLE IF NOT EXISTS orders (
      id TEXT PRIMARY KEY,
      order_id TEXT UNIQUE NOT NULL,
      customer_id TEXT NOT NULL,
      product TEXT NOT NULL,
      quantity INTEGER DEFAULT 1,
      date DATE NOT NULL,
      status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'paid', 'cancelled')),
      total REAL NOT NULL,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (customer_id) REFERENCES users(id)
    );
  `);

  sqljsDb.run(`
    CREATE TABLE IF NOT EXISTS notifications (
      id TEXT PRIMARY KEY,
      user_id TEXT NOT NULL,
      type TEXT NOT NULL,
      title TEXT NOT NULL,
      message TEXT NOT NULL,
      read INTEGER DEFAULT 0,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (user_id) REFERENCES users(id)
    );
  `);

  sqljsDb.run(`
    CREATE TABLE IF NOT EXISTS analytics (
      id TEXT PRIMARY KEY,
      user_id TEXT,
      action TEXT NOT NULL,
      resource TEXT NOT NULL,
      details TEXT,
      ip_address TEXT,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (user_id) REFERENCES users(id)
    );
  `);

  sqljsDb.run(`
    CREATE TABLE IF NOT EXISTS settings (
      id TEXT PRIMARY KEY,
      company_name TEXT NOT NULL,
      support_email TEXT NOT NULL,
      default_role TEXT DEFAULT 'user' CHECK(default_role IN ('admin', 'manager', 'user')),
      dashboard_view TEXT DEFAULT 'overview',
      email_notifications INTEGER DEFAULT 1,
      weekly_reports INTEGER DEFAULT 0,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
    );
  `);

  sqljsDb.run(`
    CREATE TABLE IF NOT EXISTS audit_logs (
      id TEXT PRIMARY KEY,
      user_id TEXT,
      action TEXT NOT NULL,
      resource_type TEXT NOT NULL,
      resource_id TEXT NOT NULL,
      changes TEXT,
      timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (user_id) REFERENCES users(id)
    );
  `);

  await seedDefaultAdminSqljs();
  await seedDefaultSettingsSqljs();
}

async function seedDefaultAdminSqljs() {
  const stmt = sqljsDb.prepare("SELECT id FROM users WHERE email = ?");
  stmt.bind([config.DEFAULT_ADMIN_EMAIL]);
  const hasAdmin = stmt.step();
  stmt.free();

  if (hasAdmin) return;

  const password = await hashPassword(config.DEFAULT_ADMIN_PASSWORD);

  sqljsDb.run(
    `
      INSERT INTO users (id, name, email, password, role, status)
      VALUES (?, ?, ?, ?, 'admin', 'active')
    `,
    [
      generateId(),
      config.DEFAULT_ADMIN_NAME,
      config.DEFAULT_ADMIN_EMAIL,
      password,
    ],
  );

  saveSqljsDatabase();
  console.log(
    `🔐 Default admin created: ${config.DEFAULT_ADMIN_EMAIL} / ${config.DEFAULT_ADMIN_PASSWORD}`,
  );
}

async function seedDefaultAdminMysql() {
  const mysqlPool = getMysqlPool();
  const [rows] = await mysqlPool.execute(
    "SELECT id FROM users WHERE email = ? LIMIT 1",
    [config.DEFAULT_ADMIN_EMAIL],
  );

  if (rows.length > 0) return;

  const password = await hashPassword(config.DEFAULT_ADMIN_PASSWORD);

  await mysqlPool.execute(
    `
      INSERT INTO users (id, name, email, password, role, status)
      VALUES (?, ?, ?, ?, 'admin', 'active')
    `,
    [
      generateId(),
      config.DEFAULT_ADMIN_NAME,
      config.DEFAULT_ADMIN_EMAIL,
      password,
    ],
  );

  console.log(
    `🔐 Default admin created: ${config.DEFAULT_ADMIN_EMAIL} / ${config.DEFAULT_ADMIN_PASSWORD}`,
  );
}

async function seedDefaultSettingsSqljs() {
  const stmt = sqljsDb.prepare("SELECT id FROM settings WHERE id = ?");
  stmt.bind(["global"]);
  const hasSettings = stmt.step();
  stmt.free();

  if (hasSettings) return;

  sqljsDb.run(
    `
      INSERT INTO settings (
        id, company_name, support_email, default_role, dashboard_view,
        email_notifications, weekly_reports
      )
      VALUES (?, ?, ?, ?, ?, ?, ?)
    `,
    [
      "global",
      "Olympus Admin Inc.",
      "support@example.com",
      "user",
      "overview",
      1,
      0,
    ],
  );

  saveSqljsDatabase();
}

async function seedDefaultSettingsMysql() {
  const mysqlPool = getMysqlPool();
  const [rows] = await mysqlPool.execute(
    "SELECT id FROM settings WHERE id = ? LIMIT 1",
    ["global"],
  );

  if (rows.length > 0) return;

  await mysqlPool.execute(
    `
      INSERT INTO settings (
        id, company_name, support_email, default_role, dashboard_view,
        email_notifications, weekly_reports
      )
      VALUES (?, ?, ?, ?, ?, ?, ?)
    `,
    [
      "global",
      "Olympus Admin Inc.",
      "support@example.com",
      "user",
      "overview",
      1,
      0,
    ],
  );
}

function createSqljsStatement(sql) {
  return {
    run: async (...params) => {
      sqljsDb.run(sql, params);
      saveSqljsDatabase();
      return { changes: sqljsDb.getRowsModified() };
    },
    get: async (...params) => {
      const stmt = sqljsDb.prepare(sql);
      stmt.bind(params);
      if (stmt.step()) {
        const row = stmt.getAsObject();
        stmt.free();
        return row;
      }
      stmt.free();
      return null;
    },
    all: async (...params) => {
      const stmt = sqljsDb.prepare(sql);
      stmt.bind(params);
      const result = [];
      while (stmt.step()) {
        result.push(stmt.getAsObject());
      }
      stmt.free();
      return result;
    },
  };
}

function createMysqlStatement(sql) {
  return {
    run: async (...params) => {
      const mysqlPool = getMysqlPool();
      const [result] = await mysqlPool.execute(sql, params);
      return { changes: result.affectedRows || 0 };
    },
    get: async (...params) => {
      const mysqlPool = getMysqlPool();
      const [rows] = await mysqlPool.execute(sql, params);
      return rows[0] || null;
    },
    all: async (...params) => {
      const mysqlPool = getMysqlPool();
      const [rows] = await mysqlPool.execute(sql, params);
      return rows;
    },
  };
}

const dbWrapper = {
  prepare: (sql) =>
    driver === "mysql" ? createMysqlStatement(sql) : createSqljsStatement(sql),
  exec: async (sql) => {
    if (driver === "mysql") {
      const mysqlPool = getMysqlPool();
      await mysqlPool.query(sql);
      return;
    }

    sqljsDb.run(sql);
    saveSqljsDatabase();
  },
};

function saveSqljsDatabase() {
  const data = sqljsDb.export();
  const buffer = Buffer.from(data);
  fs.writeFileSync(dbPath, buffer);
}

export function getDatabaseDriver() {
  return driver;
}

export { initializeDatabase, SQL };
export default dbWrapper;
