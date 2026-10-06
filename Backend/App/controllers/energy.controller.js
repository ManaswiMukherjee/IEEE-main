const {
  analyzeEnergy,
} = require("../services/energy/energyAnalyzer");

const {
  getESP32Status,
  sendEnergyCommand,
} = require("../services/energy/esp32.service");

const {
  successResponse,
  errorResponse,
} = require("../utils/response");

let latestEnergy = {
  solar: 0.85,
  wind: 0.42,
  battery: 0.6,
  grid: 0.15,
  demand: 100,
};

const getStatus = async (req, res, next) => {
  try {
    const energy = analyzeEnergy(latestEnergy);

    const esp32 = await getESP32Status();

    return successResponse(res, {
      energy,
      esp32,
    });
  } catch (error) {
    next(error);
  }
};

const control = async (req, res, next) => {
  try {
    const { source, value } = req.body;

    if (!['solar', 'wind', 'battery', 'grid'].includes(source) || typeof value !== 'number' || value < 0 || value > 1) {
      return errorResponse(res, "source must be solar, wind, battery, or grid and value must be between 0 and 1.", 400);
    }

    latestEnergy = { ...latestEnergy, [source]: value };

    const result = await sendEnergyCommand({
      source,
      value,
    });

    return successResponse(
      res,
      result,
      "Energy command processed."
    );
  } catch (error) {
    next(error);
  }
};

module.exports = {
  getStatus,
  control,
};