// SectorGenerator.cs
// Visualises sector boundaries as flat indicator planes.
// Sectors drive block/road generation – the visual here is just a ground overlay.

using System.Collections.Generic;
using UnityEngine;

public class SectorGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    private Transform _sectorRoot;

    public SectorGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(List<SectorData> sectors, CityData city)
    {
        if (sectors == null || sectors.Count == 0)
        {
            Debug.LogWarning("[Sectors] No sectors defined.");
            return;
        }

        _sectorRoot = new GameObject("Sectors").transform;
        _sectorRoot.SetParent(_parent);

        int count = 0;
        foreach (var sector in sectors)
        {
            if (sector.geometry?.bounds == null || sector.geometry.bounds.Length < 4) continue;

            float x     = sector.geometry.bounds[0];
            float z     = sector.geometry.bounds[1];
            float width = sector.geometry.bounds[2];
            float depth = sector.geometry.bounds[3];

            CreateSectorPlane(sector, x, z, width, depth);
            count++;
        }

        Debug.Log($"[Sectors] Generated {count} sectors.");
    }

    private void CreateSectorPlane(SectorData sector, float x, float z, float width, float depth)
    {
        // Subdivide sector plane into tiles (up to 100m) conforming to terrain height,
        // avoiding a single giant flat slab cutting through hilly terrain.
        const float tileSize = 100f;
        int xTiles = Mathf.Max(1, Mathf.CeilToInt(width / tileSize));
        int zTiles = Mathf.Max(1, Mathf.CeilToInt(depth / tileSize));

        Material mat = MaterialForSector(sector.type);
        GameObject sectorGroup = new GameObject($"Sector_{sector.id}");
        sectorGroup.transform.SetParent(_sectorRoot);

        for (int ix = 0; ix < xTiles; ix++)
        {
            float tileX = x + ix * (width / xTiles);
            float tileW = width / xTiles;

            for (int iz = 0; iz < zTiles; iz++)
            {
                float tileZ = z + iz * (depth / zTiles);
                float tileD = depth / zTiles;

                float centerX = tileX + tileW * 0.5f;
                float centerZ = tileZ + tileD * 0.5f;
                float baseY   = _terrainGen != null ? _terrainGen.SampleHeight(centerX, centerZ) : 0f;

                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Tile_{ix}_{iz}";
                tile.transform.SetParent(sectorGroup.transform);
                tile.transform.position   = new Vector3(centerX, baseY + 0.01f, centerZ);
                tile.transform.localScale = new Vector3(tileW, 0.02f, tileD);

                UnityEngine.Object.Destroy(tile.GetComponent<BoxCollider>());
                tile.GetComponent<Renderer>().material = mat;
            }
        }
    }

    private Material MaterialForSector(string type) => type switch
    {
        "park" or "forest" or "wetland" or "recreation" or "agriculture" => _mats.Grass,
        "commercial" or "mixed_use" or "office"                           => _mats.Commercial,
        "industrial" or "logistics"                                        => _mats.Industrial,
        "energy"                                                           => _mats.Energy,
        "transport"                                                        => _mats.Transport,
        "water"                                                            => _mats.Water,
        _                                                                  => _mats.Terrain,
    };
}
