using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// High-performance mesh builder for combining identical-material planar, box, or cylinder/sphere meshes
/// into single batches with correct triangle winding and normals.
/// </summary>
public class MeshBatcher
{
    private readonly List<Vector3> _vertices = new List<Vector3>(16384);
    private readonly List<int> _triangles = new List<int>(24576);
    private readonly List<Vector2> _uvs = new List<Vector2>(16384);
    private readonly List<Vector3> _normals = new List<Vector3>(16384);

    public int VertexCount => _vertices.Count;

    public void Clear()
    {
        _vertices.Clear();
        _triangles.Clear();
        _uvs.Clear();
        _normals.Clear();
    }

    /// <summary>
    /// Adds a quad with vertices given in counter-clockwise order when viewed from the front/top.
    /// Triangles: (0, 1, 2) and (0, 2, 3)
    /// </summary>
    public void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        int startIndex = _vertices.Count;

        _vertices.Add(v0);
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);

        Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);

        _uvs.Add(uv0);
        _uvs.Add(uv1);
        _uvs.Add(uv2);
        _uvs.Add(uv3);

        _triangles.Add(startIndex);
        _triangles.Add(startIndex + 1);
        _triangles.Add(startIndex + 2);

        _triangles.Add(startIndex);
        _triangles.Add(startIndex + 2);
        _triangles.Add(startIndex + 3);
    }

    /// <summary>
    /// Adds a 3D box with proper outward-facing normals.
    /// </summary>
    public void AddBox(Vector3 center, Vector3 size)
    {
        Vector3 half = size * 0.5f;

        Vector3 c000 = center + new Vector3(-half.x, -half.y, -half.z);
        Vector3 c100 = center + new Vector3( half.x, -half.y, -half.z);
        Vector3 c110 = center + new Vector3( half.x,  half.y, -half.z);
        Vector3 c010 = center + new Vector3(-half.x,  half.y, -half.z);

        Vector3 c001 = center + new Vector3(-half.x, -half.y,  half.z);
        Vector3 c101 = center + new Vector3( half.x, -half.y,  half.z);
        Vector3 c111 = center + new Vector3( half.x,  half.y,  half.z);
        Vector3 c011 = center + new Vector3(-half.x,  half.y,  half.z);

        Vector2 uv0 = new Vector2(0, 0);
        Vector2 uv1 = new Vector2(1, 0);
        Vector2 uv2 = new Vector2(1, 1);
        Vector2 uv3 = new Vector2(0, 1);

        // Top (+Y): looking down from above, CCW is c010 -> c011 -> c111 -> c110
        AddQuad(c010, c011, c111, c110, uv0, uv1, uv2, uv3);
        // Bottom (-Y): looking up from below, CCW is c000 -> c100 -> c101 -> c001
        AddQuad(c000, c100, c101, c001, uv0, uv1, uv2, uv3);
        // Front (+Z): looking from +Z, CCW is c001 -> c101 -> c111 -> c011
        AddQuad(c001, c101, c111, c011, uv0, uv1, uv2, uv3);
        // Back (-Z): looking from -Z, CCW is c100 -> c000 -> c010 -> c110
        AddQuad(c100, c000, c010, c110, uv0, uv1, uv2, uv3);
        // Left (-X): looking from -X, CCW is c000 -> c001 -> c011 -> c010
        AddQuad(c000, c001, c011, c010, uv0, uv1, uv2, uv3);
        // Right (+X): looking from +X, CCW is c101 -> c100 -> c110 -> c111
        AddQuad(c101, c100, c110, c111, uv0, uv1, uv2, uv3);
    }

    /// <summary>
    /// Adds a smooth tree trunk (vertical cylinder with per-vertex outward normals).
    /// </summary>
    public void AddCylinder(Vector3 bottomCenter, float radius, float height, int segments = 12)
    {
        float angleStep = 2f * Mathf.PI / segments;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * angleStep;
            float a1 = (i + 1) * angleStep;

            float cx0 = Mathf.Cos(a0), sz0 = Mathf.Sin(a0);
            float cx1 = Mathf.Cos(a1), sz1 = Mathf.Sin(a1);

            Vector3 b0 = bottomCenter + new Vector3(cx0 * radius, 0f, sz0 * radius);
            Vector3 b1 = bottomCenter + new Vector3(cx1 * radius, 0f, sz1 * radius);
            Vector3 t1 = bottomCenter + new Vector3(cx1 * radius, height, sz1 * radius);
            Vector3 t0 = bottomCenter + new Vector3(cx0 * radius, height, sz0 * radius);

            // Per-vertex outward normals (smooth shading)
            Vector3 n0 = new Vector3(cx0, 0f, sz0);
            Vector3 n1 = new Vector3(cx1, 0f, sz1);

            int si = _vertices.Count;
            _vertices.Add(b0); _vertices.Add(b1); _vertices.Add(t1); _vertices.Add(t0);
            _normals.Add(n0);  _normals.Add(n1);  _normals.Add(n1);  _normals.Add(n0);
            _uvs.Add(new Vector2((float)i / segments, 0f));
            _uvs.Add(new Vector2((float)(i + 1) / segments, 0f));
            _uvs.Add(new Vector2((float)(i + 1) / segments, 1f));
            _uvs.Add(new Vector2((float)i / segments, 1f));
            _triangles.Add(si); _triangles.Add(si + 1); _triangles.Add(si + 2);
            _triangles.Add(si); _triangles.Add(si + 2); _triangles.Add(si + 3);
        }
    }

    /// <summary>
    /// Adds a smooth organic tree canopy sphere with per-vertex outward normals for soft, rounded shading.
    /// </summary>
    public void AddCanopy(Vector3 center, Vector3 radius, int latitudeSegments = 7, int longitudeSegments = 12)
    {
        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            float theta0 = Mathf.PI * (float)lat / latitudeSegments;
            float theta1 = Mathf.PI * (float)(lat + 1) / latitudeSegments;

            float sinT0 = Mathf.Sin(theta0), cosT0 = Mathf.Cos(theta0);
            float sinT1 = Mathf.Sin(theta1), cosT1 = Mathf.Cos(theta1);

            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                float phi0 = 2f * Mathf.PI * (float)lon / longitudeSegments;
                float phi1 = 2f * Mathf.PI * (float)(lon + 1) / longitudeSegments;

                float cosPhi0 = Mathf.Cos(phi0), sinPhi0 = Mathf.Sin(phi0);
                float cosPhi1 = Mathf.Cos(phi1), sinPhi1 = Mathf.Sin(phi1);

                // Unit sphere normals (point outward from center)
                Vector3 n00 = new Vector3(cosPhi0 * sinT0, cosT0, sinPhi0 * sinT0).normalized;
                Vector3 n10 = new Vector3(cosPhi1 * sinT0, cosT0, sinPhi1 * sinT0).normalized;
                Vector3 n11 = new Vector3(cosPhi1 * sinT1, cosT1, sinPhi1 * sinT1).normalized;
                Vector3 n01 = new Vector3(cosPhi0 * sinT1, cosT1, sinPhi0 * sinT1).normalized;

                // Scale normals by radius to get actual vertex positions
                Vector3 v00 = center + new Vector3(n00.x * radius.x, n00.y * radius.y, n00.z * radius.z);
                Vector3 v10 = center + new Vector3(n10.x * radius.x, n10.y * radius.y, n10.z * radius.z);
                Vector3 v11 = center + new Vector3(n11.x * radius.x, n11.y * radius.y, n11.z * radius.z);
                Vector3 v01 = center + new Vector3(n01.x * radius.x, n01.y * radius.y, n01.z * radius.z);

                int si = _vertices.Count;
                _vertices.Add(v00); _vertices.Add(v10); _vertices.Add(v11); _vertices.Add(v01);
                _normals.Add(n00);  _normals.Add(n10);  _normals.Add(n11);  _normals.Add(n01);
                _uvs.Add(new Vector2((float)lon / longitudeSegments,       (float)lat / latitudeSegments));
                _uvs.Add(new Vector2((float)(lon + 1) / longitudeSegments, (float)lat / latitudeSegments));
                _uvs.Add(new Vector2((float)(lon + 1) / longitudeSegments, (float)(lat + 1) / latitudeSegments));
                _uvs.Add(new Vector2((float)lon / longitudeSegments,       (float)(lat + 1) / latitudeSegments));

                // CCW winding when viewed from outside the sphere
                _triangles.Add(si);     _triangles.Add(si + 1); _triangles.Add(si + 2);
                _triangles.Add(si);     _triangles.Add(si + 2); _triangles.Add(si + 3);
            }
        }
    }

    public GameObject BuildGameObject(string name, Material material, Transform parent)
    {
        if (_vertices.Count == 0) return null;

        Mesh mesh = new Mesh
        {
            name = $"{name}_Mesh",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        mesh.SetVertices(_vertices);
        mesh.SetTriangles(_triangles, 0);
        mesh.SetUVs(0, _uvs);
        mesh.SetNormals(_normals);
        mesh.RecalculateBounds();

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        mf.sharedMesh = mesh;
        mr.sharedMaterial = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        return go;
    }
}
