const express = require("express");

const {
  getStatus,
  control,
} = require("../controllers/energy.controller");

const router = express.Router();

router.get("/status", getStatus);

router.post("/control", control);

module.exports = router;