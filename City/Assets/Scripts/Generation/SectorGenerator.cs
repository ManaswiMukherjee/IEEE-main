
using System.Collections.Generic;
using UnityEngine;

public class SectorGenerator
{
    private readonly Transform _parent;
    private readonly CityMaterials _mats;
    private readonly TerrainGenerator _terrainGen;

    private Transform _sectorRoot;

    public SectorGenerator(
        Transform parent,
        CityMaterials mats,
        TerrainGenerator terrainGen = null)
    {
        _parent = parent;
        _mats = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(List<SectorData> sectors, CityData city)
    {
        if (sectors == null || sectors.Count == 0)
        {
            Debug.LogWarning("[Sectors] No sectors defined.");
            return;
        }

        // Keep the sector hierarchy for scene organization,
        // but do not render colored ground overlays.
        _sectorRoot = new GameObject("Sectors").transform;
        _sectorRoot.SetParent(_parent, false);

        foreach (var sector in sectors)
        {
            if (sector.geometry?.bounds == null ||
                sector.geometry.bounds.Length < 4)
            {
                continue;
            }

            // Empty marker object: no mesh, no renderer, no collider.
            GameObject sectorMarker =
                new GameObject($"Sector_{sector.id}");

            sectorMarker.transform.SetParent(_sectorRoot, false);

            float x = sector.geometry.bounds[0];
            float z = sector.geometry.bounds[1];

            sectorMarker.transform.localPosition =
                new Vector3(x, 0f, z);
        }

        Debug.Log(
            $"[Sectors] Registered { _sectorRoot.childCount } sectors; " +
            "ground overlays disabled.");
    }
}
