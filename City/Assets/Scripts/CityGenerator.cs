using System;
using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour {
    [Header("City Data")]
    [SerializeField] private TextAsset cityPlan;

    [Header("World Scale")]
    [SerializeField] private float coordinateScale = 0.1f;

    [Header("Building Settings")]
    [SerializeField] private int maxBuildingsPerSector = 150;
    [SerializeField] private float buildingSpacing = 45f;

    private Transform cityRoot;
    private Material groundMaterial;
    private Material roadMaterial;
    private Material waterMaterial;
    private Material greenMaterial;
    private Material buildingMaterial;
    private Material commercialMaterial;
    private Material industrialMaterial;

    [Serializable]
    public class CityPlan {
        public City city;
        public TerrainData terrain;
        public Sector[] sectors;
        public RoadGraph road_graph;
        public EnergyZone[] energy_zones;
        public EnvironmentData environment;
    }

    [Serializable]
    public class City {
        public string name;
        public int seed;
        public float[] dimensions;
        public int population_target;
    }

    [Serializable]
    public class TerrainData {
        public string type;
        public float height_scale;
    }

    [Serializable]
    public class Sector {
        public string id;
        public string type;
        public float[] bounds;
        public float density;
        public float[] building_height;
        public int population_target;
    }

    [Serializable]
    public class RoadGraph {
        public RoadNode[] nodes;
        public RoadEdge[] edges;
    }

    [Serializable]
    public class RoadNode {
        public string id;
        public float[] position;
    }

    [Serializable]
    public class RoadEdge {
        public string id;
        public string from;
        public string to;
        public string type;
        public float width;
    }

    [Serializable]
    public class EnergyZone {
        public string id;
        public string type;
        public string sector;
        public float capacity;
        public string unit;
    }

    [Serializable]
    public class EnvironmentData {
        public float minimum_green_ratio;
        public WaterBody[] water_bodies;
    }

    [Serializable]
    public class WaterBody {
        public string id;
        public float[] bounds;
    }

    private void Start() {
        GenerateCity();
    }

    public void GenerateCity() {
        if (cityPlan == null) {
            Debug.LogError("CityGenerator: No city_plan JSON assigned.");
            return;
        }

        CityPlan plan;
        try {
            plan = JsonUtility.FromJson<CityPlan>(cityPlan.text);
        }
        catch (Exception e) {
            Debug.LogError($"Failed to parse city JSON: {e}");
            return;
        }

        if (plan == null || plan.city == null) {
            Debug.LogError("Invalid city plan.");
            return;
        }

        ClearCity();
        CreateMaterials();

        cityRoot = new GameObject("GeneratedCity").transform;
        GenerateGround(plan);
        GenerateWater(plan);
        GenerateSectors(plan);
        GenerateRoads(plan);
        GenerateBuildings(plan);

        Debug.Log($"Generated city: {plan.city.name} | " +
            $"Population: {plan.city.population_target}");
    }

    private void CreateMaterials() {
        groundMaterial = CreateMaterial(new Color(0.16f, 0.32f, 0.16f));
        roadMaterial = CreateMaterial(new Color(0.055f, 0.055f, 0.06f));
        waterMaterial = CreateMaterial(new Color(0.04f, 0.28f, 0.55f));
        greenMaterial = CreateMaterial(new Color(0.18f, 0.42f, 0.18f));
        buildingMaterial = CreateMaterial(new Color(0.65f, 0.67f, 0.70f));
        commercialMaterial = CreateMaterial(new Color(0.35f, 0.42f, 0.55f));
        industrialMaterial = CreateMaterial(new Color(0.42f, 0.38f, 0.32f));
    }

    private Material CreateMaterial(Color color) {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = color;

        return material;
    }

    private void GenerateGround(CityPlan plan) {
        float width = plan.city.dimensions[0] * coordinateScale;
        float depth = plan.city.dimensions[1] * coordinateScale;
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);

        ground.name = "Terrain";
        ground.transform.SetParent(cityRoot);
        ground.transform.position =
            new Vector3(width * 0.5f, -1f, depth * 0.5f);

        ground.transform.localScale = new Vector3(width, 2f, depth);
        ground.GetComponent<Renderer>().material = groundMaterial;
    }

    private void GenerateWater(CityPlan plan) {
        if (plan.environment == null ||
            plan.environment.water_bodies == null)
            return;

        foreach (WaterBody water in plan.environment.water_bodies) {
            if (water.bounds == null || water.bounds.Length < 4)
                continue;

            float x = water.bounds[0] * coordinateScale;
            float z = water.bounds[1] * coordinateScale;
            float width = water.bounds[2] * coordinateScale;
            float depth = water.bounds[3] * coordinateScale;

            GameObject waterObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

            waterObject.name = $"Water_{water.id}";
            waterObject.transform.SetParent(cityRoot);
            waterObject.transform.position =
                new Vector3(x + width * 0.5f, 0.15f, z + depth * 0.5f);
            waterObject.transform.localScale = new Vector3(width, 0.3f, depth);
            waterObject.GetComponent<Renderer>().material = waterMaterial;
        }
    }

    private void GenerateSectors(CityPlan plan) {
        if (plan.sectors == null)
            return;

        foreach (Sector sector in plan.sectors) {
            if (sector.bounds == null ||
                sector.bounds.Length < 4)
                continue;

            float x = sector.bounds[0] * coordinateScale;
            float z = sector.bounds[1] * coordinateScale;
            float width = sector.bounds[2] * coordinateScale;
            float depth = sector.bounds[3] * coordinateScale;

            if (sector.type == "green") {
                CreateSectorPlane(sector, x, z, width, depth);
            }
        }
    }

    private void CreateSectorPlane(
        Sector sector, float x, float z, float width, float depth) {
        GameObject area = GameObject.CreatePrimitive(PrimitiveType.Cube);
        area.name = $"Sector_{sector.id}";
        area.transform.SetParent(cityRoot);
        area.transform.position =
            new Vector3(x + width * 0.5f, 0.05f, z + depth * 0.5f);

        area.transform.localScale = new Vector3(width, 0.1f, depth);

        area.GetComponent<Renderer>().material = greenMaterial;
    }

    private void GenerateRoads(CityPlan plan) {
        if (plan.road_graph == null ||
            plan.road_graph.nodes == null ||
            plan.road_graph.edges == null)
            return;
        Dictionary<string, Vector3> nodes =
            new Dictionary<string, Vector3>();
        foreach (RoadNode node in plan.road_graph.nodes) {
            if (node.position == null ||
                node.position.Length < 2)
                continue;

            nodes[node.id] = new Vector3(
                node.position[0] * coordinateScale,
                0.12f, node.position[1] * coordinateScale);
        }

        foreach (RoadEdge edge in plan.road_graph.edges) {
            if (!nodes.ContainsKey(edge.from) ||
                !nodes.ContainsKey(edge.to))
                continue;
            CreateRoad(edge, nodes[edge.from], nodes[edge.to]);
        }
    }

    private void CreateRoad(RoadEdge edge, Vector3 start, Vector3 end) {
        Vector3 direction = end - start;
        float length = direction.magnitude;
        if (length < 0.01f)
            return;

        Vector3 midpoint = (start + end) * 0.5f;

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);

        road.name = $"Road_{edge.id}";
        road.transform.SetParent(cityRoot);
        road.transform.position = midpoint;
        road.transform.localScale =
            new Vector3(edge.width * coordinateScale, 0.12f, length);

        road.transform.rotation =
            Quaternion.LookRotation(direction);
        road.GetComponent<Renderer>().material =
            roadMaterial;
    }

    private void GenerateBuildings(CityPlan plan) {
        if (plan.sectors == null)
            return;
        System.Random random = new System.Random(plan.city.seed);

        foreach (Sector sector in plan.sectors) {
            if (sector.type == "green" ||
                sector.type == "energy" ||
                sector.type == "transport")
                continue;
            if (sector.bounds == null ||
                sector.bounds.Length < 4 ||
                sector.building_height == null ||
                sector.building_height.Length < 2)
                continue;

            float x = sector.bounds[0] * coordinateScale;
            float z = sector.bounds[1] * coordinateScale;
            float width = sector.bounds[2] * coordinateScale;
            float depth = sector.bounds[3] * coordinateScale;

            int targetCount = Mathf.RoundToInt(maxBuildingsPerSector * sector.density);

            targetCount = Mathf.Clamp(targetCount, 5, maxBuildingsPerSector);

            for (int i = 0; i < targetCount; i++) {
                float px =
                    x + 20f + (float)random.NextDouble() * Mathf.Max(1f, width - 40f);

                float pz =
                    z + 20f + (float)random.NextDouble() * Mathf.Max(1f, depth - 40f);

                float height =
                    Mathf.Lerp(
                        sector.building_height[0],
                        sector.building_height[1],
                        (float)random.NextDouble()
                    );
                height *= 2f;
                float buildingWidth =
                    Mathf.Lerp(8f, 18f, (float)random.NextDouble());

                GameObject building =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = $"Building_{sector.id}_{i}";
                building.transform.SetParent(cityRoot);
                building.transform.position =
                    new Vector3(px, height * 0.5f, pz);

                building.transform.localScale =
                    new Vector3(buildingWidth, height, buildingWidth);

                Material material = GetBuildingMaterial(sector.type);
                building.GetComponent<Renderer>().material = material;
            }
        }
    }

    private Material GetBuildingMaterial(string type) {
        switch (type) {
            case "commercial":
                return commercialMaterial;
            case "industrial":
                return industrialMaterial;
            default:
                return buildingMaterial;
        }
    }

    private void ClearCity() {
        Transform existing = transform.Find("GeneratedCity");
        if (existing != null) {
            DestroyImmediate(existing.gameObject);
        }
    }
}
