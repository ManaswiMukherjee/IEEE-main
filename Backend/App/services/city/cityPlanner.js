const clamp = (value, minimum, maximum) =>
  Math.min(Math.max(value, minimum), maximum);

const planCity = async ({
  requirements,
  geography = {},
  environment = {},
}) => {
  if (!requirements || typeof requirements !== "object") {
    throw new Error("City requirements are required.");
  }

  const population = Number.isInteger(requirements.population)
    ? requirements.population
    : 100000;
  const dimensions = Array.isArray(geography.dimensions)
    ? geography.dimensions
    : [1000, 1000];

  if (
    population < 1 ||
    dimensions.length !== 2 ||
    dimensions.some((value) => typeof value !== "number" || value <= 0)
  ) {
    throw new Error("Population and dimensions must be positive values.");
  }

  const greenPriority = requirements.environment?.green_space_priority;
  const greenRatio = greenPriority === "high" ? 0.35 : 0.25;

  return {
    city: {
      name: geography.cityName || "AI Sustainable City",
      seed: Number.isInteger(geography.seed) ? geography.seed : 20261006,
      dimensions,
      population_target: population,
    },
    terrain: {
      type: geography.terrain || "flat",
      height_scale: Number.isFinite(geography.heightScale)
        ? Math.max(0, geography.heightScale)
        : 10,
    },
    population,
    greenRatio: clamp(greenRatio, 0, 1),
    preferredSources: requirements.energy?.preferred_sources || ["solar", "wind"],
    waterBodies: Array.isArray(environment.water_bodies)
      ? environment.water_bodies
      : [],
  };
};

module.exports = {
  planCity,
};