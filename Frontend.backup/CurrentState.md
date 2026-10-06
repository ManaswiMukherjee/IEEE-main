# Frontend — Current State

Last updated: 2026-10-06

## Status: ✅ Building and running

The 3D city renderer is operational. Running `npm run dev` serves the application at `http://localhost:5173`.

---

## What is implemented

### Data loading
- `App.tsx` fetches `/Plans/TestCity.json` from the Vite public directory
- Valid JSON loading with proper error states shown in the UI
- City plan is validated against `CityPlan` TypeScript type

### 3D Renderer stack
- **React Three Fiber** (R3F) Canvas with `OrbitControls`
- Camera auto-framed to the city centre `(cityW/2, 0, cityD/2)`
- Ambient + two directional lights (sun + fill)

### Components

| File | Purpose |
|------|---------|
| `App.tsx` | Fetches city plan, renders loading/error states |
| `City/CityScene.tsx` | R3F Canvas, camera, lights, OrbitControls |
| `City/CityRenderer.tsx` | Assembles all sub-renderers |
| `City/Terrain.tsx` | Base terrain + colour-coded sector overlays |
| `City/Roads.tsx` | Road network via InstancedMesh per road type |
| `City/Buildings.tsx` | Buildings via single InstancedMesh |
| `City/Vegetation.tsx` | Trees (trunk + foliage) via two InstancedMeshes |
| `City/WaterBodies.tsx` | Water bodies from environment.water_bodies |
| `City/Sectors.tsx` | Stub (sector data used by Terrain instead) |
| `Types/City.ts` | Full TypeScript types for CityPlan schema |

---

## Coordinate convention

```
X = east/west   (city space: 0 → cityW)
Z = north/south (city space: 0 → cityD)
Y = height      (terrain at Y=0, sky up)
```

### Sector bounds format
```json
"bounds": [x, z, width, depth]   // origin + size, NOT min/max
```

### Road node positions
```json
"position": [x, z]   // city space coordinates
```

---

## Y-layer stack (no z-fighting)

| Layer | Y position |
|-------|-----------|
| Base terrain plane | -0.1 |
| Sector colour overlays | 0 (polygonOffset -1) |
| Water bodies | 0.05 (polygonOffset -2) |
| Road slabs | 0.08–0.12 (box height/2) |
| Building bottoms | 0 |
| Building centres | height/2 |
| Tree trunks | 2.5 × scale |
| Tree foliage | 7 × scale |

---

## Performance approach

- **InstancedMesh** for buildings (1 draw call for all buildings)
- **InstancedMesh per road type** (5–6 draw calls for all roads)
- **Two InstancedMeshes** for all trees (trunk + foliage = 2 draw calls)
- All Three.js objects built in `useMemo` and passed as `<primitive>`
- No thousands of individual React `<mesh>` components
- `matrixAutoUpdate = false` on all instanced meshes

---

## Known limitations

- Terrain is flat (no procedural height map); the JSON `terrain.height_scale` is not yet applied
- No energy infrastructure visualization (solar panels, wind turbines) — planned for later
- No sustainability metrics overlay
- Building colours are uniform per sector; facade variation not yet implemented
- No day/night cycle or shadow maps (kept simple for performance)

---

## Running locally

```bash
cd Frontend
npm install       # only needed once
npm run dev       # starts dev server at http://localhost:5173
npm run build     # production build (zero TypeScript errors)
```
