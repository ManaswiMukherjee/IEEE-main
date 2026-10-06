const crypto = require("node:crypto");

const { planCity } = require("../services/city/cityPlanner");

const {
  generateCity,
} = require("../services/city/cityGenerator");

const {
  validateCityPlan,
} = require("../services/city/cityValidator");

const {
  successResponse,
  errorResponse,
} = require("../utils/response");

const cityPlans = new Map();

const generate = async (req, res, next) => {
  try {
    const {
      requirements,
      geography,
      environment,
    } = req.body;

    if (!requirements) {
      return errorResponse(
        res,
        "City requirements are required.",
        400
      );
    }

    const basePlan = await planCity({
      requirements,
      geography,
      environment,
    });

    const cityPlan = await generateCity(basePlan);

    const validation = validateCityPlan(cityPlan);

    if (!validation.valid) {
      return errorResponse(
        res,
        "Generated city plan is invalid.",
        422,
        validation.errors
      );
    }

    const id = crypto.randomUUID();
    cityPlans.set(id, cityPlan);

    return successResponse(
      res,
      { id, cityPlan },
      "City generated successfully."
    );
  } catch (error) {
    next(error);
  }
};

const list = (req, res) => {
  const plans = Array.from(cityPlans, ([id, cityPlan]) => ({ id, cityPlan }));
  return successResponse(res, { plans }, "City plans retrieved successfully.");
};

const getById = (req, res) => {
  const cityPlan = cityPlans.get(req.params.id);
  if (!cityPlan) return errorResponse(res, "City plan not found.", 404, [req.params.id]);
  return successResponse(res, { id: req.params.id, cityPlan }, "City plan retrieved successfully.");
};

const remove = (req, res) => {
  if (!cityPlans.delete(req.params.id)) return errorResponse(res, "City plan not found.", 404, [req.params.id]);
  return successResponse(res, null, "City plan deleted successfully.");
};

module.exports = {
  generate,
  list,
  getById,
  remove,
};