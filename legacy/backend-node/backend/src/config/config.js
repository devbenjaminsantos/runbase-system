import dotenv from "dotenv";

dotenv.config();

export const config = {
  ENV: process.env.NODE_ENV || "development",
  PORT: process.env.PORT || 5000,
  DATABASE_URL: process.env.DATABASE_URL || "./admin-panel.db",
  DATABASE_DRIVER: process.env.DATABASE_DRIVER || "sqljs",
  MYSQL_HOST: process.env.MYSQL_HOST || "localhost",
  MYSQL_PORT: Number(process.env.MYSQL_PORT || 3306),
  MYSQL_DATABASE: process.env.MYSQL_DATABASE || "olympus_admin",
  MYSQL_USER: process.env.MYSQL_USER || "root",
  MYSQL_PASSWORD: process.env.MYSQL_PASSWORD || "",
  JWT_SECRET: process.env.JWT_SECRET || "your-secret-key-change-in-production",
  JWT_EXPIRES_IN: process.env.JWT_EXPIRES_IN || "7d",
  CORS_ORIGIN: process.env.CORS_ORIGIN || "*",
  DEFAULT_ADMIN_NAME: process.env.DEFAULT_ADMIN_NAME || "Olympus Admin",
  DEFAULT_ADMIN_EMAIL:
    process.env.DEFAULT_ADMIN_EMAIL || "admin@olympus.local",
  DEFAULT_ADMIN_PASSWORD:
    process.env.DEFAULT_ADMIN_PASSWORD || "Admin123!",
};

export default config;
