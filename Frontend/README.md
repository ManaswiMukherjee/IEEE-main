# Sustainable City Platform — Frontend

## Overview

The frontend is the user-facing application of the Sustainable City Planning Platform.

It is responsible for:

* Collecting the user's city vision
* Collecting structured city requirements
* Sending requests to the backend
* Displaying AI interpretation results
* Displaying generated city plans
* Rendering the city as an interactive 3D environment
* Displaying sustainability statistics
* Displaying energy information
* Displaying real-time energy status
* Providing controls for city visualization
* Managing frontend application state

The frontend does **not** make the actual city-planning decisions.

The backend/AI/planning system determines what the city should contain. The frontend receives the resulting `CityPlan` and visualizes it.

---

# Technology Stack

The frontend is built using:

* React
* TypeScript
* Vite
* React Router
* Tailwind CSS
* Three.js
* React Three Fiber
* React Three Drei
* Zustand
* Axios
* Recharts
* Zod

---

# High-Level Architecture

```text
                    USER
                      |
                      v
              React Frontend
                      |
          +-----------+-----------+
          |                       |
          v                       v
     Planner UI              Energy UI
          |                       |
          v                       v
      Backend API <-------- Energy API
          |
          v
     AI Interpreter
          |
          v
   Structured Requirements
          |
          v
   Sustainable City Planner
          |
          v
       CityPlan
          |
          v
    Frontend 3D Engine
          |
          v
    Interactive City
```

---

# Folder Structure

```text
Frontend/
│
├── public/
│
├── src/
│   │
│   ├── assets/
│   │
│   ├── components/
│   │
│   ├── features/
│   │
│   ├── pages/
│   │
│   ├── hooks/
│   │
│   ├── store/
│   │
│   ├── services/
│   │
│   ├── types/
│   │
│   ├── utils/
│   │
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
│
├── package.json
├── tsconfig.json
├── tsconfig.app.json
└── vite.config.ts
```

---

# `public/`

Contains static files that are served directly by Vite.

```text
public/
├── favicon.svg
└── icons.svg
```

## Purpose

Files inside `public/` are not processed by the React build system.

They are useful for:

* Favicons
* Static icons
* Static metadata
* Public files that need a fixed URL

Example:

```text
public/favicon.svg
```

can be accessed as:

```text
/favicon.svg
```

Do not put React components inside `public/`.

---

# `src/`

This is the main application source directory.

Almost all frontend application code belongs inside `src/`.

---

# `src/assets/`

Contains frontend assets that are imported into React components.

```text
assets/
├── images/
├── textures/
└── models/
```

## `assets/images/`

Contains:

* UI images
* Background images
* Logos
* Illustrations
* City-related images

Example:

```text
assets/images/
├── logo.png
├── city-background.jpg
└── sustainability.png
```

---

## `assets/textures/`

Contains textures used by the 3D city.

Examples:

```text
assets/textures/
├── road/
├── grass/
├── concrete/
├── water/
└── building/
```

These can be used by Three.js materials.

---

## `assets/models/`

Contains 3D models.

Possible future models:

```text
assets/models/
├── tree.glb
├── car.glb
├── wind-turbine.glb
├── solar-panel.glb
└── building.glb
```

The frontend should only use these models for visualization.

The backend decides where objects belong.

---

# `src/components/`

Contains reusable UI components.

Components in this folder should generally be independent of a particular page.

```text
components/
├── ui/
├── layout/
└── common/
```

---

# `src/components/ui/`

Contains basic reusable UI components.

```text
ui/
├── Button.tsx
├── Card.tsx
├── Modal.tsx
└── Loading.tsx
```

## `Button.tsx`

Reusable button component.

Example:

```tsx
<Button>
    Generate City
</Button>
```

Should handle:

* Variants
* Disabled state
* Loading state
* Click events

---

## `Card.tsx`

Reusable container for dashboard information.

Used for:

* Population
* Renewable energy
* Green-space percentage
* Power demand
* City statistics

---

## `Modal.tsx`

Reusable popup component.

Possible uses:

* City information
* Building information
* Energy details
* Error messages
* Settings

---

## `Loading.tsx`

Reusable loading indicator.

Used when:

* AI is processing
* City is generating
* API request is running
* 3D city is loading

---

# `src/components/layout/`

Contains application layout components.

```text
layout/
├── Navbar.tsx
├── Sidebar.tsx
└── DashboardLayout.tsx
```

## `Navbar.tsx`

Top navigation.

Possible links:

```text
Home
Planner
City
Energy
```

---

## `Sidebar.tsx`

Dashboard navigation.

Could contain:

```text
City Overview
3D City
Infrastructure
Energy
Environment
Analytics
Settings
```

---

## `DashboardLayout.tsx`

Defines the overall dashboard structure.

Example:

```text
+--------------------------------+
| Navbar                         |
+--------+-----------------------+
| Sidebar| Main Content          |
|        |                       |
|        |                       |
+--------+-----------------------+
```

---

# `src/components/common/`

Contains reusable components that are more specific than basic UI components.

```text
common/
├── StatCard.tsx
└── StatusBadge.tsx
```

## `StatCard.tsx`

Displays important city metrics.

Example:

```text
Population
100,000
```

or:

```text
Renewable Energy
78%
```

---

## `StatusBadge.tsx`

Displays states such as:

```text
ONLINE
OFFLINE
WARNING
ACTIVE
GENERATING
```

---

# `src/features/`

This is one of the most important folders.

The application is divided into major business features.

```text
features/
├── planner/
├── ai/
├── city/
└── energy/
```

Each feature contains everything specifically related to that functionality.

This prevents the project from becoming one huge collection of components.

---

# `src/features/planner/`

Responsible for the city planning interface.

```text
planner/
├── PlannerForm.tsx
├── VisionInput.tsx
├── RequirementSummary.tsx
└── planner.api.ts
```

## `PlannerForm.tsx`

Main planning form.

The user can provide:

* City name
* Population
* Area
* Terrain
* Climate
* Geographic information
* Energy preference
* Sustainability requirements

---

## `VisionInput.tsx`

Natural-language input.

Example:

```text
"Create a sustainable city for 100,000 people
with high walkability and renewable energy."
```

This is sent to the AI interpreter.

---

## `RequirementSummary.tsx`

Displays the structured interpretation returned by the backend.

Example:

```text
Population: 100,000

Density:
Medium-High

Walkability:
High

Public Transport:
High

Green Space:
High

Renewable Priority:
High

Preferred Energy:
Solar + Wind
```

The user should be able to review this before city generation.

---

## `planner.api.ts`

Contains API functions specifically related to planning.

Example:

```ts
interpretVision()
generateCity()
```

It should not contain UI code.

---

# `src/features/ai/`

Responsible for showing AI processing.

```text
ai/
├── AIAnalysis.tsx
└── AIProgress.tsx
```

## `AIAnalysis.tsx`

Displays the AI's interpretation.

Example:

```text
Analyzed Requirements

Population       100,000
Density          Medium-High
Walkability      High
Renewable        High
Green Space      High
```

---

## `AIProgress.tsx`

Displays progress while AI/planner operations are running.

Example:

```text
✓ Understanding city vision

✓ Extracting requirements

● Planning infrastructure

○ Generating city

○ Preparing 3D visualization
```

---

# `src/features/city/`

This is the core 3D visualization system.

```text
city/
├── CityScene.tsx
├── CityBuilding.tsx
├── CityRoad.tsx
├── CityPark.tsx
├── SolarFarm.tsx
├── WindFarm.tsx
└── WaterBody.tsx
```

---

# `CityScene.tsx`

The main Three.js / React Three Fiber scene.

It receives a `CityPlan`.

Conceptually:

```text
CityPlan
   |
   +-- sectors
   +-- roads
   +-- energy
   +-- environment
   |
   v
CityScene
```

It creates:

* Camera
* Lights
* Terrain
* Buildings
* Roads
* Parks
* Energy infrastructure
* Water

---

# `CityBuilding.tsx`

Converts city sector/building information into 3D buildings.

For example:

```text
Residential sector
        |
        v
CityBuilding
        |
        v
3D building
```

The component should **not decide where the building belongs**.

The CityPlan determines its location.

---

# `CityRoad.tsx`

Visualizes the road graph.

The backend provides:

```text
nodes
edges
road type
width
```

The frontend converts these into 3D roads.

Road types:

```text
highway
arterial
collector
local
pedestrian
cycle
```

---

# `CityPark.tsx`

Visualizes:

* Parks
* Green areas
* Vegetation
* Open spaces

---

# `SolarFarm.tsx`

Visualizes solar infrastructure.

Possible visualization:

```text
Solar Zone
    |
    v
Solar panels
    |
    v
Energy visualization
```

---

# `WindFarm.tsx`

Visualizes wind turbines.

---

# `WaterBody.tsx`

Visualizes:

* Lakes
* Rivers
* Reservoirs
* Other water bodies

---

# `src/features/energy/`

Responsible for the energy dashboard.

```text
energy/
├── EnergyDashboard.tsx
├── EnergyChart.tsx
├── PowerFlow.tsx
└── energy.api.ts
```

---

# `EnergyDashboard.tsx`

Main energy dashboard.

Displays:

```text
Solar
Wind
Battery
Grid
Demand
```

Example:

```text
Solar        82 kW
Wind         43 kW
Battery      65%
Grid         15 kW
Demand       110 kW
```

---

# `EnergyChart.tsx`

Charts energy data over time.

Examples:

* Solar generation
* Wind generation
* Power demand
* Grid usage
* Battery level

Uses Recharts.

---

# `PowerFlow.tsx`

Visualizes where electricity is coming from.

Example:

```text
          SOLAR
            |
            v
        +-------+
        |  CITY |
        +-------+
          ^
          |
        BATTERY

          ^
          |
         GRID
```

This can later become an animated energy-flow visualization.

---

# `energy.api.ts`

Contains API functions for:

* Current energy status
* Energy decisions
* Historical energy data
* ESP32 status

---

# `src/pages/`

Contains complete application pages.

```text
pages/
├── Home.tsx
├── Planner.tsx
├── AIProcessing.tsx
├── CityDashboard.tsx
├── EnergyDashboard.tsx
└── NotFound.tsx
```

Pages combine feature components.

They should not contain large amounts of business logic.

---

# `Home.tsx`

Landing page.

Contains:

* Project introduction
* Sustainable city concept
* Start planning button
* Feature overview

---

# `Planner.tsx`

Main city planning page.

Combines:

```text
VisionInput
PlannerForm
RequirementSummary
```

---

# `AIProcessing.tsx`

Shows the AI and city-generation process.

---

# `CityDashboard.tsx`

Main interactive city page.

Contains:

```text
3D City
+
City Statistics
+
Layer Controls
+
Sustainability Data
```

---

# `EnergyDashboard.tsx`

Main energy monitoring page.

---

# `NotFound.tsx`

404 page.

---

# `src/hooks/`

Contains reusable React hooks.

```text
hooks/
├── useCityPlan.ts
├── useEnergy.ts
└── useWebSocket.ts
```

## `useCityPlan.ts`

Handles city-plan-related frontend logic.

Example:

```ts
const {
    cityPlan,
    loading,
    error,
    generateCity
} = useCityPlan();
```

---

## `useEnergy.ts`

Handles energy state.

---

## `useWebSocket.ts`

Handles real-time backend communication.

Potential future use:

```text
Backend
   |
WebSocket
   |
Frontend
```

This is useful for live ESP32 energy data.

---

# `src/store/`

Global frontend state using Zustand.

```text
store/
├── cityStore.ts
└── energyStore.ts
```

---

# `cityStore.ts`

Stores information such as:

```text
current CityPlan
selected building
selected sector
active city layers
camera state
```

---

# `energyStore.ts`

Stores:

```text
solar
wind
battery
grid
demand
selected source
```

---

# `src/services/`

Contains communication with external systems.

```text
services/
├── api.ts
├── aiApi.ts
├── cityApi.ts
└── energyApi.ts
```

---

# `api.ts`

Base Axios configuration.

Example:

```text
Backend:
http://localhost:5000

Frontend:
http://localhost:5173
```

All API services can use the same Axios instance.

---

# `aiApi.ts`

Handles:

```text
POST /api/ai/interpret
```

---

# `cityApi.ts`

Handles:

```text
POST /api/city/generate
GET /api/city/:id
```

---

# `energyApi.ts`

Handles:

```text
GET /api/energy/status
POST /api/energy/control
```

---

# `src/types/`

Contains TypeScript types and interfaces.

```text
types/
├── city.ts
├── ai.ts
├── energy.ts
└── api.ts
```

This folder is extremely important.

---

# `city.ts`

Defines frontend representation of the shared CityPlan.

Examples:

```ts
City
Terrain
Sector
Road
EnergyZone
Environment
CityPlan
```

These types should match:

```text
Shared/Schemas/CityPlan.schema.json
```

---

# `ai.ts`

Defines:

```text
VisionRequest
StructuredRequirements
AIInterpretation
```

---

# `energy.ts`

Defines:

```text
EnergyStatus
EnergyDecision
EnergyReading
```

---

# `api.ts`

Defines generic API response types.

Example:

```ts
ApiResponse<T>
ApiError
```

---

# `src/utils/`

Contains pure utility functions.

```text
utils/
├── coordinates.ts
├── calculations.ts
└── cityParser.ts
```

---

# `coordinates.ts`

Converts geographic coordinates into 3D coordinates.

Example:

```text
Latitude/Longitude
       |
       v
3D X/Y/Z
```

---

# `calculations.ts`

Contains calculations such as:

* Energy percentages
* Sustainability scores
* Distance calculations
* Population density
* Green-space percentage

---

# `cityParser.ts`

Converts/normalizes CityPlan data for the 3D engine.

It should not make planning decisions.

---

# `App.tsx`

Root React component.

Responsible for:

* Application routes
* Global layouts
* Main application structure

Example routes:

```text
/
 /planner
 /ai-processing
 /city
 /energy
```

---

# `main.tsx`

React entry point.

It initializes the React application.

Conceptually:

```text
main.tsx
   |
   v
App.tsx
   |
   +-- Router
   +-- Layout
   +-- Pages
```

---

# `index.css`

Global CSS.

Contains:

* Font configuration
* Global styles
* Tailwind directives
* CSS variables
* Base styles

---

# Frontend Data Flow

The most important frontend flow is:

```text
User
 |
 | Natural language
 v
VisionInput
 |
 v
planner.api.ts
 |
 v
Backend
 |
 v
AI Interpreter
 |
 v
Structured Requirements
 |
 v
RequirementSummary
 |
 | User confirms
 v
City Generator API
 |
 v
CityPlan
 |
 v
cityStore
 |
 v
CityScene
 |
 +----> CityBuilding
 +----> CityRoad
 +----> CityPark
 +----> SolarFarm
 +----> WindFarm
 +----> WaterBody
```

---

# Important Frontend Rule

The frontend **does not decide city planning**.

For example, the frontend should NOT do this:

```text
"If population > 100000,
create 50 buildings."
```

Instead:

```text
Backend Planner
       |
       v
CityPlan
       |
       v
Frontend
       |
       v
Visualization
```

The frontend is primarily a **visualization and interaction layer**.

---

# Frontend Responsibilities

```text
UI
✓

User input
✓

API communication
✓

State management
✓

3D visualization
✓

Charts
✓

Energy visualization
✓

Real-time display
✓

City planning decisions
✗

AI reasoning
✗

Infrastructure optimization
✗
```

---

# Development Command

```bash
npm run dev
```

Frontend will normally run on:

```text
http://localhost:5173
```

---

# Production Build

```bash
npm run build
```

Preview:

```bash
npm run preview
```

---

# Final Principle

The frontend should follow this principle:

```text
BACKEND DECIDES
       ↓
FRONTEND VISUALIZES
```

The backend/AI system produces the city plan.

The frontend turns that plan into an interactive experience.
