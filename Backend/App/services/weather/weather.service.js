const env = require("../../config/env");

const getCurrentWeather = async () => {
  if (env.weatherApiUrl) {
    const response = await fetch(env.weatherApiUrl, {
      headers: env.weatherApiKey
        ? { Authorization: `Bearer ${env.weatherApiKey}` }
        : undefined,
    });
    if (!response.ok) throw new Error(`Weather API returned ${response.status}.`);
    const data = await response.json();
    return normalizeWeather(data, "external-weather");
  }

  return {
    temperature: 28,
    humidity: 62,
    cloud_cover: 20,
    solar_irradiance: 780,
    wind_speed: 12,
    rainfall: 0,
    source: "mock-weather",
    timestamp: new Date().toISOString(),
  };
};

const normalizeWeather = (data, source = "weather-api") => ({
  temperature: Number(data.temperature ?? data.temp ?? 0),
  humidity: Number(data.humidity ?? 0),
  cloud_cover: Number(data.cloud_cover ?? data.cloudCover ?? 0),
  solar_irradiance: Number(data.solar_irradiance ?? data.solarIrradiance ?? 0),
  wind_speed: Number(data.wind_speed ?? data.windSpeed ?? 0),
  rainfall: Number(data.rainfall ?? 0),
  source,
  timestamp: data.timestamp || new Date().toISOString(),
});

module.exports = {
  getCurrentWeather,
  normalizeWeather,
};