const {
  getCurrentWeather,
} = require("../services/weather/weather.service");

const {
  successResponse,
} = require("../utils/response");

const getWeather = async (req, res, next) => {
  try {
    const weather = await getCurrentWeather();

    return successResponse(
      res,
      weather,
      "Weather retrieved successfully."
    );
  } catch (error) {
    next(error);
  }
};

module.exports = {
  getWeather,
};