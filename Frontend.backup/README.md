# Frontend README

## What this is

The 3D city renderer for the IEEE Sustainable City project.

Loads a `CityPlan` JSON file and renders it as an interactive 3D city in the browser using React Three Fiber and Three.js.

## Running

```bash
npm install    # install dependencies (only needed once)
npm run dev    # start dev server at http://localhost:5173
npm run build  # production build
```

## Key files

```
Src/
├── App.tsx              # Root: fetches city plan JSON, shows loading/error states
├── main.tsx             # React entry point
├── index.css            # Full-screen reset styles
├── Types/
│   └── City.ts          # TypeScript types for CityPlan schema
└── City/
    ├── CityScene.tsx     # R3F Canvas, camera, lighting, OrbitControls
    ├── CityRenderer.tsx  # Assembles Terrain + Roads + Buildings + Vegetation + Water
    ├── Terrain.tsx       # Ground plane + sector colour overlays
    ├── Roads.tsx         # Road network (InstancedMesh per road type)
    ├── Buildings.tsx     # Buildings (single InstancedMesh for all)
    ├── Vegetation.tsx    # Trees in green zones (2 InstancedMeshes)
    ├── WaterBodies.tsx   # Water bodies from environment.water_bodies
    └── Sectors.tsx       # Stub (sector geometry handled by Terrain)
```

## City plan location

`public/Plans/TestCity.json`

Served at `/Plans/TestCity.json` by Vite's static file server. Loaded with `fetch()` in `App.tsx`.

## Coordinate system

```
X = east/west   (city space: 0 → cityW)
Z = north/south (city space: 0 → cityD)
Y = height      (up)
```

Sector `bounds` format: `[x, z, width, depth]` (origin + size, NOT min-max corners).

## Performance

All repeated geometry uses Three.js `InstancedMesh`:
- All buildings → 1 draw call
- Roads per type → 5-6 draw calls  
- Tree trunks → 1 draw call
- Tree foliage → 1 draw call

Total scene: ~10-15 draw calls regardless of city size.
