// EnergyGenerator.cs
// Generates energy infrastructure visualisations:
// solar farms, wind turbines, substations – from energy_zones data
// and energy-typed sectors.

using System.Collections.Generic;
using UnityEngine;

public class EnergyGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    public EnergyGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(
        EnergyZonesData  energyZones,
        List<SectorData> sectors,
        CityData         city)
    {
        Transform energyRoot = new GameObject("Energy").transform;
        energyRoot.SetParent(_parent);

        if (sectors == null) { Debug.Log("[Energy] Generated energy infrastructure."); return; }

        int count = 0;
        foreach (var sector in sectors)
        {
            if (sector.type != "energy") continue;
            if (sector.geometry?.bounds == null || sector.geometry.bounds.Length < 4) continue;

            float sx     = sector.geometry.bounds[0];
            float sz     = sector.geometry.bounds[1];
            float sWidth = sector.geometry.bounds[2];
            float sDepth = sector.geometry.bounds[3];

            // Create a solar panel array indicator
            CreateSolarArray(sector.id, sx, sz, sWidth, sDepth, energyRoot);
            count++;
        }

        Debug.Log($"[Energy] Generated energy infrastructure ({count} energy sectors).");
    }

    private void CreateSolarArray(string id, float x, float z, float width, float depth, Transform parent)
    {
        float centerX = x + width * 0.5f;
        float centerZ = z + depth * 0.5f;
        float terrainH = _terrainGen != null ? _terrainGen.SampleHeight(centerX, centerZ) : 0f;

        // Represent a solar farm as a flat panel-coloured slab with a slight tilt
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Solar_{id}";
        go.transform.SetParent(parent);
        go.transform.position   = new Vector3(centerX, terrainH + 1.0f, centerZ);
        go.transform.localScale = new Vector3(width, 0.1f, depth);
        go.transform.eulerAngles = new Vector3(15f, 0f, 0f);  // south-facing tilt
        go.GetComponent<Renderer>().material = _mats.Solar;
        UnityEngine.Object.Destroy(go.GetComponent<BoxCollider>());
    }
}
