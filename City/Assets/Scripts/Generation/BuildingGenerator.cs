// BuildingGenerator.cs
// Places buildings inside generated blocks, following the sector's building_rules
// and the global buildings archetypes. Uses plot-based subdivision per block.
// Optimized for fast generation, low memory, and clean aesthetics.

using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    private Transform _buildingsRoot;

    // Rooftop detail materials
    private Material _roofParapetMat;
    private Material _pentMat;

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
        _buildingsRoot.SetParent(_parent, false);

        CreateSharedMaterials();

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

        BuildingSpacingData    globalSpacing = buildingsData?.spacing    ?? new BuildingSpacingData();
        BuildingGenerationData globalGen     = buildingsData?.generation ?? new BuildingGenerationData();
        int maxPerBlock = globalGen.maximum_buildings_per_block > 0
            ? Mathf.Min(globalGen.maximum_buildings_per_block, 12) : 10;

        var rng           = new System.Random(seed);
        int totalBuilt    = 0;
        int buildingIndex = 0;

        foreach (var block in blocks)
        {
            if (IsNonDevelopable(block.sectorType)) continue;

            sectorMap.TryGetValue(block.sectorId, out SectorData sector);
            BuildingRulesData rules = sector?.building_rules ?? DefaultRules();

            List<BuildingArchetypeData> allowed = ResolveAllowedArchetypes(
                rules.allowed_archetypes, archetypes, block.sectorType);
            if (allowed.Count == 0) continue;

            float frontSetback = Mathf.Max(rules.spacing?.front_setback ?? 3f, globalSpacing.minimum_front_setback);
            float sideSetback  = Mathf.Max(rules.spacing?.side_setback  ?? 2f, globalSpacing.minimum_side_setback);
            float rearSetback  = Mathf.Max(rules.spacing?.rear_setback  ?? 3f, globalSpacing.minimum_rear_setback);
            float gap          = Mathf.Max(rules.spacing?.building_gap  ?? 4f, globalSpacing.minimum_building_gap);

            var archetype = allowed[rng.Next(allowed.Count)];
            float plotW = Lerp(archetype.footprint.minimum_width, archetype.footprint.maximum_width, 0.5f);
            float plotD = Lerp(archetype.footprint.minimum_depth, archetype.footprint.maximum_depth, 0.5f);
            plotW = Mathf.Clamp(plotW, 10f, block.width  - sideSetback * 2f);
            plotD = Mathf.Clamp(plotD, 10f, block.depth  - frontSetback - rearSetback);

            float maxCoverage = rules.coverage?.maximum ?? 0.6f;
            float innerW = block.width  - sideSetback * 2f;
            float innerD = block.depth  - frontSetback - rearSetback;
            if (innerW <= 0 || innerD <= 0) continue;

            int colCount = Mathf.Max(1, Mathf.FloorToInt(innerW / (plotW + gap)));
            int rowCount = Mathf.Max(1, Mathf.FloorToInt(innerD / (plotD + gap)));
            int buildingsInBlock  = 0;
            float totalBuildingArea = 0f;
            float blockArea       = block.width * block.depth;
            float maxBuildingArea = blockArea * maxCoverage;

            for (int row = 0; row < rowCount && buildingsInBlock < maxPerBlock; row++)
            {
                for (int col = 0; col < colCount && buildingsInBlock < maxPerBlock; col++)
                {
                    var arch = allowed[rng.Next(allowed.Count)];

                    float availableW = plotW;
                    float availableD = plotD;
                    float minW = Mathf.Min(arch.footprint.minimum_width, availableW);
                    float minD = Mathf.Min(arch.footprint.minimum_depth, availableD);

                    float bw = Mathf.Clamp(
                        Lerp(arch.footprint.minimum_width, arch.footprint.maximum_width, (float)rng.NextDouble()),
                        minW, availableW);
                    float bd = Mathf.Clamp(
                        Lerp(arch.footprint.minimum_depth, arch.footprint.maximum_depth, (float)rng.NextDouble()),
                        minD, availableD);

                    int floors = rng.Next(
                        Mathf.Max(1, rules.height.minimum_floors),
                        Mathf.Max(rules.height.minimum_floors + 1, rules.height.maximum_floors + 1));
                    float floorH = arch.height.floor_height > 0 ? arch.height.floor_height : 3.5f;
                    float height = floors * floorH;

                    float px = block.x + sideSetback  + col * (plotW + gap) + bw * 0.5f;
                    float pz = block.z + frontSetback + row * (plotD + gap) + bd * 0.5f;

                    float buildingArea = bw * bd;
                    if (blockArea <= 0f || totalBuildingArea + buildingArea > maxBuildingArea)
                        continue;

                    PlaceBuilding(buildingIndex++, arch, px, height, pz, bw, bd,
                        block.sectorType, sector, floors, rng);

                    buildingsInBlock++;
                    totalBuildingArea += buildingArea;
                    totalBuilt++;
                }
            }
        }

        Debug.Log($"[Buildings] Generated {totalBuilt} buildings.");
    }

    // ── Material setup ────────────────────────────────────────────────────────

    private void CreateSharedMaterials()
    {
        _roofParapetMat = MakeFlat("Roof_Parapet", new Color(0.28f, 0.30f, 0.33f));
        _pentMat        = MakeFlat("Penthouse",    new Color(0.38f, 0.40f, 0.45f));
    }

    private static Material MakeFlat(string name, Color col)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh) { name = name };
        m.enableInstancing = true;
        if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Color"))      m.SetColor("_Color", col);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
        return m;
    }

    // ── Building placement ────────────────────────────────────────────────────

    private void PlaceBuilding(
        int index, BuildingArchetypeData arch,
        float px, float height, float pz,
        float bw, float bd,
        string sectorType, SectorData sector,
        int floors, System.Random rng)
    {
        float terrainHeight = _terrainGen != null ? _terrainGen.SampleHeight(px, pz) : 0f;
        float centerY = terrainHeight + height * 0.5f;
        float roofY   = terrainHeight + height;

        // Main building volume
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Building_{index}_{arch.id}";
        go.transform.SetParent(_buildingsRoot, false);
        go.transform.position   = new Vector3(px, centerY, pz);
        go.transform.localScale = new Vector3(bw, height, bd);

        Renderer r = go.GetComponent<Renderer>();
        r.sharedMaterial    = FacadeMaterial(arch.type, sectorType);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows    = false;
        UnityEngine.Object.Destroy(go.GetComponent<BoxCollider>());

        // Rooftop feature for visual interest (clean, single roof element for mid/high rises)
        if (floors >= 4)
        {
            float pentW = bw * 0.5f;
            float pentD = bd * 0.5f;
            float pentH = Mathf.Clamp(height * 0.12f, 1.5f, 4f);

            GameObject pent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pent.name = "Penthouse";
            pent.transform.SetParent(_buildingsRoot, false);
            pent.transform.position   = new Vector3(px, roofY + pentH * 0.5f, pz);
            pent.transform.localScale = new Vector3(pentW, pentH, pentD);

            Renderer pentR = pent.GetComponent<Renderer>();
            pentR.sharedMaterial    = _pentMat;
            pentR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pentR.receiveShadows    = false;
            UnityEngine.Object.Destroy(pent.GetComponent<BoxCollider>());
        }
    }

    // ── Material selection ────────────────────────────────────────────────────

    private Material FacadeMaterial(string archType, string sectorType) =>
        archType switch
        {
            "industrial" or "warehouse"                          => _mats.Industrial,
            "retail" or "office" or "tower" or "mixed_use"      => _mats.Commercial,
            "school" or "hospital" or "government" or "community" => _mats.Commercial,
            "parking" or "utility"                               => _mats.Industrial,
            _                                                    => _mats.Building,
        };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsNonDevelopable(string type) => type is
        "park" or "forest" or "wetland" or "water" or "agriculture" or "energy";

    private static List<BuildingArchetypeData> ResolveAllowedArchetypes(
        List<string> allowedIds,
        Dictionary<string, BuildingArchetypeData> archetypes,
        string sectorType)
    {
        var result = new List<BuildingArchetypeData>();

        if (allowedIds != null)
            foreach (var id in allowedIds)
                if (archetypes.TryGetValue(id, out var arch))
                    result.Add(arch);

        if (result.Count == 0)
            foreach (var kv in archetypes)
                if (ArchetypeMatchesSector(kv.Value.type, sectorType))
                    result.Add(kv.Value);

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

    private static BuildingRulesData DefaultRules() => new BuildingRulesData
    {
        height    = new BuildingHeightRulesData    { minimum_floors = 1, maximum_floors = 4 },
        footprint = new BuildingFootprintRulesData { minimum_width = 10f, maximum_width = 24f, minimum_depth = 10f, maximum_depth = 20f },
        spacing   = new BuildingSpacingRulesData   { building_gap = 5f, front_setback = 3f, side_setback = 3f, rear_setback = 3f },
        coverage  = new BuildingCoverageRulesData  { maximum = 0.5f },
    };

    private static float Lerp(float a, float b, float t) => a + (b - a) * Mathf.Clamp01(t);
}
