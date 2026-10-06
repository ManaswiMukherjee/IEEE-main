# IEEE Sustainable City Project — README

## Project Overview

An AI-driven sustainable city planning and visualization system.

The system takes a user vision (text/image), generates a sustainable city plan using AI, and renders it as an interactive 3D city in the browser.

---

## Repository Structure

```
IEEE-main/
├── Frontend/           ← React + Three.js 3D city renderer
├── Backend/            ← Node.js/Express API + AI city planner  
├── Hardware/           ← ESP32 energy monitoring integration
├── Shared/
│   └── Schemas/
│       └── CityPlan.schema.json   ← JSON schema for city plan format
└── Docs/               ← System design documents
```

---

## Quick Start (Frontend)

```bash
cd Frontend
npm install
npm run dev
# Open http://localhost:5173
```

The browser will load `TestCity.json` and display "Mumbai Sustainable District" as a 3D interactive city.

---

## City Plan JSON Format

The city plan is the central data contract between the AI planner and the 3D renderer.

See [`Shared/Schemas/CityPlan.schema.json`](Shared/Schemas/CityPlan.schema.json) for the full schema.

### Key fields

```json
{
  "city": {
    "name": "...",
    "seed": 42,
    "dimensions": [7071, 7071],   // [width, depth] in metres
    "population_target": 100000
  },
  "terrain": { "type": "rolling", "height_scale": 15 },
  "sectors": [
    {
      "id": "residential_north",
      "type": "residential",
      "bounds": [500, 5000, 2500, 1800],   // [x, z, width, depth]
      "density": 0.75,
      "building_height": [4, 12]
    }
  ],
  "road_graph": {
    "nodes": [{ "id": "node_central", "position": [4300, 4500] }],
    "edges": [{ "id": "e1", "from": "node_a", "to": "node_b", "type": "arterial", "width": 16 }]
  },
  "energy_zones": [...],
  "environment": {
    "minimum_green_ratio": 0.25,
    "water_bodies": [{ "id": "lake", "bounds": [100, 200, 1200, 1000] }]
  }
}
```

### Coordinate system

```
X = east/west   (0 → city width)
Z = north/south (0 → city depth)
Y = height      (up)
```

### Sector bounds interpretation

`bounds = [x, z, width, depth]` — **origin + size** format (NOT min/max corners).

---

## Subsystems

### 1. User Vision Interpreter
Converts user text/image description into structured requirements.
See [`Docs/UserVisionInterpreter.txt`](Docs/UserVisionInterpreter.txt)

### 2. Sustainable City Planner  
AI that generates a city plan JSON from requirements.
See [`Docs/SustainableCityPlanner.txt`](Docs/SustainableCityPlanner.txt)

### 3. 3D City Renderer
Converts city plan JSON into interactive 3D visualization.
See [`Docs/CityInteractiveViewGenerator.txt`](Docs/CityInteractiveViewGenerator.txt)
**Implementation: [`Frontend/`](Frontend/)**

### 4. Energy Allocation Analyzer
Analyzes energy zones and sustainability metrics.
See [`Docs/EnenryAllocationAnalyzer.txt`](Docs/EnenryAllocationAnalyzer.txt)

---

## Test Data

`Frontend/public/Plans/TestCity.json` — "Mumbai Sustainable District"

- 7071 × 7071 m city
- 12 sectors (residential, commercial, industrial, civic, green, energy, transport)  
- 15 road nodes, 21 edges (arterial, collector, local, pedestrian, cycle roads)
- 6 energy zones (solar, wind, battery, substation)
- 2 water bodies

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend framework | React 19 + TypeScript |
| 3D rendering | Three.js + React Three Fiber + @react-three/drei |
| Build tool | Vite 8 |
| Backend | Node.js / Express |
| AI | (planned) LLM via backend |
| Hardware | ESP32 + sensors |
