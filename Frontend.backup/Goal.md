# Frontend — Goal

## Immediate Goal (Phase 1) ✅ Done

Render `TestCity.json` as an interactive 3D city in the browser.

- Load `/Plans/TestCity.json` from the public directory
- Display the city plan as a 3D scene using React Three Fiber + Three.js
- All city layers visible: terrain, sectors, roads, buildings, vegetation, water
- Camera orbits, pans, and zooms around the full city
- No TypeScript errors, no build errors, no JSON parse errors
- Performant (InstancedMesh for all repeated geometry)

## Medium-Term Goals (Phase 2)

- Procedural terrain height map from `terrain.height_scale`
- Energy infrastructure visualization (solar panels, wind turbines)
- Building facade variation (colours, materials by sector type)
- City information overlay (sector labels, road names)
- Sustainability metrics panel (energy balance, green ratio)

## Long-Term Goals (Phase 3)

- Connect to backend AI city planner (city plan generated from user vision)
- Real-time updates when city plan changes
- Day/night cycle visualization
- Before/after comparison mode
- Animated traffic, energy flows
- Mobile-responsive layout

## Architecture

```
User Vision (text/image)
        ↓
  AI Planner (Backend)
        ↓
  CityPlan JSON  ←── TestCity.json (for testing)
        ↓
  App.tsx (fetch + state)
        ↓
  CityScene (R3F Canvas + camera + lights)
        ↓
  CityRenderer
  ├── Terrain     (base + sector overlays)
  ├── WaterBodies (lakes, rivers)
  ├── Roads       (instanced per road type)
  ├── Buildings   (single instanced mesh)
  └── Vegetation  (trunk + foliage instanced)
```
