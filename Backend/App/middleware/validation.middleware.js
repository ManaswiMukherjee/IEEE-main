const validateBody = (requiredFields = []) => {
  return (req, res, next) => {
    const missingFields = requiredFields.filter((field) => {
      const value = req.body?.[field];

      return (
        value === undefined ||
        value === null ||
        (typeof value === "string" && value.trim() === "")
      );
    });

    if (missingFields.length > 0) {
      return res.status(400).json({
        success: false,
        message: "Missing required fields",
        errors: missingFields,
      });
    }

    next();
  };
};

module.exports = {
  validateBody,
};