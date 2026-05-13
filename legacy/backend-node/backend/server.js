import express from "express";
import cors from "cors";
import http from "http";
import { Server } from "socket.io";
import { config } from "./src/config/config.js";
import { errorHandler } from "./src/middleware/auth.js";
import { getDatabaseDriver, initializeDatabase } from "./src/db/database.js";
import authRoutes from "./src/routes/authRoutes.js";
import userRoutes from "./src/routes/userRoutes.js";
import orderRoutes from "./src/routes/orderRoutes.js";
import notificationRoutes from "./src/routes/notificationRoutes.js";
import analyticsRoutes from "./src/routes/analyticsRoutes.js";
import settingsRoutes from "./src/routes/settingsRoutes.js";

const app = express();
const server = http.createServer(app);
const io = new Server(server, {
  cors: {
    origin: config.CORS_ORIGIN,
    methods: ["GET", "POST"],
  },
});

// Middleware
app.use(cors({ origin: config.CORS_ORIGIN }));
app.use(express.json());

// Inicializar aplicação
async function start() {
  await initializeDatabase();

  // Health check
  app.get("/api/health", (req, res) => {
    res.json({
      status: "OK",
      timestamp: new Date().toISOString(),
      driver: getDatabaseDriver(),
    });
  });

  // Routes
  app.use("/api/auth", authRoutes);
  app.use("/api/users", userRoutes);
  app.use("/api/orders", orderRoutes);
  app.use("/api/notifications", notificationRoutes);
  app.use("/api/analytics", analyticsRoutes);
  app.use("/api/settings", settingsRoutes);

  // Socket.io para notificações em tempo real
  io.on("connection", (socket) => {
    console.log("Client connected:", socket.id);

    socket.on("disconnect", () => {
      console.log("Client disconnected:", socket.id);
    });

    socket.on("notification", (data) => {
      io.emit("notification", data);
    });
  });

  // Error handler
  app.use(errorHandler);

  // 404 handler
  app.use((req, res) => {
    res.status(404).json({ error: "Route not found" });
  });

  // Server
  server.listen(config.PORT, () => {
    console.log(`✅ Server running on http://localhost:${config.PORT}`);
    console.log(`📊 Environment: ${config.ENV}`);
    console.log(`🗄 Database driver: ${getDatabaseDriver()}`);
  });
}

start().catch((err) => {
  console.error("❌ Failed to start server:", err);
  process.exit(1);
});

export { io };
