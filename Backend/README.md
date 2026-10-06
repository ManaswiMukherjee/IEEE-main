# Sustainable City Platform — Backend

## Overview

The backend is the central application and intelligence layer of the Sustainable City Planning Platform.

It connects:

```text
Frontend
   |
   v
Backend
   |
   +---- AI
   |
   +---- City Planner
   |
   +---- Database
   |
   +---- Weather
   |
   +---- Energy System
   |
   +---- ESP32
```

The backend is responsible for:

* Receiving requests from the frontend
* Validating user input
* Sending natural-language vision to the AI interpreter
* Converting user vision into structured requirements
* Generating sustainable city plans
* Validating generated city plans
* Providing CityPlan data to the frontend
* Managing city data
* Managing energy information
* Communicating with weather services
* Communicating with ESP32 hardware
* Providing APIs
* Handling errors
* Managing authentication in the future
* Managing persistent data

---

# Technology Stack

The backend uses:

* Node.js
* Express
* JavaScript
* REST API
* Zod
* dotenv
* CORS
* MongoDB/Mongoose initially if a document-oriented database is used

The backend can later be migrated to PostgreSQL + Prisma if the project requires more relational/geospatial querying.

---

# Backend Architecture

```text
                   FRONTEND
                       |
                       v
                    ROUTES
                       |
                       v
                 CONTROLLERS
                       |
                       v
                   SERVICES
                       |
        +--------------+--------------+
        |              |              |
        v              v              v
       AI            CITY           ENERGY
        |              |              |
        v              v              v
     LLM API       Planner        ESP32/API
                       |
                       v
                    CityPlan
                       |
                       v
                    Database
```

---

# Folder Structure

```text
Backend/
│
├── App/
│   │
│   ├── config/
│   ├── controllers/
│   ├── routes/
│   ├── services/
│   │   ├── ai/
│   │   ├── city/
│   │   ├── energy/
│   │   └── weather/
│   ├── models/
│   ├── middleware/
│   ├── utils/
│   └── server.js
│
├── .env
├── .env.example
├── package.json
└── package-lock.json
```

---

# `App/`

This is the main backend application directory.

All backend source code lives inside this directory.

---

# `App/config/`

Contains application configuration.

```text
config/
├── env.js
└── database.js
```

---

# `env.js`

Responsible for loading and validating environment variables.

Examples:

```text
PORT
DATABASE_URL
AI_API_KEY
WEATHER_API_KEY
ESP32_URL
```

The application should never hard-code secrets.

Bad:

```js
const API_KEY = "my-secret-key";
```

Good:

```text
.env
```

and:

```js
process.env.AI_API_KEY
```

---

# `database.js`

Responsible for database connection.

Conceptually:

```text
Backend
   |
database.js
   |
   v
Database
```

It should:

* Connect to database
* Handle connection errors
* Export database connection

---

# `App/controllers/`

Controllers handle HTTP requests.

```text
controllers/
├── ai.controller.js
├── city.controller.js
├── energy.controller.js
└── weather.controller.js
```

Controllers should remain relatively thin.

Their job is:

```text
Request
   |
Validate/extract data
   |
Call service
   |
Return response
```

Controllers should NOT contain the entire AI or planning algorithm.

---

# `ai.controller.js`

Handles AI-related HTTP requests.

Example:

```text
POST /api/ai/interpret
```

Input:

```json
{
  "vision": "Create a sustainable city..."
}
```

The controller passes the vision to:

```text
visionInterpreter.js
```

---

# `city.controller.js`

Handles city-related requests.

Examples:

```text
POST /api/city/generate

GET /api/city/:id
```

---

# `energy.controller.js`

Handles:

```text
GET /api/energy/status

POST /api/energy/control
```

It communicates with the energy services.

---

# `weather.controller.js`

Handles weather-related requests.

Example:

```text
GET /api/weather
```

---

# `App/routes/`

Contains API route definitions.

```text
routes/
├── ai.routes.js
├── city.routes.js
├── energy.routes.js
└── weather.routes.js
```

Routes define:

```text
HTTP Method
+
URL
+
Controller
```

Example:

```text
POST /api/ai/interpret
          |
          v
ai.controller.js
```

---

# `ai.routes.js`

AI routes.

```text
POST /api/ai/interpret
```

Future routes:

```text
POST /api/ai/analyze
GET  /api/ai/status
```

---

# `city.routes.js`

City routes.

```text
POST /api/city/generate
GET  /api/city/:id
GET  /api/city
DELETE /api/city/:id
```

---

# `energy.routes.js`

Energy routes.

```text
GET  /api/energy/status
POST /api/energy/control
GET  /api/energy/history
```

---

# `weather.routes.js`

Weather routes.

```text
GET /api/weather
GET /api/weather/forecast
```

---

# `App/services/`

This is the most important backend business-logic folder.

```text
services/
├── ai/
├── city/
├── energy/
└── weather/
```

Services contain actual application logic.

Controllers call services.

---

# `services/ai/`

Contains AI functionality.

```text
ai/
├── visionInterpreter.js
└── promptBuilder.js
```

---

# `visionInterpreter.js`

Responsible for converting natural language into structured requirements.

Example:

```text
User:

"I want a city for 100,000 people,
with high walkability, lots of parks,
solar energy and wind energy."

             |
             v

      AI Vision Interpreter
             |
             v

Structured Requirements
```

Example output:

```json
{
  "population": 100000,
  "urban_density": "medium_high",
  "transport": {
    "public_transport_priority": "high",
    "walkability": "high"
  },
  "environment": {
    "green_space_priority": "high"
  },
  "energy": {
    "renewable_priority": "high",
    "preferred_sources": [
      "solar",
      "wind"
    ]
  }
}
```

Important:

The AI interpreter should **not generate the final city geometry**.

Its responsibility is understanding the user's intention.

---

# `promptBuilder.js`

Builds structured prompts for the AI model.

Instead of writing prompts everywhere:

```js
const prompt = "...";
```

we centralize prompt construction.

It can create prompts for:

```text
Vision interpretation
Requirement extraction
Planning assistance
```

---

# `services/city/`

Responsible for sustainable city planning.

```text
city/
├── cityPlanner.js
├── cityValidator.js
└── cityGenerator.js
```

---

# `cityPlanner.js`

This is the actual city-planning logic.

Input:

```text
Structured Requirements
+
Geographic Information
+
Environmental Information
+
Population Requirements
+
Energy Requirements
```

Output:

```text
CityPlan
```

Example:

```text
Requirements
     |
     v
City Planner
     |
     v
Residential zones
Commercial zones
Industrial zones
Roads
Parks
Solar
Wind
Water
     |
     v
CityPlan
```

The planner should consider:

* Population capacity
* Land availability
* Terrain
* Walkability
* Transportation
* Green space
* Renewable energy
* Infrastructure connectivity
* Environmental constraints

---

# `cityValidator.js`

Validates the generated city plan.

This is extremely important.

The planner should not be allowed to return invalid data.

Validation can check:

```text
✓ Required fields exist

✓ Buildings don't overlap

✓ Roads are connected

✓ Restricted areas are respected

✓ Population capacity is sufficient

✓ Energy zones are valid

✓ Green-space requirement is satisfied
```

It should validate against:

```text
Shared/Schemas/CityPlan.schema.json
```

---

# `cityGenerator.js`

Responsible for procedural generation of city geometry/data.

Important distinction:

```text
AI
 |
understands intention
 |
v
Planner
 |
decides logical city structure
 |
v
Generator
 |
creates exact geometry/data
 |
v
CityPlan
```

For example, the generator can create:

```text
100 buildings
50 roads
10 parks
3 solar farms
2 wind farms
```

according to the planner's requirements.

---

# `services/energy/`

Handles energy intelligence.

```text
energy/
├── energyAnalyzer.js
└── esp32.service.js
```

---

# `energyAnalyzer.js`

Analyzes:

```text
Solar availability
Wind availability
Battery state
Grid availability
Power demand
Weather
Time
```

It determines the most appropriate energy source.

Example:

```text
Solar = 85%
Wind = 42%
Battery = 60%
Grid = available
Demand = 100 kW

             |
             v

      Energy Analyzer

             |
             v

Selected source:
SOLAR
```

Example output:

```json
{
  "solar": 0.85,
  "wind": 0.42,
  "grid": 0.15,
  "battery": 0.60,
  "selected_source": "solar"
}
```

---

# `esp32.service.js`

Handles communication with ESP32 hardware.

Conceptually:

```text
Weather
   |
Energy Analyzer
   |
Decision
   |
ESP32 Service
   |
   v
ESP32
   |
   +---- Solar
   +---- Wind
   +---- Grid
   +---- Battery
```

It may eventually use:

```text
HTTP
MQTT
WebSocket
Serial
```

depending on the hardware architecture.

---

# `services/weather/`

Handles external weather services.

```text
weather/
└── weather.service.js
```

---

# `weather.service.js`

Responsible for obtaining:

* Temperature
* Humidity
* Cloud cover
* Solar irradiance
* Wind speed
* Rainfall
* Forecast data

The backend should act as the middle layer.

Instead of:

```text
Frontend ---> Weather API
```

prefer:

```text
Frontend
   |
   v
Backend
   |
   v
Weather API
```

This allows API keys to remain private.

---

# `App/models/`

Database models.

```text
models/
├── CityPlan.js
├── User.js
└── EnergyLog.js
```

---

# `CityPlan.js`

Represents stored city plans.

Possible data:

```text
City name
Population
Terrain
Sectors
Roads
Energy zones
Environment
Created date
User
```

The stored structure should remain compatible with:

```text
Shared/Schemas/CityPlan.schema.json
```

---

# `User.js`

Represents users.

Possible future fields:

```text
name
email
password
createdAt
role
```

Authentication does not need to be implemented in the first prototype.

---

# `EnergyLog.js`

Stores historical energy information.

Example:

```text
timestamp
solar
wind
battery
grid
demand
selected_source
```

This allows the frontend to display historical charts.

---

# `App/middleware/`

Contains Express middleware.

```text
middleware/
├── error.middleware.js
├── auth.middleware.js
└── validation.middleware.js
```

---

# `error.middleware.js`

Centralized error handling.

Instead of every controller handling errors differently:

```text
Controller
    |
    v
Error
    |
    v
error.middleware.js
    |
    v
Standard API response
```

---

# `auth.middleware.js`

Handles authentication.

Future responsibilities:

* JWT verification
* User identification
* Role checking

Example:

```text
Request
   |
JWT
   |
auth.middleware
   |
Controller
```

This can remain minimal during the initial prototype.

---

# `validation.middleware.js`

Validates incoming API data.

For example:

```text
POST /api/city/generate
```

should validate:

```text
population
terrain
city area
requirements
```

before reaching the planner.

---

# `App/utils/`

Contains reusable backend utility functions.

```text
utils/
├── logger.js
├── response.js
└── validator.js
```

---

# `logger.js`

Centralized logging.

Used for:

```text
API requests
AI calls
Planner execution
Errors
ESP32 communication
```

---

# `response.js`

Creates consistent API responses.

Example:

```json
{
  "success": true,
  "data": {}
}
```

Error:

```json
{
  "success": false,
  "error": {
    "message": "Invalid city plan"
  }
}
```

---

# `validator.js`

Reusable validation functions.

Can contain:

* JSON schema validation
* Common field validation
* CityPlan validation helpers

---

# `server.js`

This is the backend entry point.

It is responsible for:

```text
Create Express app
       |
Configure middleware
       |
Connect database
       |
Register routes
       |
Start server
```

Conceptually:

```text
server.js
   |
   +-- dotenv
   |
   +-- CORS
   |
   +-- JSON parser
   |
   +-- Routes
   |
   +-- Error handling
   |
   v
Express Server
```

---

# `.env`

Contains local secrets/configuration.

Example:

```env
PORT=5000

DATABASE_URL=

AI_API_KEY=

WEATHER_API_KEY=

ESP32_URL=
```

Never commit `.env` to GitHub.

---

# `.env.example`

Contains the required environment variable names without secrets.

Example:

```env
PORT=5000
DATABASE_URL=
AI_API_KEY=
WEATHER_API_KEY=
ESP32_URL=
```

This file SHOULD be committed.

---

# API Architecture

The backend exposes REST APIs.

## AI

```http
POST /api/ai/interpret
```

Input:

```json
{
  "vision": "I want a sustainable city for 100000 people..."
}
```

Output:

```json
{
  "success": true,
  "data": {
    "requirements": {}
  }
}
```

---

# City

```http
POST /api/city/generate
```

Generates a city.

Input:

```json
{
  "requirements": {},
  "geography": {},
  "environment": {}
}
```

Output:

```json
{
  "success": true,
  "data": {
    "cityPlan": {}
  }
}
```

---

# Get City

```http
GET /api/city/:id
```

Returns an existing CityPlan.

---

# Energy

```http
GET /api/energy/status
```

Returns current energy information.

Example:

```json
{
  "solar": 0.85,
  "wind": 0.42,
  "battery": 0.60,
  "grid": 0.15,
  "demand": 100,
  "selected_source": "solar"
}
```

---

# Energy Control

```http
POST /api/energy/control
```

Used to communicate an energy decision to the hardware layer.

---

# Weather

```http
GET /api/weather
```

Returns current weather/environmental information.

---

# Complete Backend Data Flow

The main city-generation flow is:

```text
USER
 |
 v
FRONTEND
 |
 | POST /api/ai/interpret
 v
AI CONTROLLER
 |
 v
VISION INTERPRETER
 |
 v
STRUCTURED REQUIREMENTS
 |
 v
FRONTEND
 |
 | POST /api/city/generate
 v
CITY CONTROLLER
 |
 v
CITY PLANNER
 |
 v
CITY GENERATOR
 |
 v
CITY VALIDATOR
 |
 v
VALID CITY PLAN
 |
 +----------+
 |          |
 v          v
DATABASE   FRONTEND
              |
              v
          3D CITY
```

---

# Energy Data Flow

```text
Weather API
     |
     v
Weather Service
     |
     v
Energy Analyzer
     |
     +------ Solar
     +------ Wind
     +------ Battery
     +------ Grid
     +------ Demand
     |
     v
Energy Decision
     |
     +------------+
     |            |
     v            v
Frontend        ESP32
Dashboard
```

---

# Separation of Responsibilities

This separation is critical.

## AI Interpreter

```text
Understands what the user wants.
```

It does NOT create the final city.

---

## City Planner

```text
Decides the logical structure of the city.
```

---

## City Generator

```text
Creates the actual city geometry/data.
```

---

## City Validator

```text
Makes sure the generated city is valid.
```

---

## Frontend

```text
Visualizes and interacts with the result.
```

---

## Energy Analyzer

```text
Determines the best energy allocation.
```

---

## ESP32 Service

```text
Communicates decisions to hardware.
```

---

# Most Important Shared Contract

The backend and frontend must communicate through a standardized CityPlan.

The primary source of truth is:

```text
Shared/
└── Schemas/
    └── CityPlan.schema.json
```

The flow is:

```text
City Planner
      |
      v
CityPlan JSON
      |
      +----------------+
      |                |
      v                v
Database          Frontend
                       |
                       v
                  3D Renderer
```

This means the frontend does not need to understand how the planner generated the city.

It only needs to understand the `CityPlan` structure.

---

# Backend Responsibilities

```text
API
✓

AI integration
✓

Requirement interpretation
✓

City planning
✓

City generation
✓

City validation
✓

Database
✓

Weather integration
✓

Energy analysis
✓

ESP32 communication
✓

Authentication
✓ Future

3D rendering
✗
```

The backend should **never contain Three.js rendering code**.

---

# Development

Start the backend:

```bash
npm run dev
```

or:

```bash
node App/server.js
```

Example server:

```text
http://localhost:5000
```

---

# Development Order

The backend should be developed in this order:

### Phase 1 — Basic server

```text
server.js
    |
    v
Express
    |
    v
GET /api/health
```

### Phase 2 — AI

```text
POST /api/ai/interpret
```

### Phase 3 — City Planner

```text
POST /api/city/generate
```

### Phase 4 — City validation

```text
CityPlan schema
+
City validator
```

### Phase 5 — Database

Store generated cities.

### Phase 6 — Energy

Implement:

```text
Weather
+
Energy Analyzer
+
Energy API
```

### Phase 7 — ESP32

Connect:

```text
Energy Analyzer
       |
       v
ESP32
```

---

# Final Architecture Principle

The backend should follow:

```text
RECEIVE
   ↓
VALIDATE
   ↓
PROCESS
   ↓
GENERATE
   ↓
VALIDATE RESULT
   ↓
STORE
   ↓
RETURN
```

The most important separation is:

```text
AI
 ↓
UNDERSTAND USER INTENT

PLANNER
 ↓
MAKE CITY DECISIONS

GENERATOR
 ↓
CREATE CITY DATA

VALIDATOR
 ↓
CHECK CITY

FRONTEND
 ↓
VISUALIZE CITY
```

This architecture keeps the project modular and allows each subsystem to evolve independently.
