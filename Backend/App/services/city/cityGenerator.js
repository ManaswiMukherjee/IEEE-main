const generateCity = async (plan) => {
  if (!plan?.city || !plan?.terrain) {
    throw new Error("A planned city is required.");
  }

  const population = plan.population || plan.city.population_target;
  const sectors = [
    { id: "residential-01", type: "residential", bounds: [0, 0, 400, 400], density: 0.65, building_height: [8, 24], population_target: Math.floor(population * 0.6) },
    { id: "mixed-01", type: "mixed", bounds: [400, 0, 300, 400], density: 0.75, building_height: [6, 20], population_target: population - Math.floor(population * 0.6) },
    { id: "commercial-01", type: "commercial", bounds: [700, 0, 300, 250], density: 0.8, building_height: [8, 30] },
    { id: "civic-01", type: "civic", bounds: [700, 250, 300, 200], density: 0.55, building_height: [4, 16] },
    { id: "green-01", type: "green", bounds: [0, 400, 700, 600] },
    { id: "energy-01", type: "energy", bounds: [700, 450, 300, 250] },
    { id: "transport-01", type: "transport", bounds: [700, 700, 300, 300] },
  ];

  return {
    city: plan.city,
    terrain: plan.terrain,
    sectors,
    road_graph: {
    nodes: [
      { id: "node-01", position: [0, 0] },
      { id: "node-02", position: [500, 0] },
      { id: "node-03", position: [1000, 0] },
      { id: "node-04", position: [1000, 500] },
      { id: "node-05", position: [1000, 1000] },
      { id: "node-06", position: [0, 1000] },
    ],
    edges: [
      { id: "road-01", from: "node-01", to: "node-02", type: "arterial", width: 12 },
      { id: "road-02", from: "node-02", to: "node-03", type: "arterial", width: 12 },
      { id: "road-03", from: "node-03", to: "node-04", type: "collector", width: 8 },
      { id: "road-04", from: "node-04", to: "node-05", type: "collector", width: 8 },
      { id: "road-05", from: "node-05", to: "node-06", type: "arterial", width: 12 },
      { id: "road-06", from: "node-06", to: "node-01", type: "arterial", width: 12 },
      { id: "road-07", from: "node-02", to: "node-05", type: "local", width: 6 },
    ],
    },
    energy_zones: [
      { id: "solar-01", type: "solar", sector: "energy-01", capacity: 0.5, unit: "MW" },
      { id: "wind-01", type: "wind", sector: "energy-01", capacity: 0.3, unit: "MW" },
      { id: "battery-01", type: "battery", sector: "energy-01", capacity: 1, unit: "MWh" },
    ],
    environment: {
      minimum_green_ratio: plan.greenRatio || 0.25,
      water_bodies: plan.waterBodies,
    },
  };
};

module.exports = {
  generateCity,
};