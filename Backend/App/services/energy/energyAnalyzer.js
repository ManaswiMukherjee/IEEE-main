const clamp = (value) => Math.min(Math.max(Number(value) || 0, 0), 1);

const analyzeEnergy = ({
  solar = 0,
  wind = 0,
  battery = 0,
  grid = 1,
  demand = 0,
  batteryReserve = 0.2,
}) => {
  const availableSources = {
    solar: clamp(solar),
    wind: clamp(wind),
    battery: clamp(battery),
    grid: clamp(grid),
  };
  const normalizedDemand = Math.max(Number(demand) || 0, 0);
  const batteryScore = availableSources.battery > batteryReserve
    ? availableSources.battery * (normalizedDemand > 0 ? 1 : 0.5)
    : 0;
  const candidates = {
    solar: availableSources.solar,
    wind: availableSources.wind,
    battery: batteryScore,
    grid: availableSources.grid * 0.5,
  };
  const selectedSource = Object.entries(candidates)
    .sort(([, first], [, second]) => second - first)[0][0];

  return {
    ...availableSources,
    demand: normalizedDemand,
    selected_source: selectedSource,
    renewable_share: Math.min(availableSources.solar + availableSources.wind, 1),
    timestamp: new Date().toISOString(),
  };
};

module.exports = {
  analyzeEnergy,
};