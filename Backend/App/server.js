const express = require("express");
const cors = require("cors");

const env = require("./config/env");
const connectDatabase = require("./config/database");

const errorMiddleware = require("./middleware/error.middleware");

const aiRoutes = require("./routes/ai.routes");
const cityRoutes = require("./routes/city.routes");
const energyRoutes = require("./routes/energy.routes");
const weatherRoutes = require("./routes/weather.routes");

const app = express();

// --------------------------------------------------
// GLOBAL MIDDLEWARE
// --------------------------------------------------

app.use(
  cors({
    origin: "http://localhost:5173",
    credentials: true,
  })
);

app.use(express.json());

app.use(express.urlencoded({ extended: true }));

// --------------------------------------------------
// HEALTH CHECK
// --------------------------------------------------

app.get("/api/health", (req, res) => {
  res.json({
    success: true,
    message: "Sustainable City Backend is running.",
    environment: env.nodeEnv,
    timestamp: new Date().toISOString(),
  });
});

// --------------------------------------------------
// API ROUTES
// --------------------------------------------------

app.use("/api/ai", aiRoutes);

app.use("/api/city", cityRoutes);

app.use("/api/energy", energyRoutes);

app.use("/api/weather", weatherRoutes);

// --------------------------------------------------
// 404
// --------------------------------------------------

app.use((req, res) => {
  res.status(404).json({
    success: false,
    message: `Route not found: ${req.method} ${req.originalUrl}`,
  });
});

// --------------------------------------------------
// ERROR HANDLER
// --------------------------------------------------

app.use(errorMiddleware);

// --------------------------------------------------
// START SERVER
// --------------------------------------------------

const startServer = async () => {
  try {
    await connectDatabase();

    app.listen(env.port, () => {
      console.log("");
      console.log("======================================");
      console.log("🌱 Sustainable City Backend");
      console.log("======================================");
      console.log(`🚀 Server: http://localhost:${env.port}`);
      console.log(
        `❤️ Health: http://localhost:${env.port}/api/health`
      );
      console.log("======================================");
      console.log("");
    });
  } catch (error) {
    console.error(
      "Failed to start server:",
      error.message
    );

    process.exit(1);
  }
};

if (require.main === module) {
  startServer();
}

module.exports = app;