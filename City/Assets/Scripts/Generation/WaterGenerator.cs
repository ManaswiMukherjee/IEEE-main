// WaterGenerator.cs
// Generates water surface planes from the water supply data and any
// water-typed sectors. Visualises rivers, lakes, reservoirs, wetlands.

using System.Collections.Generic;
using UnityEngine;

public class WaterGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    private const float WaterOffset = 0.01f;

    public WaterGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(WaterData waterData, List<SectorData> sectors, CityData city)
    {
        Transform waterRoot = new GameObject("Water").transform;
        waterRoot.SetParent(_parent);

        int count = 0;
        if (sectors != null)
        {
            foreach (var sector in sectors)
            {
                if (sector.type != "water") continue;
                if (sector.geometry?.bounds == null || sector.geometry.bounds.Length < 4) continue;

                CreateWaterBody(sector.id, sector.geometry.bounds[0], sector.geometry.bounds[1],
                    sector.geometry.bounds[2], sector.geometry.bounds[3], waterRoot);
                count++;
            }
        }

        Debug.Log($"[Water] Generated water systems ({count} water bodies).");
    }

    /// <summary>Create a water plane at the given bounds.</summary>
    public void CreateWaterBody(string id, float x, float z, float width, float depth, Transform parent)
    {
        float centerX = x + width * 0.5f;
        float centerZ = z + depth * 0.5f;
        float terrainH = _terrainGen != null ? _terrainGen.SampleHeight(centerX, centerZ) : 0f;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Water_{id}";
        go.transform.SetParent(parent);
        go.transform.position   = new Vector3(centerX, terrainH + WaterOffset, centerZ);
        go.transform.localScale = new Vector3(width, 0.05f, depth);
        go.GetComponent<Renderer>().material = _mats.Water;
        UnityEngine.Object.Destroy(go.GetComponent<BoxCollider>());
    }
}
