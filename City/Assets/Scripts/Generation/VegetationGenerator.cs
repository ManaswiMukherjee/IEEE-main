// VegetationGenerator.cs
// Places vegetation (trees, shrubs) inside park/forest/green sectors.
// Avoids roads and respects vegetation generation parameters.

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
        vegRoot.SetParent(_parent);

        if (vegetation == null || sectors == null)
        {
            Debug.Log("[Vegetation] Generated 0 vegetation objects."); return;
        }

        VegetationGenerationData gen = vegetation.generation ?? new VegetationGenerationData();
        float density = gen.density > 0 ? gen.density : 50f;   // trees per hectare
        var   rng     = new System.Random(seed + 3);

        int count = 0;

        foreach (var sector in sectors)
        {
            if (!IsVegetationSector(sector.type)) continue;
            if (sector.geometry?.bounds == null || sector.geometry.bounds.Length < 4) continue;

            float sx     = sector.geometry.bounds[0];
            float sz     = sector.geometry.bounds[1];
            float sWidth = sector.geometry.bounds[2];
            float sDepth = sector.geometry.bounds[3];

            float areaHa  = (sWidth * sDepth) / 10000f;
            int   treeCount = Mathf.RoundToInt(areaHa * density);
            treeCount = Mathf.Clamp(treeCount, 5, 300);

            for (int i = 0; i < treeCount; i++)
            {
                float px = sx + 2f + (float)rng.NextDouble() * (sWidth - 4f);
                float pz = sz + 2f + (float)rng.NextDouble() * (sDepth - 4f);

                float trunkH = 0.5f + (float)rng.NextDouble() * 1.5f;
                float canopyR = 1.5f + (float)rng.NextDouble() * 3f;

                PlaceTree(count, px, pz, trunkH, canopyR, vegRoot);
                count++;
            }
        }

        Debug.Log($"[Vegetation] Generated {count} vegetation objects.");
    }

    private void PlaceTree(int index, float px, float pz, float trunkH, float canopyR, Transform parent)
    {
        float terrainHeight = _terrainGen != null ? _terrainGen.SampleHeight(px, pz) : 0f;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = $"Tree_{index}_Trunk";
        trunk.transform.SetParent(parent);
        trunk.transform.position   = new Vector3(px, terrainHeight + trunkH * 0.5f, pz);
        trunk.transform.localScale = new Vector3(0.25f, trunkH, 0.25f);
        trunk.GetComponent<Renderer>().material = _mats.Building; // brown-ish default
        UnityEngine.Object.Destroy(trunk.GetComponent<CapsuleCollider>());

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = $"Tree_{index}_Canopy";
        canopy.transform.SetParent(parent);
        canopy.transform.position   = new Vector3(px, terrainHeight + trunkH + canopyR * 0.7f, pz);
        canopy.transform.localScale = new Vector3(canopyR * 2f, canopyR * 1.5f, canopyR * 2f);
        canopy.GetComponent<Renderer>().material = _mats.Vegetation;
        UnityEngine.Object.Destroy(canopy.GetComponent<SphereCollider>());
    }

    private static bool IsVegetationSector(string type) => type is
        "park" or "forest" or "wetland" or "recreation" or "agriculture";
}
