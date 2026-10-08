// RoadGenerator.cs
// Generates road meshes from road_graph nodes and edges.
// For orthogonal routing, generates an L-shaped Manhattan-style route.
// For straight routing, generates a direct connection.
// Follows terrain elevation across each edge with subdivision to avoid clipping.

using System.Collections.Generic;
using UnityEngine;

public class RoadGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    // Hierarchy roots
    private Transform _roadsRoot;
    private Transform _arterialsRoot;
    private Transform _collectorsRoot;
    private Transform _localRoot;
    private Transform _pedestrianRoot;
    private Transform _cycleRoot;

    // Road height offset above terrain to avoid z-fighting
    private const float RoadOffset = 0.02f;

    public RoadGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(RoadGraphData roadGraph, CityData city)
    {
        if (roadGraph == null)
        {
            Debug.LogWarning("[Roads] No road_graph defined."); return;
        }

        CreateHierarchy();

        // Build node position lookup (storing world x, z)
        var nodePositions = new Dictionary<string, Vector2>();
        foreach (var node in roadGraph.nodes)
        {
            nodePositions[node.id] = new Vector2(node.position.x, node.position.z);
        }

        int count = 0;
        foreach (var edge in roadGraph.edges)
        {
            if (!nodePositions.TryGetValue(edge.from, out var start)) continue;
            if (!nodePositions.TryGetValue(edge.to,   out var end))   continue;

            string routing = edge.geometry?.routing ?? "straight";
            GenerateEdge(edge, start, end, routing);
            count++;
        }

        Debug.Log($"[Roads] Generated {count} road edges.");
    }

    // ─── Hierarchy creation ───────────────────────────────────────────────────

    private void CreateHierarchy()
    {
        _roadsRoot     = MakeChild(_parent,     "Roads");
        _arterialsRoot = MakeChild(_roadsRoot,  "Arterials");
        _collectorsRoot= MakeChild(_roadsRoot,  "Collectors");
        _localRoot     = MakeChild(_roadsRoot,  "Local");
        _pedestrianRoot= MakeChild(_roadsRoot,  "Pedestrian");
        _cycleRoot     = MakeChild(_roadsRoot,  "Cycle");
    }

    // ─── Edge generation ──────────────────────────────────────────────────────

    private void GenerateEdge(RoadEdgeData edge, Vector2 start, Vector2 end, string routing)
    {
        Transform parent = ParentForType(edge.type);
        Material  mat    = MaterialForType(edge.type);
        float     width  = edge.width > 0 ? edge.width : 8f;

        if (routing == "orthogonal")
        {
            // L-shaped route: go along X first, then Z
            Vector2 corner = new Vector2(end.x, start.y);
            SubdivideAndCreateSegments($"Road_{edge.id}_H", start, corner, width, parent, mat);
            SubdivideAndCreateSegments($"Road_{edge.id}_V", corner, end,   width, parent, mat);
        }
        else if (routing == "custom" && edge.geometry?.waypoints != null &&
                 edge.geometry.waypoints.Count >= 2)
        {
            var wpts = edge.geometry.waypoints;
            for (int i = 0; i < wpts.Count - 1; i++)
            {
                Vector2 a = new Vector2(wpts[i].x,   wpts[i].z);
                Vector2 b = new Vector2(wpts[i+1].x, wpts[i+1].z);
                SubdivideAndCreateSegments($"Road_{edge.id}_{i}", a, b, width, parent, mat);
            }
        }
        else
        {
            // Default: straight line
            SubdivideAndCreateSegments($"Road_{edge.id}", start, end, width, parent, mat);
        }
    }

    private void SubdivideAndCreateSegments(
        string prefix, Vector2 start2D, Vector2 end2D,
        float width, Transform parent, Material mat)
    {
        float dist = Vector2.Distance(start2D, end2D);
        if (dist < 0.1f) return;

        // Subdivide road into segments of at most ~30m so it clings closely to terrain slopes
        const float maxStep = 30f;
        int steps = Mathf.Max(1, Mathf.CeilToInt(dist / maxStep));

        Vector2 prevPoint2D = start2D;
        float prevY = GetRoadSurfaceY(prevPoint2D.x, prevPoint2D.y);
        Vector3 prevPos = new Vector3(prevPoint2D.x, prevY, prevPoint2D.y);

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 currPoint2D = Vector2.Lerp(start2D, end2D, t);
            float currY = GetRoadSurfaceY(currPoint2D.x, currPoint2D.y);
            Vector3 currPos = new Vector3(currPoint2D.x, currY, currPoint2D.y);

            string segName = steps == 1 ? prefix : $"{prefix}_sub_{i}";
            CreateSegment(segName, prevPos, currPos, width, parent, mat);

            prevPos = currPos;
        }
    }

    private float GetRoadSurfaceY(float x, float z)
    {
        float terrainH = _terrainGen != null ? _terrainGen.SampleHeight(x, z) : 0f;
        return terrainH + RoadOffset;
    }

    private static void CreateSegment(
        string name, Vector3 start, Vector3 end,
        float width, Transform parent, Material mat)
    {
        Vector3 dir = end - start;
        float length = dir.magnitude;
        if (length < 0.1f) return;

        Vector3 mid = (start + end) * 0.5f;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position   = mid;
        // Thin road slice (0.04m) sitting just atop terrain
        go.transform.localScale = new Vector3(width, 0.04f, length);
        go.transform.rotation   = Quaternion.LookRotation(dir.normalized);
        go.GetComponent<Renderer>().material = mat;
        UnityEngine.Object.Destroy(go.GetComponent<BoxCollider>());
    }

    private Transform ParentForType(string type) => type switch
    {
        "arterial"   => _arterialsRoot,
        "collector"  => _collectorsRoot,
        "pedestrian" => _pedestrianRoot,
        "cycle"      => _cycleRoot,
        _            => _localRoot,
    };

    private Material MaterialForType(string type) => type switch
    {
        "pedestrian" => _mats.Sidewalk,
        "cycle"      => _mats.Sidewalk,
        _            => _mats.Road,
    };

    private static Transform MakeChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.transform;
    }
}
