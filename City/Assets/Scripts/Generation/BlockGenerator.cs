// BlockGenerator.cs
// Generates urban blocks by subdividing each sector with a local grid of roads.
// The road grid divides the sector into blocks; blocks are then the unit for plot
// subdivision and building placement.

using System.Collections.Generic;
using UnityEngine;

/// <summary>Lightweight representation of a generated urban block.</summary>
public struct GeneratedBlock
{
    public float  x, z;       // origin (world space)
    public float  width, depth;
    public string sectorId;
    public string sectorType;
}

public class BlockGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    public List<GeneratedBlock> GeneratedBlocks { get; private set; } = new List<GeneratedBlock>();

    private Transform _blocksRoot;
    private const float BlockY = 0.05f;  // slight lift above terrain

    public BlockGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(
        List<SectorData> sectors,
        RoadGraphData    roadGraph,
        BlocksData       blocks,
        CityData         city,
        int              seed)
    {
        _blocksRoot = new GameObject("Blocks").transform;
        _blocksRoot.SetParent(_parent);
        GeneratedBlocks.Clear();

        if (sectors == null) return;

        BlockSizeData      sizeRules = blocks?.size        ?? new BlockSizeData();
        BlockSubdivisionData subRules = blocks?.subdivision ?? new BlockSubdivisionData();

        int totalBlocks = 0;

        foreach (var sector in sectors)
        {
            // Skip non-developable sectors
            if (IsNonDevelopable(sector.type)) continue;
            if (sector.geometry?.bounds == null || sector.geometry.bounds.Length < 4) continue;

            float sx     = sector.geometry.bounds[0];
            float sz     = sector.geometry.bounds[1];
            float sWidth = sector.geometry.bounds[2];
            float sDepth = sector.geometry.bounds[3];

            // Determine block size from street_rules or global block config
            float blockW = GetBlockWidth(sector, sizeRules);
            float blockD = GetBlockDepth(sector, sizeRules);
            float roadW  = GetLocalRoadWidth(sector);

            // Generate grid of blocks inside this sector
            float curZ = sz + roadW;
            while (curZ + blockD < sz + sDepth - roadW)
            {
                float curX = sx + roadW;
                while (curX + blockW < sx + sWidth - roadW)
                {
                    var block = new GeneratedBlock
                    {
                        x = curX, z = curZ,
                        width = blockW, depth = blockD,
                        sectorId   = sector.id,
                        sectorType = sector.type,
                    };
                    GeneratedBlocks.Add(block);
                    CreateBlockVisual(block, sector);
                    totalBlocks++;

                    curX += blockW + roadW;
                }
                curZ += blockD + roadW;
            }
        }

        Debug.Log($"[Blocks] Generated {totalBlocks} blocks.");
    }

    private void CreateBlockVisual(GeneratedBlock block, SectorData sector)
    {
        float centerX = block.x + block.width * 0.5f;
        float centerZ = block.z + block.depth * 0.5f;
        float terrainHeight = _terrainGen != null ? _terrainGen.SampleHeight(centerX, centerZ) : 0f;

        // Blocks are visualised as a very thin slab slightly above the terrain
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Block_{block.sectorId}_{GeneratedBlocks.Count}";
        go.transform.SetParent(_blocksRoot);
        go.transform.position   = new Vector3(centerX, terrainHeight + BlockY, centerZ);
        go.transform.localScale = new Vector3(block.width, 0.04f, block.depth);
        go.GetComponent<Renderer>().material = _mats.Sidewalk;
        UnityEngine.Object.Destroy(go.GetComponent<BoxCollider>());
    }

    // ─── Parameter resolution ─────────────────────────────────────────────────

    private static bool IsNonDevelopable(string type) => type is
        "park" or "forest" or "wetland" or "water" or "agriculture";

    private static float GetBlockWidth(SectorData sector, BlockSizeData size)
    {
        if (sector.street_rules?.blocks != null)
        {
            float min = sector.street_rules.blocks.minimum_width;
            float max = sector.street_rules.blocks.maximum_width;
            return (min + max) * 0.5f;
        }
        float smin = size.minimum_width > 0 ? size.minimum_width : 60f;
        float smax = size.maximum_width > 0 ? size.maximum_width : 150f;
        return (smin + smax) * 0.5f;
    }

    private static float GetBlockDepth(SectorData sector, BlockSizeData size)
    {
        if (sector.street_rules?.blocks != null)
        {
            float min = sector.street_rules.blocks.minimum_depth;
            float max = sector.street_rules.blocks.maximum_depth;
            return (min + max) * 0.5f;
        }
        float smin = size.minimum_depth > 0 ? size.minimum_depth : 40f;
        float smax = size.maximum_depth > 0 ? size.maximum_depth : 100f;
        return (smin + smax) * 0.5f;
    }

    private static float GetLocalRoadWidth(SectorData sector)
    {
        return sector.street_rules?.local_roads?.width > 0
            ? sector.street_rules.local_roads.width
            : 8f;
    }
}
