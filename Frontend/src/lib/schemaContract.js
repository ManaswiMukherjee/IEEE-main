// ---------------------------------------------------------------------------
// FROZEN SCHEMA CONTRACT
//
// Single source of truth shared by:
//   - the server-side LLM prompt (vite.config.js)  -> tells Claude the shape
//   - server-side validation (vite.config.js)      -> rejects bad LLM output
//   - the offline rule-based interpreter            -> same output shape
//   - Subsystem 3 (the city planner)               -> consumes this schema
//
// Change the schema HERE and nowhere else.
// ---------------------------------------------------------------------------

export const ENUMS = {
  priority: ['low', 'medium', 'high'],
  density: ['low', 'medium', 'medium_high', 'high'],
  energy_sources: ['solar', 'wind', 'hydro', 'geothermal', 'biomass', 'nuclear'],
}

export const REQUIRED_PARAMS = ['location', 'land_area_km2', 'population']

// Human-readable description of the contract, injected into the LLM prompt.
export function buildSystemPrompt() {
  return `You are the City Vision Interpreter (Subsystem 2) of a sustainable-city
planning system. Your ONLY job is to convert a human's natural-language vision,
combined with mandatory parameters, into a STRICT machine-readable requirements
schema. You do NOT design the city — a later subsystem does that.

Respond with ONLY a single JSON object, no prose, no markdown fences.

The JSON object MUST have exactly this shape and use ONLY these enum values:

{
  "location": <string, copy the given location>,
  "population": <integer, copy the given population>,
  "land_area_km2": <number, copy the given land area>,
  "urban_density": one of ${JSON.stringify(ENUMS.density)},
  "transport": {
    "public_transport_priority": one of ${JSON.stringify(ENUMS.priority)},
    "walkability": one of ${JSON.stringify(ENUMS.priority)}
  },
  "environment": {
    "green_space_priority": one of ${JSON.stringify(ENUMS.priority)}
  },
  "energy": {
    "renewable_priority": one of ${JSON.stringify(ENUMS.priority)},
    "preferred_sources": array of zero or more of ${JSON.stringify(ENUMS.energy_sources)}
  }
}

Guidance:
- Derive urban_density primarily from population / land_area_km2 (people per km²):
  <1500 -> "low", 1500-5000 -> "medium", 5000-10000 -> "medium_high", >10000 -> "high".
  Nudge up or down if the vision strongly implies dense or spread-out living.
- Infer the priorities and energy sources from the vision text. If the vision
  doesn't mention something, use "medium" (and an empty array for sources).
- Only include energy sources the user actually implies.`
}

// Validate an object against the frozen contract. Returns an array of error
// strings (empty array = valid).
export function validateSchema(s) {
  const errors = []
  if (!s || typeof s !== 'object') return ['schema is not an object']

  if (typeof s.location !== 'string' || !s.location) errors.push('location missing')
  if (!(Number(s.population) > 0)) errors.push('population invalid')
  if (!(Number(s.land_area_km2) > 0)) errors.push('land_area_km2 invalid')

  if (!ENUMS.density.includes(s.urban_density)) errors.push('urban_density invalid')

  const t = s.transport || {}
  if (!ENUMS.priority.includes(t.public_transport_priority))
    errors.push('transport.public_transport_priority invalid')
  if (!ENUMS.priority.includes(t.walkability)) errors.push('transport.walkability invalid')

  const e = s.environment || {}
  if (!ENUMS.priority.includes(e.green_space_priority))
    errors.push('environment.green_space_priority invalid')

  const en = s.energy || {}
  if (!ENUMS.priority.includes(en.renewable_priority))
    errors.push('energy.renewable_priority invalid')
  if (!Array.isArray(en.preferred_sources)) {
    errors.push('energy.preferred_sources must be an array')
  } else if (en.preferred_sources.some((x) => !ENUMS.energy_sources.includes(x))) {
    errors.push('energy.preferred_sources has an unknown source')
  }

  return errors
}
