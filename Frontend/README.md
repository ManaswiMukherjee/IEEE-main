# IEEE CityBuilder

A chatbot pipeline that turns a natural-language city vision into a validated,
3D-**renderable** City Model JSON file.

```
chatbot (vision + Location / Land Area / Population)
   → Subsystem 2: formal requirements schema      (/api/interpret)
   → Subsystem 3: renderable City MODEL instance   (/api/citymodel)
        validated against schemas/city-model.schema.json (ajv + geometry checks)
   → download city-model.json  → feed to the 3D renderer
```

## Run

```bash
npm install
cp .env.example .env     # then paste your ANTHROPIC_API_KEY into .env
npm run dev
```

Open http://localhost:5173.

## Why the model is "renderable" (not a dot-grid)

The City Model uses a **hybrid** representation designed for a real 3D renderer:

- **Dense things are parametric.** Each `block` carries `building` params
  (`height_range`, `footprint_range`, `coverage`, `roof_types`, `color_palette`);
  each `green_area` carries `tree_density`. The renderer procedurally instantiates
  varied buildings and scatters trees from these + the city `seed`. This is what
  creates a skyline (tall slender downtown towers, low pitched-roof suburbs)
  instead of uniform boxes.
- **Sparse hero things are explicit.** `water_bodies`, `windmills` and
  `landmarks` are placed with real coordinates.

So the schema describes **what and how much**; the renderer decides **exactly
where** each building/tree goes. The LLM never has to enumerate 50,000 buildings.

## What the renderer must build from each field

| Field | Renderer job |
|-------|--------------|
| `blocks[].building` | fill the block with procedurally varied extruded buildings |
| `green_areas` | green surface + trees scattered by `tree_density` / `tree_species` |
| `water_bodies` | `strip` = river/canal (extrude `path` by `width`); `area` = lake/marina (fill `polygon`) |
| `windmills` | tower + nacelle + rotor at `position`, sized by `hub_height`/`rotor_radius` |
| `landmarks` | explicit hero buildings |
| `roads` | ribbons extruded along `path` by `width` |

## Coordinate convention (schema ⇄ renderer — keep in sync)

- `city.size = [width, depth]` in metres, origin (0,0); `width*depth/1e6 ≈ km²`.
- `bounds = [x, y, w, d]` (lower-left corner + size); `point = [x, y]`;
  `range = [min, max]`; colours are `"#rrggbb"`.

## Environment variables (`.env`)

| Var | Purpose | Default |
|-----|---------|---------|
| `ANTHROPIC_API_KEY` | your key (required for the AI path) | — |
| `ANTHROPIC_MODEL` | model for requirements interpretation | `claude-haiku-4-5` |
| `ANTHROPIC_PLAN_MODEL` | model for the detailed city-model generation | `claude-sonnet-5-5` |

> Subsystem 3 defaults to **Sonnet 5.5** because the detailed model is large and
> needs a high output limit + strong structured-JSON fidelity (Haiku's lower
> output cap truncates it). Cost is roughly **$0.05–0.10 per model** — set
> `ANTHROPIC_PLAN_MODEL=claude-haiku-4-5` to go cheaper (simpler models only).
> The generation streams, so large outputs don't time out.

## Schema contracts

- **Requirements** (frozen): [src/lib/schemaContract.js](src/lib/schemaContract.js)
- **City Model** (renderable): [schemas/city-model.schema.json](schemas/city-model.schema.json) — validated with ajv (draft 2020-12), plus geometry checks in [src/lib/cityModelPrompt.js](src/lib/cityModelPrompt.js).

## 3D viewer

After a model validates, click **View 3D ▸** in the City Model tab. The viewer
([src/lib/cityRenderer.js](src/lib/cityRenderer.js), shown by
[src/components/Viewer.jsx](src/components/Viewer.jsx)) renders the model with:

- PBR sun + soft shadows, hemisphere fill, ACES tone mapping, fog
- a Sky shader + image-based lighting, so glass towers and water reflect the sky
- procedural **windowed** buildings (canvas facade textures), setback tiers,
  podiums and rooftop features — merged per style for performance
- instanced trees, ribbon rivers/roads, animated wind turbines
- `environment.time_of_day` presets: `day` / `dawn` / `dusk` / `night`
  (night dims the sun and lights the windows via the emissive map)

Drag to orbit, scroll to zoom, Esc or **Close** to exit. It reads the same
`city-model.json` you can download — the renderer and schema share the
coordinate convention above.

## Structure

- `src/App.jsx` — layout, chat flow, two-stage pipeline
- `src/components/` — ChatBox, ParametersPanel, Outputs (Requirements / City Model tabs + Download)
- `src/lib/interpreter.js` — client calls to both endpoints + offline requirements fallback
- `vite.config.js` — server-side `/api/interpret` and `/api/citymodel` endpoints
