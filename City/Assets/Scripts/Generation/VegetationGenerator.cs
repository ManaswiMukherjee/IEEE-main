// VegetationGenerator.cs
// Places vegetation (trees, shrubs) inside park/forest/green sectors.
// Generates natural rounded trees (cylindrical trunks and spherical organic canopies)
// batched into unified meshes for maximum visual fidelity and 60+ FPS performance.

using System.Collections.Generic;
using UnityEngine;

public class VegetationGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    public VegetationGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(
        VegetationData    vegetation,
        List<SectorData>  sectors,
        CityData          city,
        int               seed)
    {
        Transform vegRoot = new GameObject("Vegetation").transform;
        vegRoot.SetParent(_parent, false);

        if (vegetation == null || sectors == null)
        {
            Debug.Log("[Vegetation] Generated 0 vegetation objects.");
            return;
        }

        VegetationGenerationData gen = vegetation.generation ?? new VegetationGenerationData();
        float density = gen.density > 0 ? gen.density : 50f;
        var rng = new System.Random(seed + 3);

        MeshBatcher trunkBatcher = new MeshBatcher();
        MeshBatcher canopyBatcher = new MeshBatcher();

        int count = 0;

        foreach (var sector in sectors)
        {
            if (!IsVegetationSector(sector.type)) continue;
            if (sector.geometry?.bounds == null || sector.geometry.bounds.Length < 4) continue;

            float sx     = sector.geometry.bounds[0];
            float sz     = sector.geometry.bounds[1];
            float sWidth = sector.geometry.bounds[2];
            float sDepth = sector.geometry.bounds[3];

            float areaHa    = (sWidth * sDepth) / 10000f;
            int   treeCount = Mathf.RoundToInt(areaHa * density);
            treeCount = Mathf.Clamp(treeCount, 15, 160);

            for (int i = 0; i < treeCount; i++)
            {
                float px = sx + 4f + (float)rng.NextDouble() * (sWidth - 8f);
                float pz = sz + 4f + (float)rng.NextDouble() * (sDepth - 8f);

                float trunkH  = 1.5f + (float)rng.NextDouble() * 1.5f;
                float trunkR  = 0.25f + (float)rng.NextDouble() * 0.15f;
                float canopyR = 2.2f + (float)rng.NextDouble() * 2.0f;

                float terrainHeight = _terrainGen != null ? _terrainGen.SampleHeight(px, pz) : 0f;

                // Natural rounded trunk with smooth normals
                trunkBatcher.AddCylinder(
                    new Vector3(px, terrainHeight, pz),
                    trunkR,
                    trunkH,
                    12);

                // Natural rounded organic canopy with smooth outward normals
                canopyBatcher.AddCanopy(
                    new Vector3(px, terrainHeight + trunkH + canopyR * 0.75f, pz),
                    new Vector3(canopyR, canopyR * 1.15f, canopyR),
                    8,
                    14);

                count++;
            }
        }

        trunkBatcher.BuildGameObject("Batched_Trunks", _mats?.Industrial ?? _mats.Building, vegRoot);
        canopyBatcher.BuildGameObject("Batched_Canopies", _mats?.Vegetation ?? _mats.Grass, vegRoot);

        Debug.Log($"[Vegetation] Generated {count} natural rounded trees batched into 2 unified meshes.");
    }

    private static bool IsVegetationSector(string type) => type is
        "park" or "forest" or "wetland" or "recreation" or "agriculture";
}
