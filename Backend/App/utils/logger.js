const logger = {
  info(message, data = "") {
    console.log(`[INFO] ${message}`, data);
  },

  success(message, data = "") {
    console.log(`[SUCCESS] ${message}`, data);
  },

  warn(message, data = "") {
    console.warn(`[WARNING] ${message}`, data);
  },

  error(message, error = "") {
    console.error(`[ERROR] ${message}`, error);
  },
};

module.exports = logger;