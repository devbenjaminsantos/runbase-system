import express from "express";
import {
  getUserNotifications,
  createNotification,
  markAsRead,
  getUnreadCount,
} from "../controllers/notificationController.js";
import { authMiddleware, roleMiddleware } from "../middleware/auth.js";

const router = express.Router();

router.use(authMiddleware);

router.get("/", getUserNotifications);
router.get("/unread", getUnreadCount);
router.put("/:id/read", markAsRead);
router.post("/", roleMiddleware("admin"), createNotification);

export default router;
