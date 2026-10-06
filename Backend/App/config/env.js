const dotenv = require("dotenv");

dotenv.config();

const env = {
  port: process.env.PORT || 5000,

  nodeEnv: process.env.NODE_ENV || "development",

  databaseUrl: process.env.DATABASE_URL || "",

  aiApiKey: process.env.AI_API_KEY || "",

  weatherApiKey: process.env.WEATHER_API_KEY || "",

  weatherApiUrl: process.env.WEATHER_API_URL || "",

  esp32Url: process.env.ESP32_URL || "",
};

module.exports = env;