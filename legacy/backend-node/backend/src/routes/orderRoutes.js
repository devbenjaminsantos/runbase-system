import express from "express";
import {
  getAllOrders,
  getOrderById,
  createOrder,
  updateOrder,
  deleteOrder,
  getOrderStats,
} from "../controllers/orderController.js";
import { authMiddleware, roleMiddleware } from "../middleware/auth.js";

const router = express.Router();

// Middleware: autenticação obrigatória
router.use(authMiddleware);

// Public routes (qualquer usuário autenticado)
router.get("/", getAllOrders);
router.get("/stats", getOrderStats);
router.get("/:id", getOrderById);

// Admin/Manager
router.post("/", roleMiddleware("admin", "manager"), createOrder);
router.put("/:id", roleMiddleware("admin", "manager"), updateOrder);
router.delete("/:id", roleMiddleware("admin"), deleteOrder);

export default router;
