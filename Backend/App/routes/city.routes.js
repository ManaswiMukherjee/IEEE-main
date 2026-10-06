const express = require("express");

const {
  generate,
  getById,
  list,
  remove,
} = require("../controllers/city.controller");

const router = express.Router();

router.post("/generate", generate);
router.get("/", list);
router.get("/:id", getById);
router.delete("/:id", remove);

module.exports = router;