const assert = require("node:assert/strict");
const test = require("node:test");

const app = require("../App/server");

let server;
let baseUrl;

test.before(async () => {
  server = app.listen(0);
  await new Promise((resolve) => server.once("listening", resolve));
  baseUrl = `http://127.0.0.1:${server.address().port}`;
});

test.after(async () => {
  await new Promise((resolve, reject) => server.close((error) => (error ? reject(error) : resolve())));
});

const request = async (path, options) => {
  const response = await fetch(`${baseUrl}${path}`, {
    headers: { "content-type": "application/json" },
    ...options,
  });
  return { response, body: await response.json() };
};

test("health endpoint responds", async () => {
  const { response, body } = await request("/api/health");
  assert.equal(response.status, 200);
  assert.equal(body.success, true);
});

test("AI interpretation feeds city generation", async () => {
  const interpreted = await request("/api/ai/interpret", {
    method: "POST",
    body: JSON.stringify({ vision: "A city for 100,000 people with high walkability, solar and wind energy" }),
  });
  assert.equal(interpreted.response.status, 200);
  assert.equal(interpreted.body.data.requirements.population, 100000);

  const generated = await request("/api/city/generate", {
    method: "POST",
    body: JSON.stringify({ requirements: interpreted.body.data.requirements }),
  });
  assert.equal(generated.response.status, 200);
  assert.ok(generated.body.data.id);
  assert.equal(generated.body.data.cityPlan.city.population_target, 100000);
  assert.ok(generated.body.data.cityPlan.sectors.length > 0);

  const retrieved = await request(`/api/city/${generated.body.data.id}`);
  assert.equal(retrieved.response.status, 200);
  assert.equal(retrieved.body.data.cityPlan.city.name, "AI Sustainable City");
});

test("energy control validates input and weather returns normalized data", async () => {
  const status = await request("/api/energy/status");
  assert.equal(status.response.status, 200);
  assert.ok(status.body.data.energy.selected_source);

  const invalidControl = await request("/api/energy/control", {
    method: "POST",
    body: JSON.stringify({ source: "nuclear", value: 1 }),
  });
  assert.equal(invalidControl.response.status, 400);

  const weather = await request("/api/weather");
  assert.equal(weather.response.status, 200);
  assert.equal(typeof weather.body.data.solar_irradiance, "number");
});
