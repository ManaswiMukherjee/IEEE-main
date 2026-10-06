const EnergyLog = {
  collectionName: "energy_logs",

  fields: {
    timestamp: "date",
    solar: "number",
    wind: "number",
    battery: "number",
    grid: "number",
    demand: "number",
    selected_source: "string",
  },
};

module.exports = EnergyLog;