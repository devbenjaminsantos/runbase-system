import fs from "fs/promises";
import path from "path";
import mysql from "mysql2/promise";
import { fileURLToPath } from "url";
import { config } from "../config/config.js";

let pool;
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const mysqlSchemaPath = path.join(__dirname, "schema.mysql.sql");

export function getMysqlConfig() {
  return {
    host: config.MYSQL_HOST,
    port: config.MYSQL_PORT,
    database: config.MYSQL_DATABASE,
    user: config.MYSQL_USER,
    password: config.MYSQL_PASSWORD,
    waitForConnections: true,
    connectionLimit: 10,
    queueLimit: 0,
    multipleStatements: true,
  };
}

function getMysqlAdminConfig() {
  return {
    host: config.MYSQL_HOST,
    port: config.MYSQL_PORT,
    user: config.MYSQL_USER,
    password: config.MYSQL_PASSWORD,
    multipleStatements: true,
  };
}

export function getMysqlPool() {
  if (!pool) {
    pool = mysql.createPool(getMysqlConfig());
  }

  return pool;
}

export async function testMysqlConnection() {
  const mysqlPool = getMysqlPool();
  const connection = await mysqlPool.getConnection();

  try {
    await connection.ping();
    return true;
  } finally {
    connection.release();
  }
}

export async function ensureMysqlDatabase() {
  const connection = await mysql.createConnection(getMysqlAdminConfig());

  try {
    await connection.query(
      `CREATE DATABASE IF NOT EXISTS \`${config.MYSQL_DATABASE}\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci`,
    );
  } finally {
    await connection.end();
  }
}

export async function initializeMysqlSchema() {
  await ensureMysqlDatabase();

  const schemaSql = await fs.readFile(mysqlSchemaPath, "utf8");
  const mysqlPool = getMysqlPool();
  await mysqlPool.query(schemaSql);
}

export async function closeMysqlPool() {
  if (!pool) return;
  await pool.end();
  pool = null;
}
