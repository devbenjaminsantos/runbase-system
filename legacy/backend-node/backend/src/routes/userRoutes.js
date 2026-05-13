import express from "express";
import {
  getAllUsers,
  getUserById,
  createUser,
  updateUser,
  deleteUser,
  getUserStats,
} from "../controllers/userController.js";
import { authMiddleware, roleMiddleware } from "../middleware/auth.js";

const router = express.Router();

// Middleware: autenticação obrigatória
router.use(authMiddleware);

// Public routes (qualquer usuário autenticado)
router.get("/", getAllUsers);
router.get("/stats", getUserStats);
router.get("/:id", getUserById);

// Admin only
router.post("/", roleMiddleware("admin", "manager"), createUser);
router.put("/:id", roleMiddleware("admin", "manager"), updateUser);
router.delete("/:id", roleMiddleware("admin"), deleteUser);

export default router;
