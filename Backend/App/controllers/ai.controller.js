const { interpretVision } = require("../services/ai/visionInterpreter");

const { successResponse, errorResponse } = require("../utils/response");

const interpret = async (req, res, next) => {
  try {
    const { vision } = req.body;

    if (!vision) {
      return errorResponse(
        res,
        "City vision is required.",
        400
      );
    }

    const result = await interpretVision(vision);

    return successResponse(
      res,
      result,
      "City vision interpreted successfully."
    );
  } catch (error) {
    next(error);
  }
};

module.exports = {
  interpret,
};