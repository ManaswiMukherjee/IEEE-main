// ---------------------------------------------------------------------------
// UserVisionInterpreter — Subsystem 2
//
// Job:   HUMAN VISION  ->  UNDERSTANDING  ->  FORMAL REQUIREMENTS
// NOT:   designing the city (that is Subsystem 3).
//
// `interpretVision` is async: it asks the real Claude API (via the /api/interpret
// server endpoint, which keeps the key server-side) and automatically FALLS BACK
// to the offline rule-based interpreter if the API has no key, errors, or is
// unreachable — so a live demo never hard-fails.
// ---------------------------------------------------------------------------

import { ENUMS, REQUIRED_PARAMS } from './schemaContract.js'

export { ENUMS, REQUIRED_PARAMS }

// Which mandatory params are still missing from the supplied set.
export function missingParams(params) {
  return REQUIRED_PARAMS.filter((k) => {
    const v = params[k]
    if (v === undefined || v === null || v === '') return true
    if ((k === 'land_area_km2' || k === 'population') && !(Number(v) > 0)) return true
    return false
  })
}

// Main entry point. Returns { schema, notes, source }.
//   source: 'ai'      -> interpreted by Claude
//           'offline'  -> API unavailable, used the rule-based fallback
export async function interpretVision(visionText, params) {
  try {
    const res = await fetch('/api/interpret', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ vision: visionText, params }),
    })

    if (res.ok) {
      const { schema, model } = await res.json()
      return {
        schema,
        notes: [`Interpreted by ${model}.`],
        source: 'ai',
      }
    }

    // Known "expected" failures fall through to offline mode quietly.
    const { error } = await res.json().catch(() => ({ error: 'api_error' }))
    const local = interpretVisionLocal(visionText, params)
    local.notes.unshift(
      error === 'no_api_key'
        ? 'No API key set — using the offline interpreter.'
        : 'API unavailable — using the offline interpreter.'
    )
    local.source = 'offline'
    return local
  } catch {
    const local = interpretVisionLocal(visionText, params)
    local.notes.unshift('API unreachable — using the offline interpreter.')
    local.source = 'offline'
    return local
  }
}

// Subsystem 3: ask the API to generate a renderable City MODEL instance that
// validates against schemas/city-model.schema.json. Returns { cityModel,
// modelName } on success, or throws with a readable message.
export async function generateCityModel(requirements, params) {
  const res = await fetch('/api/citymodel', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ requirements, params }),
  })
  if (res.ok) return res.json()

  const body = await res.json().catch(() => ({}))
  if (body.error === 'no_api_key') throw new Error('No API key set — add one to .env to generate a model.')
  if (body.error === 'invalid_model') {
    throw new Error('The model could not produce a valid city model: ' + (body.details || []).join('; '))
  }
  throw new Error(body.message || 'City model generation failed.')
}

// ---------------------------------------------------------------------------
// Offline rule-based interpreter (fallback). Produces the same frozen schema
// shape as the API, using keyword matching + density math.
// ---------------------------------------------------------------------------
function deriveDensity(population, areaKm2) {
  const perKm2 = population / areaKm2
  if (perKm2 < 1500) return 'low'
  if (perKm2 < 5000) return 'medium'
  if (perKm2 < 10000) return 'medium_high'
  return 'high'
}

function has(text, words) {
  return words.some((w) => text.includes(w))
}

export function interpretVisionLocal(visionText, params) {
  const text = (visionText || '').toLowerCase()
  const population = Number(params.population)
  const areaKm2 = Number(params.land_area_km2)
  const notes = []

  const walkability = has(text, ['walkable', 'walkability', 'pedestrian', 'walk', 'car-free', 'car free'])
    ? 'high'
    : 'medium'
  const publicTransport = has(text, [
    'public transport', 'public transit', 'transit', 'metro', 'subway',
    'tram', 'bus', 'train', 'rail',
  ])
    ? 'high'
    : 'medium'
  if (walkability === 'high') notes.push('Detected a walkability focus.')
  if (publicTransport === 'high') notes.push('Detected strong public-transport intent.')

  const greenSpace = has(text, ['green', 'park', 'nature', 'tree', 'forest', 'garden', 'natural'])
    ? 'high'
    : 'medium'
  if (greenSpace === 'high') notes.push('Detected a green-space priority.')

  const sourceMap = {
    solar: ['solar', 'photovoltaic', 'pv'],
    wind: ['wind', 'turbine'],
    hydro: ['hydro', 'hydroelectric', 'water power', 'tidal'],
    geothermal: ['geothermal'],
    biomass: ['biomass', 'bioenergy'],
    nuclear: ['nuclear'],
  }
  const preferredSources = Object.entries(sourceMap)
    .filter(([, words]) => has(text, words))
    .map(([source]) => source)

  const renewableMentioned = has(text, ['renewable', 'sustainable', 'clean energy', 'green energy', 'carbon'])
  const renewablePriority =
    preferredSources.some((s) => s !== 'nuclear') || renewableMentioned ? 'high' : 'medium'
  if (preferredSources.length) notes.push(`Preferred energy sources: ${preferredSources.join(', ')}.`)

  const urbanDensity = deriveDensity(population, areaKm2)
  notes.push(
    `Density ${urbanDensity} (${Math.round(population / areaKm2).toLocaleString()} people/km²).`
  )

  const schema = {
    location: params.location,
    population,
    land_area_km2: areaKm2,
    urban_density: urbanDensity,
    transport: {
      public_transport_priority: publicTransport,
      walkability,
    },
    environment: {
      green_space_priority: greenSpace,
    },
    energy: {
      renewable_priority: renewablePriority,
      preferred_sources: preferredSources,
    },
  }

  return { schema, notes, source: 'offline' }
}
