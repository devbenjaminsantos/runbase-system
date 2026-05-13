import express from "express";
import {
  getSettings,
  resetSettings,
  updateSettings,
} from "../controllers/settingsController.js";
import { authMiddleware, roleMiddleware } from "../middleware/auth.js";

const router = express.Router();

router.use(authMiddleware);

router.get("/", getSettings);
router.put("/", roleMiddleware("admin", "manager"), updateSettings);
router.post("/reset", roleMiddleware("admin", "manager"), resetSettings);

export default router;
