const authMiddleware = (req, res, next) => {
  // Authentication will be implemented later.
  // For now, allow the request to continue.

  next();
};

module.exports = authMiddleware;