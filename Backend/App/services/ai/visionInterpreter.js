const {
  buildVisionInterpreterPrompt,
} = require("./promptBuilder");

const VALID_PRIORITIES = new Set(["low", "medium", "high"]);
const VALID_DENSITIES = new Set(["low", "medium", "medium_high", "high"]);

const validateRequirements = (requirements) => {
  const errors = [];
  if (!requirements || !Number.isInteger(requirements.population) || requirements.population < 1) errors.push("population must be a positive integer");
  if (!VALID_DENSITIES.has(requirements?.urban_density)) errors.push("urban_density is invalid");
  if (!VALID_PRIORITIES.has(requirements?.transport?.public_transport_priority)) errors.push("public transport priority is invalid");
  if (!VALID_PRIORITIES.has(requirements?.transport?.walkability)) errors.push("walkability priority is invalid");
  if (!VALID_PRIORITIES.has(requirements?.environment?.green_space_priority)) errors.push("green-space priority is invalid");
  if (!VALID_PRIORITIES.has(requirements?.energy?.renewable_priority)) errors.push("renewable priority is invalid");
  if (!Array.isArray(requirements?.energy?.preferred_sources) || requirements.energy.preferred_sources.length === 0) errors.push("preferred energy sources are required");
  return { valid: errors.length === 0, errors };
};

const interpretVision = async (vision) => {
  if (!vision || typeof vision !== "string") {
    throw new Error("City vision must be a valid string.");
  }

  const prompt = buildVisionInterpreterPrompt(vision);

  const populationMatch = vision.match(/(?:population|people|residents)\D*(\d[\d,]*)/i);
  const population = populationMatch
    ? Number(populationMatch[1].replace(/,/g, ""))
    : 100000;
  const lowerVision = vision.toLowerCase();
  const priority = (term) => (lowerVision.includes(term) ? "high" : "medium");
  const preferredSources = ["solar", "wind"].filter((source) => lowerVision.includes(source));
  const requirements = {
    population: Number.isSafeInteger(population) && population > 0 ? population : 100000,
    urban_density: lowerVision.includes("dense") || lowerVision.includes("compact") ? "high" : "medium_high",
    transport: {
      public_transport_priority: priority("public transport"),
      walkability: priority("walkability"),
    },
    environment: {
      green_space_priority: priority("green"),
    },
    energy: {
      renewable_priority: priority("renewable"),
      preferred_sources: preferredSources.length > 0 ? preferredSources : ["solar", "wind"],
    },

    infrastructure: {
      smart_city_priority: "high",
    },
  };

  const validation = validateRequirements(requirements);
  if (!validation.valid) {
    const error = new Error("AI requirements failed validation.");
    error.statusCode = 422;
    error.details = validation.errors;
    throw error;
  }

  return {
    requirements,
    source: "mock-interpreter",
    prompt,
  };
};

module.exports = {
  interpretVision,
  validateRequirements,
};