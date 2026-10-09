// ---------------------------------------------------------------------------
// Subsystem 3 — detailed, renderable City MODEL generation
//
// Emits an instance of schemas/city-model.schema.json: a library of reusable,
// richly-detailed building_styles (facades, windows, roofs, setbacks), blocks
// that reference a style, plus explicitly placed water, grasslands (with tree
// detail), windmills and landmark towers. Enough for a renderer to build a
// realistic, windowed skyline.
//
// COORDINATE CONVENTION (keep in sync with the renderer):
//   - city.size = [width, depth] in metres, origin (0,0).
//   - bounds = [x, y, w, d] (lower-left corner + size), inside the city.
//   - point = [x, y]; range = [min, max]; colours are "#rrggbb".
// ---------------------------------------------------------------------------

export function buildCityModelSystemPrompt(schemaJsonString) {
  return `You are Subsystem 3, the detailed procedural city planner. You turn
formal requirements into a RICH, RENDERABLE 3D city model with real architectural
detail — tall windowed towers, varied facades, realistic skylines — NOT plain
boxes and NOT a flat dot-grid.

Output a single JSON object and NOTHING else (no prose, no markdown fences). It
MUST validate against this JSON Schema (draft 2020-12):

${schemaJsonString}

COORDINATE CONVENTION (follow exactly):
- "city.size" = [width, depth] in metres, origin (0,0). Size to the land area
  (width*depth/1e6 ≈ area_km2).
- "bounds" = [x, y, w, d]: lower-left corner + size; everything inside the city,
  blocks not overlapping each other or the water. "point" = [x, y].

DETAIL LIVES IN building_styles (define 6–12 reusable presets, then reference
them from blocks). For each style set:
- "height_range": make downtowns TALL. Use these as guidance:
    * supertall downtown core -> [150, 340]
    * downtown / financial     -> [90, 220]
    * commercial / office      -> [40, 110]
    * residential_high         -> [30, 80]
    * residential_low          -> [8, 20]
    * industrial               -> [10, 28] (wide footprints)
    * civic / mixed            -> [20, 60]
- "floor_height" 3–4.5 m (sets the number of window rows).
- "facade": choose "material" and a "window_pattern" that fits (glass_curtain +
  full_curtain or horizontal_bands for towers; punched_windows/brick + grid for
  residential; industrial + sparse for sheds). Set a realistic "window_ratio"
  (towers 0.6–0.9, residential 0.25–0.45, industrial 0.1–0.25) and give
  "window_color", "frame_color", "spandrel_color" and a "lit_ratio".
- "roof_types", optional "rooftop_features" (hvac/antenna/spire/solar_panels…),
  a "wall_palette", and for towers a "setback" (tiers) and/or "podium".

Then create:
- "blocks": 18–40 blocks arranged like a real city — a cluster of tall
  downtown/commercial blocks in/near the centre, residential around them,
  industrial toward an edge, waterfront blocks along any river/lake. Each block
  has bounds, a "zone", and a "style" that is one of your building_styles ids.
  Leave street gaps between blocks. Use "height_bias" to vary a few blocks.
- "landmarks": 3–8 hero/supertall towers near downtown (give some a "spire" and
  a tall "height" up to ~400) for a recognisable skyline.
- "water_bodies": 1–3 (a winding river is great) that do NOT cross building
  blocks. rivers/canals = shape "strip" (path + width); lakes/marinas = "area".
- "green_areas": several, sized so their total area meets
  environment.minimum_green_ratio; vary tree_density (park ~0.35, forest ~0.85,
  grassland ~0.05) and set tree_height_range / canopy_radius_range / species.
- "roads": a believable network (a few boulevards/avenues + the odd highway)
  roughly separating blocks; mark some tree_lined.
- "windmills": 3–10 on the outskirts, ridgelines or offshore near water.
- Set terrain (type, height_scale, ground_color) and environment
  (minimum_green_ratio, and optionally time_of_day + sky_color + water_color).
- Pick any non-negative integer seed.`
}

// Geometry/semantic checks JSON Schema can't express. Returns error strings.
export function checkCityModel(m) {
  const errors = []
  if (!m || typeof m !== 'object') return ['model is not an object']

  const size = m.city?.size
  if (!Array.isArray(size) || size.length !== 2) return ['city.size missing']
  const [W, D] = size

  const insideBounds = (b, label) => {
    if (!Array.isArray(b) || b.length !== 4) return
    const [x, y, w, d] = b
    if (x < 0 || y < 0 || x + w > W + 1 || y + d > D + 1) {
      errors.push(`${label} bounds [${b}] fall outside the city size [${W}, ${D}]`)
    }
  }
  const insidePoint = (p, label) => {
    if (!Array.isArray(p) || p.length !== 2) return
    const [x, y] = p
    if (x < 0 || y < 0 || x > W || y > D) errors.push(`${label} point [${p}] is outside the city`)
  }
  const checkRange = (r, label) => {
    if (Array.isArray(r) && r.length === 2 && r[0] > r[1]) errors.push(`${label} range ${JSON.stringify(r)} has min > max`)
  }

  // building_styles: unique ids, sane ranges
  const styleIds = new Set()
  for (const s of m.building_styles || []) {
    if (styleIds.has(s?.id)) errors.push(`duplicate building_style id "${s?.id}"`)
    styleIds.add(s?.id)
    checkRange(s?.height_range, `style ${s?.id} height_range`)
    checkRange(s?.footprint_range, `style ${s?.id} footprint_range`)
  }

  const blockIds = new Set()
  for (const b of m.blocks || []) {
    if (blockIds.has(b?.id)) errors.push(`duplicate block id "${b?.id}"`)
    blockIds.add(b?.id)
    insideBounds(b?.bounds, `block ${b?.id}`)
    if (!styleIds.has(b?.style)) errors.push(`block ${b?.id}: style "${b?.style}" is not a defined building_style`)
  }
  for (const g of m.green_areas || []) insideBounds(g?.bounds, `green_area ${g?.id}`)
  for (const w of m.windmills || []) insidePoint(w?.position, `windmill ${w?.id}`)
  for (const l of m.landmarks || []) {
    insidePoint(l?.position, `landmark ${l?.id}`)
    if (l?.style && !styleIds.has(l.style)) errors.push(`landmark ${l?.id}: style "${l.style}" is not a defined building_style`)
  }

  return errors
}
