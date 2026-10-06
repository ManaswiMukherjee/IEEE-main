const CityPlan = {
  collectionName: "city_plans",

  fields: {
    city: "object",
    terrain: "object",
    sectors: "array",
    road_graph: "object",
    energy_zones: "array",
    environment: "object",
    createdAt: "date",
    updatedAt: "date",
  },
};

module.exports = CityPlan;