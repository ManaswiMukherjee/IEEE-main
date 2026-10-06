const TERRAIN_TYPES = new Set(["flat", "rolling", "hilly", "mountainous"]);
const SECTOR_TYPES = new Set(["residential", "commercial", "industrial", "civic", "mixed", "green", "energy", "transport"]);
const ROAD_TYPES = new Set(["highway", "arterial", "collector", "local", "pedestrian", "cycle"]);
const ENERGY_TYPES = new Set(["solar", "wind", "hydro", "battery", "grid", "substation"]);

const isNumber = (value) => typeof value === "number" && Number.isFinite(value);
const validBounds = (bounds) => Array.isArray(bounds) && bounds.length === 4 && bounds.every(isNumber);
const validPositivePair = (pair) => Array.isArray(pair) && pair.length === 2 && pair.every((value) => isNumber(value) && value > 0);

const validateCityPlan = (cityPlan) => {
  const errors = [];
  if (!cityPlan || typeof cityPlan !== "object") return { valid: false, errors: ["City plan is missing."] };

  if (!cityPlan.city || typeof cityPlan.city.name !== "string" || !Number.isInteger(cityPlan.city.seed) || cityPlan.city.seed < 0 || !validPositivePair(cityPlan.city.dimensions) || !Number.isInteger(cityPlan.city.population_target) || cityPlan.city.population_target < 1) errors.push("City metadata is invalid.");
  if (!cityPlan.terrain || !TERRAIN_TYPES.has(cityPlan.terrain.type) || !isNumber(cityPlan.terrain.height_scale) || cityPlan.terrain.height_scale < 0) errors.push("Terrain is invalid.");
  if (!Array.isArray(cityPlan.sectors) || cityPlan.sectors.length === 0) errors.push("Sectors must be a non-empty array.");
  if (!Array.isArray(cityPlan.energy_zones)) errors.push("Energy zones must be an array.");
  if (!cityPlan.environment || !isNumber(cityPlan.environment.minimum_green_ratio) || cityPlan.environment.minimum_green_ratio < 0 || cityPlan.environment.minimum_green_ratio > 1) errors.push("Environment is invalid.");

  const sectorIds = new Set();
  let capacity = 0;
  let greenArea = 0;
  if (Array.isArray(cityPlan.sectors)) {
    for (const sector of cityPlan.sectors) {
      if (!sector || typeof sector.id !== "string" || sectorIds.has(sector.id) || !SECTOR_TYPES.has(sector.type) || !validBounds(sector.bounds)) errors.push(`Invalid sector: ${sector?.id || "unknown"}.`);
      else sectorIds.add(sector.id);
      if (Number.isInteger(sector?.population_target)) capacity += sector.population_target;
      if (sector?.type === "green" && validBounds(sector.bounds)) greenArea += sector.bounds[2] * sector.bounds[3];
    }
  }

  const graph = cityPlan.road_graph;
  const nodeIds = new Set(graph?.nodes?.map((node) => node.id));
  if (!graph || !Array.isArray(graph.nodes) || !Array.isArray(graph.edges) || graph.nodes.length === 0) errors.push("Road graph is invalid.");
  if (graph?.edges?.some((edge) => !edge || !ROAD_TYPES.has(edge.type) || !nodeIds.has(edge.from) || !nodeIds.has(edge.to) || !isNumber(edge.width) || edge.width <= 0)) errors.push("Road graph contains invalid edges.");
  if (graph?.nodes?.some((node) => !node || typeof node.id !== "string" || !Array.isArray(node.position) || node.position.length !== 2 || !node.position.every(isNumber))) errors.push("Road graph contains invalid nodes.");

  if (Array.isArray(cityPlan.energy_zones)) {
    for (const zone of cityPlan.energy_zones) {
      if (!zone || !ENERGY_TYPES.has(zone.type) || !sectorIds.has(zone.sector) || !isNumber(zone.capacity) || zone.capacity < 0 || (zone.unit && !["MW", "MWh"].includes(zone.unit))) errors.push(`Invalid energy zone: ${zone?.id || "unknown"}.`);
    }
  }

  const dimensions = cityPlan.city?.dimensions || [0, 0];
  const greenRatio = (dimensions[0] * dimensions[1]) > 0 ? greenArea / (dimensions[0] * dimensions[1]) : 0;
  if (capacity < (cityPlan.city?.population_target || 0)) errors.push("Sector population capacity is below the target.");
  if (greenRatio < (cityPlan.environment?.minimum_green_ratio || 0)) errors.push("Minimum green-space ratio is not satisfied.");

  return { valid: errors.length === 0, errors };
};

module.exports = {
  validateCityPlan,
};