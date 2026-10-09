// RoadGenerator.cs
// Generates a realistic layered road network:
//   1. Renders all edges from the road_graph (arterials, collectors, locals, cycle, pedestrian)
//      each with: road body + sidewalks + curb strips + center markings + intersection pads.
//   2. Procedurally generates an orthogonal local-road grid inside every developable sector
//      using the sector's street_rules or sensible defaults.
//   3. Street trees along arterials and collectors.

using System.Collections.Generic;
using UnityEngine;

public class RoadGenerator
{
    // ── Scene hierarchy roots ─────────────────────────────────────────────────
    private Transform _roadsRoot;
    private Transform _arterialsRoot;
    private Transform _collectorsRoot;
    private Transform _localRoot;
    private Transform _pedestrianRoot;
    private Transform _cycleRoot;
    private Transform _treesRoot;

    // ── Shared dependencies ───────────────────────────────────────────────────
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    // ── Per-type materials ────────────────────────────────────────────────────
    private Material _matArterial;
    private Material _matCollector;
    private Material _matLocal;
    private Material _matCurb;
    private Material _matSidewalk;
    private Material _matCycleway;
    private Material _matPedestrian;
    private Material _matCenterLine;
    private Material _matIntersection;
    private Material _matTreeTrunk;
    private Material _matTreeCanopy;

    // ── Constants ─────────────────────────────────────────────────────────────
    private const float RoadY        = 0.02f;  // road surface lift above terrain
    private const float SidewalkY    = 0.04f;  // sidewalk slightly higher
    private const float CurbY        = 0.08f;  // curb higher still
    private const float CenterLineY  = 0.03f;  // dashes float on road
    private const float IntersectY   = 0.015f; // intersection pad flush

    private const float MinSegLen    = 0.5f;

    // Sidewalk and curb widths (metres, world space)
    private const float SidewalkWidth = 2.5f;
    private const float CurbWidth     = 0.3f;

    // Tree spacing along arterials / collectors
    private const float TreeSpacing       = 20f;
    private const float TreeMinSpacing    = 8f;
    private const float TreeTrunkRadius   = 0.18f;
    private const float TreeTrunkHeight   = 2.0f;
    private const float TreeCanopyRadius  = 2.5f;
    private const float TreeCanopyOffsetY = 3.0f;

    // ── State: collected node positions for procedural local grid ─────────────
    private Dictionary<string, Vector2> _nodePositions;

    // ─────────────────────────────────────────────────────────────────────────

    public RoadGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    // ── Public entry point ────────────────────────────────────────────────────

    public void Generate(RoadGraphData roadGraph, CityData city)
    {
        BuildMaterials();
        BuildHierarchy();

        _nodePositions = new Dictionary<string, Vector2>();

        if (roadGraph != null)
        {
            // Index node positions
            foreach (var node in roadGraph.nodes)
                _nodePositions[node.id] = new Vector2(node.position.x, node.position.z);

            // Render intersection pads first (drawn under roads)
            foreach (var node in roadGraph.nodes)
                RenderIntersectionPad(node, roadGraph.edges);

            // Render each edge with full cross-section
            int edgeCount = 0;
            foreach (var edge in roadGraph.edges)
            {
                if (!_nodePositions.TryGetValue(edge.from, out var a)) continue;
                if (!_nodePositions.TryGetValue(edge.to,   out var b)) continue;
                RenderEdge(edge, a, b);
                edgeCount++;
            }

            Debug.Log($"[Roads] Rendered {edgeCount} road_graph edges.");
        }
        else
        {
            Debug.LogWarning("[Roads] No road_graph defined – only procedural grid will be generated.");
        }

        Debug.Log("[Roads] Road generation complete.");
    }

    // ── Overload that also generates local grids per sector ──────────────────

    public void GenerateWithSectors(
        RoadGraphData      roadGraph,
        List<SectorData>   sectors,
        CityData           city)
    {
        Generate(roadGraph, city);

        if (sectors == null) return;

        int localCount = 0;
        foreach (var sector in sectors)
        {
            if (IsNonRoadSector(sector.type)) continue;
            if (sector.geometry?.bounds == null || sector.geometry.bounds.Length < 4) continue;

            localCount += GenerateLocalGrid(sector);
        }

        Debug.Log($"[Roads] Generated {localCount} local road segments across sectors.");
    }

    // ── Material factory ──────────────────────────────────────────────────────

    private void BuildMaterials()
    {
        _matArterial    = MakeRoad("Road_Arterial",    new Color(0.18f, 0.18f, 0.20f), 0.35f);
        _matCollector   = MakeRoad("Road_Collector",   new Color(0.20f, 0.20f, 0.22f), 0.30f);
        _matLocal       = MakeRoad("Road_Local",       new Color(0.22f, 0.22f, 0.24f), 0.20f);
        _matCurb        = MakeRoad("Road_Curb",        new Color(0.60f, 0.60f, 0.62f), 0.15f);
        _matSidewalk    = MakeRoad("Road_Sidewalk",    new Color(0.80f, 0.78f, 0.74f), 0.10f);
        _matCycleway    = MakeRoad("Road_Cycle",       new Color(0.62f, 0.78f, 0.62f), 0.20f);
        _matPedestrian  = MakeRoad("Road_Pedestrian",  new Color(0.88f, 0.84f, 0.78f), 0.10f);
        _matCenterLine  = MakeEmissive("Road_CenterLine", new Color(0.95f, 0.85f, 0.2f), new Color(0.4f, 0.35f, 0.0f));
        _matIntersection = MakeRoad("Road_Intersection", new Color(0.16f, 0.16f, 0.18f), 0.35f);
        _matTreeTrunk   = MakeRoad("Tree_Trunk",  new Color(0.38f, 0.26f, 0.14f), 0.1f);
        _matTreeCanopy  = MakeRoad("Tree_Canopy", new Color(0.18f, 0.48f, 0.18f), 0.1f);
    }

    private static Material MakeRoad(string name, Color color, float smoothness)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh) { name = name };
        m.enableInstancing = true;
        if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor",  color);
        if (m.HasProperty("_Color"))      m.SetColor("_Color",      color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    private static Material MakeEmissive(string name, Color color, Color emit)
    {
        var m = MakeRoad(name, color, 0.6f);
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emit);
        }
        return m;
    }

    // ── Scene hierarchy ───────────────────────────────────────────────────────

    private void BuildHierarchy()
    {
        _roadsRoot      = MakeGroup(_parent, "Roads");
        _arterialsRoot  = MakeGroup(_roadsRoot, "Arterials");
        _collectorsRoot = MakeGroup(_roadsRoot, "Collectors");
        _localRoot      = MakeGroup(_roadsRoot, "Local");
        _pedestrianRoot = MakeGroup(_roadsRoot, "Pedestrian");
        _cycleRoot      = MakeGroup(_roadsRoot, "Cycle");
        _treesRoot      = MakeGroup(_roadsRoot, "StreetTrees");
    }

    private static Transform MakeGroup(Transform parent, string name)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    // ── Intersection pad ──────────────────────────────────────────────────────
    // Fills the gap where roads meet with a square pad as wide as the widest
    // connecting road.

    private void RenderIntersectionPad(RoadNodeData node, List<RoadEdgeData> edges)
    {
        Vector2 pos = new Vector2(node.position.x, node.position.z);
        float maxWidth = 8f;

        foreach (var e in edges)
        {
            if ((e.from == node.id || e.to == node.id) && e.width > maxWidth)
                maxWidth = e.width;
        }

        // Include sidewalk + curb on each side
        float padSize = maxWidth + (SidewalkWidth + CurbWidth) * 2f;
        float y       = GetY(pos.x, pos.y) + IntersectY;

        var pad = QuadFlat($"Intersection_{node.id}", _matIntersection, _roadsRoot);
        pad.transform.position   = new Vector3(pos.x, y, pos.y);
        pad.transform.localScale = new Vector3(padSize, 1f, padSize);
    }

    // ── Full edge cross-section ───────────────────────────────────────────────

    private void RenderEdge(RoadEdgeData edge, Vector2 a, Vector2 b)
    {
        bool isArterial  = edge.type == "arterial";
        bool isCollector = edge.type == "collector";
        bool isCycle     = edge.type == "cycle";
        bool isPed       = edge.type == "pedestrian";

        float roadW = edge.width > 0f ? edge.width : DefaultWidth(edge.type);

        Transform roadParent = RootForType(edge.type);
        Material  roadMat    = MatForType(edge.type);

        string routing = edge.geometry?.routing ?? "straight";

        if (routing == "orthogonal")
        {
            Vector2 corner = new Vector2(b.x, a.y);
            RenderCrossSection(edge, a, corner, roadW, roadMat, roadParent, isArterial, isCollector, isCycle, isPed);
            RenderCrossSection(edge, corner, b, roadW, roadMat, roadParent, isArterial, isCollector, isCycle, isPed);
        }
        else if (routing == "custom" && edge.geometry?.waypoints != null && edge.geometry.waypoints.Count >= 2)
        {
            var wp = edge.geometry.waypoints;
            for (int i = 0; i < wp.Count - 1; i++)
            {
                var pa = new Vector2(wp[i].x, wp[i].z);
                var pb = new Vector2(wp[i + 1].x, wp[i + 1].z);
                RenderCrossSection(edge, pa, pb, roadW, roadMat, roadParent, isArterial, isCollector, isCycle, isPed);
            }
        }
        else
        {
            RenderCrossSection(edge, a, b, roadW, roadMat, roadParent, isArterial, isCollector, isCycle, isPed);
        }
    }

    private void RenderCrossSection(
        RoadEdgeData edge,
        Vector2 a2, Vector2 b2,
        float roadW,
        Material roadMat,
        Transform roadParent,
        bool isArterial, bool isCollector,
        bool isCycle,    bool isPed)
    {
        float len = Vector2.Distance(a2, b2);
        if (len < MinSegLen) return;

        string id  = edge.id;
        bool hasSidewalk = !isCycle && !isPed && (edge.sidewalk?.enabled ?? true);
        bool hasCycle    = !isCycle && !isPed && (edge.cycling?.enabled ?? false);

        // ── Road body ─────────────────────────────────────────────────────────
        BuildFlatQuad($"Road_{id}", a2, b2, roadW, GetY, RoadY, roadMat, roadParent);

        // ── Sidewalks + curbs on each side ────────────────────────────────────
        if (hasSidewalk)
        {
            float totalSide = SidewalkWidth + CurbWidth;

            // Left side (negative right)
            float leftCenter = roadW * 0.5f + CurbWidth * 0.5f;
            BuildFlatQuadOffset($"Curb_{id}_L", a2, b2, CurbWidth, -leftCenter, GetY, CurbY, _matCurb, roadParent);
            float leftSWCenter = roadW * 0.5f + CurbWidth + SidewalkWidth * 0.5f;
            BuildFlatQuadOffset($"SW_{id}_L", a2, b2, SidewalkWidth, -leftSWCenter, GetY, SidewalkY, _matSidewalk, roadParent);

            // Right side
            BuildFlatQuadOffset($"Curb_{id}_R", a2, b2, CurbWidth,     leftCenter,   GetY, CurbY,     _matCurb,     roadParent);
            BuildFlatQuadOffset($"SW_{id}_R",   a2, b2, SidewalkWidth, leftSWCenter, GetY, SidewalkY, _matSidewalk, roadParent);
        }

        // ── Cycle lane alongside sidewalk ─────────────────────────────────────
        if (hasCycle)
        {
            float cycleW      = edge.cycling.width > 0f ? edge.cycling.width : 1.5f;
            float totalOffset = roadW * 0.5f + CurbWidth + SidewalkWidth + cycleW * 0.5f;
            BuildFlatQuadOffset($"Cycle_{id}_L", a2, b2, cycleW, -totalOffset, GetY, SidewalkY + 0.005f, _matCycleway, roadParent);
            BuildFlatQuadOffset($"Cycle_{id}_R", a2, b2, cycleW,  totalOffset, GetY, SidewalkY + 0.005f, _matCycleway, roadParent);
        }

        // ── Center line markings (arterials/collectors only) ──────────────────
        if (isArterial || isCollector)
        {
            float dashLen     = isArterial ? 4f : 2.5f;
            float dashGap     = isArterial ? 6f : 4f;
            float dashW       = 0.25f;
            float markingY    = GetMidY(a2, b2) + CenterLineY;

            RenderDashedLine($"CenterLine_{id}", a2, b2, dashW, dashLen, dashGap, markingY, _matCenterLine, roadParent);
        }

        // ── Street trees along arterials/collectors ───────────────────────────
        if ((isArterial || isCollector) && (edge.street_trees?.enabled ?? false))
        {
            float treeOffset = roadW * 0.5f + CurbWidth + SidewalkWidth * 0.7f;
            SpawnStreetTrees($"Trees_{id}", a2, b2, treeOffset, TreeSpacing);
        }
    }

    // ── Procedural local road grid inside each sector ─────────────────────────

    private int GenerateLocalGrid(SectorData sector)
    {
        float[] bounds = sector.geometry.bounds;
        float sx     = bounds[0];
        float sz     = bounds[1];
        float sWidth = bounds[2];
        float sDepth = bounds[3];

        float roadW   = sector.street_rules?.local_roads?.width > 0
            ? sector.street_rules.local_roads.width : 8f;

        float spacingX = sector.street_rules?.blocks != null
            ? (sector.street_rules.blocks.minimum_width + sector.street_rules.blocks.maximum_width) * 0.5f
            : BlockSpacingForType(sector.type);

        float spacingZ = sector.street_rules?.blocks != null
            ? (sector.street_rules.blocks.minimum_depth + sector.street_rules.blocks.maximum_depth) * 0.5f
            : spacingX * 0.8f;

        spacingX = Mathf.Max(spacingX, 60f);
        spacingZ = Mathf.Max(spacingZ, 50f);

        bool hasSidewalk = true;
        bool hasTrees    = sector.street_rules?.street_trees?.enabled ?? false;
        float treeSpacing = sector.street_rules?.street_trees?.spacing > 0
            ? sector.street_rules.street_trees.spacing : TreeSpacing;

        int count = 0;
        string sid = sector.id;

        // ── E–W roads (constant Z lines) ─────────────────────────────────────
        float z = sz + spacingZ;
        int ri  = 0;
        while (z < sz + sDepth - spacingZ * 0.3f)
        {
            Vector2 a = new Vector2(sx, z);
            Vector2 b = new Vector2(sx + sWidth, z);
            BuildFlatQuad($"LR_{sid}_EW_{ri}", a, b, roadW, GetY, RoadY, _matLocal, _localRoot);

            if (hasSidewalk)
            {
                BuildFlatQuadOffset($"SW_{sid}_EW_{ri}_L", a, b, SidewalkWidth, -(roadW * 0.5f + SidewalkWidth * 0.5f), GetY, SidewalkY, _matSidewalk, _localRoot);
                BuildFlatQuadOffset($"SW_{sid}_EW_{ri}_R", a, b, SidewalkWidth,   roadW * 0.5f + SidewalkWidth * 0.5f,  GetY, SidewalkY, _matSidewalk, _localRoot);
            }

            if (hasTrees)
            {
                SpawnStreetTrees($"LT_{sid}_EW_{ri}", a, b, roadW * 0.5f + SidewalkWidth * 0.7f, treeSpacing);
            }

            z += spacingZ;
            ri++;
            count++;
        }

        // ── N–S roads (constant X lines) ─────────────────────────────────────
        float x = sx + spacingX;
        int ci  = 0;
        while (x < sx + sWidth - spacingX * 0.3f)
        {
            Vector2 a = new Vector2(x, sz);
            Vector2 b = new Vector2(x, sz + sDepth);
            BuildFlatQuad($"LR_{sid}_NS_{ci}", a, b, roadW, GetY, RoadY, _matLocal, _localRoot);

            if (hasSidewalk)
            {
                BuildFlatQuadOffset($"SW_{sid}_NS_{ci}_L", a, b, SidewalkWidth, -(roadW * 0.5f + SidewalkWidth * 0.5f), GetY, SidewalkY, _matSidewalk, _localRoot);
                BuildFlatQuadOffset($"SW_{sid}_NS_{ci}_R", a, b, SidewalkWidth,   roadW * 0.5f + SidewalkWidth * 0.5f,  GetY, SidewalkY, _matSidewalk, _localRoot);
            }

            if (hasTrees)
            {
                SpawnStreetTrees($"LT_{sid}_NS_{ci}", a, b, roadW * 0.5f + SidewalkWidth * 0.7f, treeSpacing);
            }

            x += spacingX;
            ci++;
            count++;
        }

        return count;
    }

    // ── Mesh builders ─────────────────────────────────────────────────────────

    /// Build a flat road quad between two 2D world points at a given lateral width.
    private void BuildFlatQuad(
        string   name,
        Vector2  a2, Vector2 b2,
        float    width,
        System.Func<float, float, float> getY,
        float    yOffset,
        Material mat,
        Transform parent)
    {
        BuildFlatQuadOffset(name, a2, b2, width, 0f, getY, yOffset, mat, parent);
    }

    /// Same but laterally shifted by `lateralOffset` (positive = right of direction A→B).
    private void BuildFlatQuadOffset(
        string   name,
        Vector2  a2, Vector2 b2,
        float    width,
        float    lateralOffset,
        System.Func<float, float, float> getY,
        float    yOffset,
        Material mat,
        Transform parent)
    {
        float len = Vector2.Distance(a2, b2);
        if (len < MinSegLen) return;

        Vector2 dir2  = (b2 - a2).normalized;
        Vector2 right2 = new Vector2(dir2.y, -dir2.x); // 90° CW

        // Center line of this strip, offset laterally
        Vector2 ca = a2 + right2 * lateralOffset;
        Vector2 cb = b2 + right2 * lateralOffset;

        float hw = width * 0.5f;

        // Overlap endpoints slightly to hide seams at intersections
        const float ovlp = 0.2f;
        Vector2 ea = ca - dir2 * ovlp;
        Vector2 eb = cb + dir2 * ovlp;

        Vector3 v0 = ToV3(ea - right2 * hw, getY, yOffset);
        Vector3 v1 = ToV3(ea + right2 * hw, getY, yOffset);
        Vector3 v2 = ToV3(eb - right2 * hw, getY, yOffset);
        Vector3 v3 = ToV3(eb + right2 * hw, getY, yOffset);

        Vector3 mid = (v0 + v1 + v2 + v3) * 0.25f;

        Mesh mesh = new Mesh { name = name + "_M" };
        mesh.vertices  = new[] { v0 - mid, v1 - mid, v2 - mid, v3 - mid };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.uv        = new Vector2[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = mid;

        go.AddComponent<MeshFilter>().sharedMesh     = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    /// Render dashed center line markings along a road segment.
    private void RenderDashedLine(
        string name,
        Vector2 a2, Vector2 b2,
        float dashW, float dashLen, float dashGap,
        float worldY,
        Material mat,
        Transform parent)
    {
        float totalLen = Vector2.Distance(a2, b2);
        if (totalLen < dashLen) return;

        Vector2 dir = (b2 - a2).normalized;
        float traveled = dashLen * 0.5f; // start offset
        int di = 0;

        while (traveled + dashLen < totalLen)
        {
            Vector2 da = a2 + dir * traveled;
            Vector2 db = a2 + dir * (traveled + dashLen);

            Vector3 va = new Vector3(da.x, worldY, da.y);
            Vector3 vb = new Vector3(db.x, worldY, db.y);

            Vector2 right2 = new Vector2(dir.y, -dir.x);
            float hw = dashW * 0.5f;
            Vector3 v0 = va - new Vector3(right2.x, 0, right2.y) * hw;
            Vector3 v1 = va + new Vector3(right2.x, 0, right2.y) * hw;
            Vector3 v2 = vb - new Vector3(right2.x, 0, right2.y) * hw;
            Vector3 v3 = vb + new Vector3(right2.x, 0, right2.y) * hw;
            Vector3 mid = (v0 + v1 + v2 + v3) * 0.25f;

            Mesh mesh = new Mesh { name = $"{name}_{di}_M" };
            mesh.vertices  = new[] { v0 - mid, v1 - mid, v2 - mid, v3 - mid };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.uv        = new Vector2[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject($"{name}_{di}");
            go.transform.SetParent(parent, false);
            go.transform.position = mid;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;

            traveled += dashLen + dashGap;
            di++;

            // Cap dashes per segment to avoid runaway memory on very long roads
            if (di > 200) break;
        }
    }

    /// Place a simple quad (for the intersection pad).
    private static GameObject QuadFlat(string name, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial    = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows    = false;
        var col = go.GetComponent<Collider>();
        if (col) Object.Destroy(col);
        return go;
    }

    // ── Street trees ──────────────────────────────────────────────────────────

    private void SpawnStreetTrees(
        string name,
        Vector2 a2, Vector2 b2,
        float lateralOffset,
        float spacing)
    {
        float totalLen = Vector2.Distance(a2, b2);
        if (totalLen < TreeMinSpacing) return;

        Vector2 dir   = (b2 - a2).normalized;
        Vector2 right = new Vector2(dir.y, -dir.x);

        float dist  = spacing * 0.5f;
        int   index = 0;

        // Trees on both sides
        float[] sides = { -lateralOffset, lateralOffset };

        while (dist < totalLen && index < 80) // cap trees per segment
        {
            Vector2 pt = a2 + dir * dist;

            foreach (float side in sides)
            {
                Vector2 treePos = pt + right * side;
                float   ty      = GetY(treePos.x, treePos.y);

                // Trunk
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = $"{name}_T_{index}";
                trunk.transform.SetParent(_treesRoot, false);
                trunk.transform.position   = new Vector3(treePos.x, ty + TreeTrunkHeight * 0.5f, treePos.y);
                trunk.transform.localScale = new Vector3(TreeTrunkRadius * 2f, TreeTrunkHeight, TreeTrunkRadius * 2f);
                ApplyRdr(trunk, _matTreeTrunk);
                DestroyColl(trunk);

                // Canopy
                var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                canopy.name = $"{name}_C_{index}";
                canopy.transform.SetParent(_treesRoot, false);
                canopy.transform.position   = new Vector3(treePos.x, ty + TreeTrunkHeight + TreeCanopyOffsetY, treePos.y);
                canopy.transform.localScale = Vector3.one * (TreeCanopyRadius * 2f);
                ApplyRdr(canopy, _matTreeCanopy);
                DestroyColl(canopy);
            }

            dist  += spacing;
            index++;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector3 ToV3(Vector2 p, System.Func<float, float, float> getY, float yOff)
        => new Vector3(p.x, getY(p.x, p.y) + yOff, p.y);

    private float GetY(float x, float z)
        => (_terrainGen != null ? _terrainGen.SampleHeight(x, z) : 0f);

    private float GetMidY(Vector2 a, Vector2 b)
    {
        Vector2 m = (a + b) * 0.5f;
        return GetY(m.x, m.y);
    }

    private static void ApplyRdr(GameObject go, Material mat)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        r.sharedMaterial    = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows    = false;
    }

    private static void DestroyColl(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c) Object.Destroy(c);
    }

    private Transform RootForType(string type) => type switch
    {
        "arterial"   => _arterialsRoot,
        "collector"  => _collectorsRoot,
        "pedestrian" => _pedestrianRoot,
        "cycle"      => _cycleRoot,
        _            => _localRoot,
    };

    private Material MatForType(string type) => type switch
    {
        "arterial"   => _matArterial,
        "collector"  => _matCollector,
        "pedestrian" => _matPedestrian,
        "cycle"      => _matCycleway,
        _            => _matLocal,
    };

    private static float DefaultWidth(string type) => type switch
    {
        "arterial"   => 18f,
        "collector"  => 12f,
        "pedestrian" => 4f,
        "cycle"      => 3f,
        _            => 8f,
    };

    private static float BlockSpacingForType(string type) => type switch
    {
        "commercial" or "mixed_use" => 120f,
        "industrial"                => 180f,
        "civic"                     => 130f,
        _                           => 100f,
    };

    private static bool IsNonRoadSector(string type) => type is
        "park" or "forest" or "wetland" or "water" or "energy";
}
