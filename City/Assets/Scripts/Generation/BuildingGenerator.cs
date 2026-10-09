// BuildingGenerator.cs
// Places buildings inside generated blocks, following the sector's building_rules
// and the global buildings archetypes. Uses plot-based subdivision per block.

using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    private Transform _buildingsRoot;

    public BuildingGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(
        List<GeneratedBlock>      blocks,
        BuildingsData             buildingsData,
        List<SectorData>          sectors,
        CityData                  city,
        int                       seed)
    {
        _buildingsRoot = new GameObject("Buildings").transform;
        _buildingsRoot.SetParent(_parent);

        if (blocks == null || blocks.Count == 0)
        {
            Debug.LogWarning("[Buildings] No blocks to place buildings in."); return;
        }

        // Build archetype lookup
        var archetypes = new Dictionary<string, BuildingArchetypeData>();
        if (buildingsData?.archetypes != null)
            foreach (var a in buildingsData.archetypes)
                archetypes[a.id] = a;

        // Build sector lookup
        var sectorMap = new Dictionary<string, SectorData>();
        if (sectors != null)
            foreach (var s in sectors)
                sectorMap[s.id] = s;

        BuildingSpacingData  globalSpacing  = buildingsData?.spacing      ?? new BuildingSpacingData();
        BuildingGenerationData globalGen     = buildingsData?.generation   ?? new BuildingGenerationData();
        int maxPerBlock = globalGen.maximum_buildings_per_block > 0
            ? globalGen.maximum_buildings_per_block : 20;

        var rng          = new System.Random(seed);
        int totalBuilt   = 0;
        int buildingIndex = 0;

        foreach (var block in blocks)
        {
            // Skip non-developable sector types
            if (IsNonDevelopable(block.sectorType)) continue;

            // Get sector rules
            sectorMap.TryGetValue(block.sectorId, out SectorData sector);
            BuildingRulesData rules = sector?.building_rules ?? DefaultRules();

            // Pick archetypes allowed in this sector
            List<BuildingArchetypeData> allowed = ResolveAllowedArchetypes(
                rules.allowed_archetypes, archetypes, block.sectorType);
            if (allowed.Count == 0) continue;

            // Subdivide block into plots
            float frontSetback = Mathf.Max(rules.spacing?.front_setback ?? 3f, globalSpacing.minimum_front_setback);
            float sideSetback  = Mathf.Max(rules.spacing?.side_setback  ?? 2f, globalSpacing.minimum_side_setback);
            float rearSetback  = Mathf.Max(rules.spacing?.rear_setback  ?? 3f, globalSpacing.minimum_rear_setback);
            float gap          = Mathf.Max(rules.spacing?.building_gap  ?? 4f, globalSpacing.minimum_building_gap);

            // Use preferred archetype footprint to determine plot grid
            var archetype = allowed[rng.Next(allowed.Count)];
            float plotW = Lerp(archetype.footprint.minimum_width, archetype.footprint.maximum_width, 0.5f);
            float plotD = Lerp(archetype.footprint.minimum_depth, archetype.footprint.maximum_depth, 0.5f);
            plotW = Mathf.Clamp(plotW, 6f, block.width  - sideSetback * 2f);
            plotD = Mathf.Clamp(plotD, 6f, block.depth  - frontSetback - rearSetback);

            // Coverage check
            float maxCoverage = rules.coverage?.maximum ?? 0.6f;

            float innerW = block.width  - sideSetback * 2f;
            float innerD = block.depth  - frontSetback - rearSetback;
            if (innerW <= 0 || innerD <= 0) continue;

            int colCount = Mathf.Max(1, Mathf.FloorToInt(innerW / (plotW + gap)));
            int rowCount = Mathf.Max(1, Mathf.FloorToInt(innerD / (plotD + gap)));
            int buildingsInBlock = 0;
            float totalBuildingArea = 0f;
            float blockArea = block.width * block.depth;
            float maxBuildingArea = blockArea * maxCoverage;

            for (int row = 0; row < rowCount && buildingsInBlock < maxPerBlock; row++)
            {
                for (int col = 0; col < colCount && buildingsInBlock < maxPerBlock; col++)
                {
                    // Pick archetype for this plot
                    var arch = allowed[rng.Next(allowed.Count)];


                    float availableW = plotW;
                    float availableD = plotD;

                    float minW = Mathf.Min(
                        arch.footprint.minimum_width, availableW);

                    float minD = Mathf.Min(
                        arch.footprint.minimum_depth, availableD);

                    float bw = Mathf.Clamp(
                        Lerp(
                            arch.footprint.minimum_width,
                            arch.footprint.maximum_width,
                            (float)rng.NextDouble()),
                        minW,
                        availableW);

                    float bd = Mathf.Clamp(
                        Lerp(
                            arch.footprint.minimum_depth,
                            arch.footprint.maximum_depth,
                            (float)rng.NextDouble()),
                        minD,
                        availableD);

                    int floors = rng.Next(
                        Mathf.Max(1, rules.height.minimum_floors),
                        Mathf.Max(rules.height.minimum_floors + 1, rules.height.maximum_floors + 1));
                    float floorH = arch.height.floor_height > 0 ? arch.height.floor_height : 3f;
                    float height = floors * floorH;

                    float px = block.x + sideSetback + col * (plotW + gap) + bw * 0.5f;
                    float pz = block.z + frontSetback + row * (plotD + gap) + bd * 0.5f;

                    // Check coverage won't be exceeded (approximation)
                    float buildingArea = bw * bd;

                    if (blockArea <= 0f ||
                        totalBuildingArea + buildingArea > maxBuildingArea)
                    {
                        continue;
                    }

                    PlaceBuilding(buildingIndex++, arch, px, height, pz, bw, bd,
                        block.sectorType, sector);
                    buildingsInBlock++;
                    totalBuildingArea += buildingArea;
                    totalBuilt++;
                }
            }
        }

        Debug.Log($"[Buildings] Generated {totalBuilt} buildings.");
    }

    // ─── Building placement ───────────────────────────────────────────────────

    private void PlaceBuilding(
        int index, BuildingArchetypeData arch,
        float px, float height, float pz,
        float bw, float bd,
        string sectorType, SectorData sector)
    {
        float terrainHeight = _terrainGen != null ? _terrainGen.SampleHeight(px, pz) : 0f;
        float centerY = terrainHeight + height * 0.5f;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Building_{index}_{arch.id}";
        go.transform.SetParent(_buildingsRoot);
        go.transform.position   = new Vector3(px, centerY, pz);
        go.transform.localScale = new Vector3(bw, height, bd);

        Renderer buildingRenderer = go.GetComponent<Renderer>();

        buildingRenderer.sharedMaterial =
            MaterialForArchetype(arch.type, sectorType);

        buildingRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        buildingRenderer.receiveShadows = false;
        UnityEngine.Object.Destroy(go.GetComponent<BoxCollider>());
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsNonDevelopable(string type) => type is
        "park" or "forest" or "wetland" or "water" or "agriculture" or "energy";

    private static List<BuildingArchetypeData> ResolveAllowedArchetypes(
        List<string> allowedIds,
        Dictionary<string, BuildingArchetypeData> archetypes,
        string sectorType)
    {
        var result = new List<BuildingArchetypeData>();

        if (allowedIds != null)
        {
            foreach (var id in allowedIds)
                if (archetypes.TryGetValue(id, out var arch))
                    result.Add(arch);
        }

        // Fallback: pick archetypes by sector type match
        if (result.Count == 0)
        {
            foreach (var kv in archetypes)
                if (ArchetypeMatchesSector(kv.Value.type, sectorType))
                    result.Add(kv.Value);
        }

        // Last resort: take all
        if (result.Count == 0)
            result.AddRange(archetypes.Values);

        return result;
    }

    private static bool ArchetypeMatchesSector(string archType, string sectorType) =>
        sectorType switch
        {
            "residential"               => archType is "house" or "apartment",
            "commercial" or "mixed_use" => archType is "retail" or "office" or "mixed_use" or "apartment",
            "office"                    => archType is "office" or "tower",
            "industrial" or "logistics" => archType is "industrial" or "warehouse",
            "civic"                     => archType is "government" or "community",
            "education"                 => archType is "school",
            "healthcare"                => archType is "hospital",
            _                           => true,
        };

    private Material MaterialForArchetype(string archType, string sectorType) =>
        archType switch
        {
            "industrial" or "warehouse" => _mats.Industrial,
            "retail" or "office"
                or "tower" or "mixed_use" => _mats.Commercial,
            "school" or "hospital"
                or "government"
                or "community"           => _mats.Commercial,
            "parking" or "utility"      => _mats.Industrial,
            _                           => _mats.Building,
        };

    private static BuildingRulesData DefaultRules() => new BuildingRulesData
    {
        height   = new BuildingHeightRulesData { minimum_floors = 1, maximum_floors = 4 },
        footprint = new BuildingFootprintRulesData { minimum_width = 8f, maximum_width = 20f, minimum_depth = 8f, maximum_depth = 16f },
        spacing  = new BuildingSpacingRulesData { building_gap = 4f, front_setback = 3f, side_setback = 2f, rear_setback = 3f },
        coverage = new BuildingCoverageRulesData { maximum = 0.5f },
    };

    private static float Lerp(float a, float b, float t) => a + (b - a) * Mathf.Clamp01(t);
}
