const env = require("../../config/env");

const getESP32Status = async () => {
  if (!env.esp32Url) {
    return {
      connected: false,
      message: "ESP32 URL is not configured.",
    };
  }

  // ESP32 communication will be implemented here.

  return {
    connected: false,
    message: "ESP32 integration not implemented yet.",
  };
};

const sendEnergyCommand = async (command) => {
  if (!env.esp32Url) {
    return {
      success: false,
      message: "ESP32 URL is not configured.",
    };
  }

  console.log("ESP32 command:", command);

  return {
    success: true,
    message: "Command prepared for ESP32.",
  };
};

module.exports = {
  getESP32Status,
  sendEnergyCommand,
};