import express from "express";
import {
  register,
  login,
  logout,
  getCurrentUser,
  refreshToken,
} from "../controllers/authController.js";
import { authMiddleware } from "../middleware/auth.js";

const router = express.Router();

router.post("/register", register);
router.post("/login", login);
router.post("/logout", logout);
router.get("/me", authMiddleware, getCurrentUser);
router.post("/refresh", refreshToken);

export default router;
