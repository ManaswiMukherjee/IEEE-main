const express = require("express");

const {
  interpret,
} = require("../controllers/ai.controller");

const router = express.Router();

router.post("/interpret", interpret);

module.exports = router;