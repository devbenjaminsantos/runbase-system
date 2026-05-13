import express from "express";
import {
  trackAction,
  getAnalytics,
  getAnalyticsSummary,
  getAuditLog,
} from "../controllers/analyticsController.js";
import { authMiddleware, roleMiddleware } from "../middleware/auth.js";

const router = express.Router();

// Track ações públicas
router.post("/track", trackAction);

// Require auth para acessar analytics
router.use(authMiddleware);
router.use(roleMiddleware("admin", "manager"));

router.get("/", getAnalytics);
router.get("/summary", getAnalyticsSummary);
router.get("/audit-log", getAuditLog);

export default router;
