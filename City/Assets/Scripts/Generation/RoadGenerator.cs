
using System.Collections.Generic;
using UnityEngine;

public class RoadGenerator
{
    private readonly Transform _parent;
    private readonly CityMaterials _mats;
    private readonly TerrainGenerator _terrainGen;

    private Transform _roadsRoot;
    private Transform _arterialsRoot;
    private Transform _collectorsRoot;
    private Transform _localRoot;
    private Transform _pedestrianRoot;
    private Transform _cycleRoot;

    private const float RoadOffset = 0.025f;
    private const float MinimumSegmentLength = 0.1f;

    public RoadGenerator(
        Transform parent,
        CityMaterials mats,
        TerrainGenerator terrainGen = null)
    {
        _parent = parent;
        _mats = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(RoadGraphData roadGraph, CityData city)
    {
        if (roadGraph == null)
        {
            Debug.LogWarning("[Roads] No road_graph defined.");
            return;
        }

        CreateHierarchy();

        var nodePositions = new Dictionary<string, Vector2>();

        foreach (var node in roadGraph.nodes)
        {
            nodePositions[node.id] =
                new Vector2(node.position.x, node.position.z);
        }

        int count = 0;

        foreach (var edge in roadGraph.edges)
        {
            if (!nodePositions.TryGetValue(edge.from, out var start))
                continue;

            if (!nodePositions.TryGetValue(edge.to, out var end))
                continue;

            string routing = edge.geometry?.routing ?? "straight";

            GenerateEdge(edge, start, end, routing);
            count++;
        }

        Debug.Log($"[Roads] Generated {count} road edges.");
    }

    private void CreateHierarchy()
    {
        _roadsRoot = MakeChild(_parent, "Roads");
        _arterialsRoot = MakeChild(_roadsRoot, "Arterials");
        _collectorsRoot = MakeChild(_roadsRoot, "Collectors");
        _localRoot = MakeChild(_roadsRoot, "Local");
        _pedestrianRoot = MakeChild(_roadsRoot, "Pedestrian");
        _cycleRoot = MakeChild(_roadsRoot, "Cycle");
    }

    private void GenerateEdge(
        RoadEdgeData edge,
        Vector2 start,
        Vector2 end,
        string routing)
    {
        Transform parent = ParentForType(edge.type);
        Material mat = MaterialForType(edge.type);
        float width = edge.width > 0f ? edge.width : 8f;

        if (mat == null)
        {
            Debug.LogWarning(
                $"[Roads] No material assigned for road type '{edge.type}'.");
            return;
        }

        if (routing == "orthogonal")
        {
            Vector2 corner = new Vector2(end.x, start.y);

            CreateRoadStrip(
                $"Road_{edge.id}_H",
                start, corner, width, parent, mat);

            CreateRoadStrip(
                $"Road_{edge.id}_V",
                corner, end, width, parent, mat);
        }
        else if (routing == "custom"
                 && edge.geometry?.waypoints != null
                 && edge.geometry.waypoints.Count >= 2)
        {
            var waypoints = edge.geometry.waypoints;

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector2 a = new Vector2(
                    waypoints[i].x, waypoints[i].z);

                Vector2 b = new Vector2(
                    waypoints[i + 1].x,
                    waypoints[i + 1].z);

                CreateRoadStrip(
                    $"Road_{edge.id}_{i}",
                    a, b, width, parent, mat);
            }
        }
        else
        {
            CreateRoadStrip(
                $"Road_{edge.id}",
                start, end, width, parent, mat);
        }
    }

    private void CreateRoadStrip(
        string name,
        Vector2 start2D,
        Vector2 end2D,
        float width,
        Transform parent,
        Material material)
    {
        Vector3 start = new Vector3(
            start2D.x,
            GetRoadSurfaceY(start2D.x, start2D.y),
            start2D.y);

        Vector3 end = new Vector3(
            end2D.x,
            GetRoadSurfaceY(end2D.x, end2D.y),
            end2D.y);

        Vector3 direction = end - start;
        direction.y = 0f;

        float length = direction.magnitude;

        if (length < MinimumSegmentLength)
            return;

        direction.Normalize();

        // Horizontal vector perpendicular to the road direction.
        Vector3 right = Vector3.Cross(
            Vector3.up, direction).normalized;

        float halfWidth = width * 0.5f;

        // Extend slightly to reduce hairline gaps at connections.
        const float endOverlap = 0.15f;

        Vector3 extendedStart = start - direction * endOverlap;
        Vector3 extendedEnd = end + direction * endOverlap;

        Vector3 midpoint = (extendedStart + extendedEnd) * 0.5f;

        Vector3 leftStart =
            extendedStart - right * halfWidth;

        Vector3 rightStart =
            extendedStart + right * halfWidth;

        Vector3 leftEnd =
            extendedEnd - right * halfWidth;

        Vector3 rightEnd =
            extendedEnd + right * halfWidth;

        // Build a thin, flat road surface rather than a cube.
        Vector3[] vertices =
        {
            leftStart - midpoint,
            rightStart - midpoint,
            leftEnd - midpoint,
            rightEnd - midpoint
        };

        int[] triangles =
        {
            0, 2, 1,
            1, 2, 3
        };

        Vector2[] uv =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, length / width),
            new Vector2(1f, length / width)
        };

        Mesh mesh = new Mesh
        {
            name = $"{name}_Mesh",
            vertices = vertices,
            triangles = triangles,
            uv = uv
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject road = new GameObject(name);
        road.transform.SetParent(parent, false);
        road.transform.position = midpoint;
        road.transform.rotation = Quaternion.identity;
        road.transform.localScale = Vector3.one;

        var filter = road.AddComponent<MeshFilter>();
        var renderer = road.AddComponent<MeshRenderer>();

        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;

        // No collider: roads are visual surfaces for now.
    }

    private float GetRoadSurfaceY(float x, float z)
    {
        float terrainHeight = _terrainGen != null
            ? _terrainGen.SampleHeight(x, z)
            : 0f;

        return terrainHeight + RoadOffset;
    }

    private Transform ParentForType(string type) => type switch
    {
        "arterial" => _arterialsRoot,
        "collector" => _collectorsRoot,
        "pedestrian" => _pedestrianRoot,
        "cycle" => _cycleRoot,
        _ => _localRoot
    };

    private Material MaterialForType(string type) => type switch
    {
        "pedestrian" => _mats?.Sidewalk,
        "cycle" => _mats?.Sidewalk,
        _ => _mats?.Road
    };

    private static Transform MakeChild(
        Transform parent,
        string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }
}
