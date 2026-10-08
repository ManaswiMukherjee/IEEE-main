// TerrainGenerator.cs
// Generates a Unity Terrain from TerrainData and city dimensions.
// Uses multi-octave Perlin noise for rolling/hilly terrain; flat for flat type.

using System;
using UnityEngine;

public class TerrainGenerator
{
    private readonly Transform      _parent;
    private readonly CityMaterials  _mats;

    // Unity Terrain created here, exposed so road/building generators can sample height.
    public Terrain UnityTerrain { get; private set; }
    public float MinHeight { get; private set; } = 0f;
    public float MaxHeight { get; private set; } = 0f;

    public TerrainGenerator(Transform parent, CityMaterials mats)
    {
        _parent = parent;
        _mats   = mats;
    }

    public void Generate(TerrainData terrainData, CityData city)
    {
        float width = city.dimensions.width;
        float depth = city.dimensions.depth;

        // Resolve generation params
        TerrainGenerationData gen = terrainData.generation ?? new TerrainGenerationData();
        int   resolution   = Mathf.Max(gen.resolution, 33); // Unity terrain min resolution is 33
        int   seed         = gen.seed;
        string method      = gen.method ?? "flat";

        // Determine actual vertical height range
        float elevMin = terrainData.elevation?.minimum ?? 0f;
        float elevMax = terrainData.elevation?.maximum ?? (gen.height_scale > 0 ? gen.height_scale : 50f);
        if (elevMax <= elevMin) elevMax = elevMin + 50f;
        float heightScale = elevMax;

        // Snap resolution to 2^n + 1 as Unity requires
        resolution = NextTerrainResolution(resolution);

        // Build heightmap
        float[,] heights = GenerateHeights(
            method, resolution, heightScale, width, depth, seed, terrainData, gen, out float actualMin, out float actualMax);

        MinHeight = actualMin;
        MaxHeight = actualMax;

        // Create Unity Terrain Data asset
        var terrainDataAsset = new UnityEngine.TerrainData();
        terrainDataAsset.heightmapResolution = resolution;
        terrainDataAsset.size = new Vector3(width, heightScale, depth);
        terrainDataAsset.SetHeights(0, 0, heights);

        // Assign a green TerrainLayer so URP Terrain Lit renders properly without fallback checkerboard
        TerrainLayer layer = new TerrainLayer();
        Texture2D diffuseTex = new Texture2D(4, 4);
        Color[] cols = new Color[16];
        Color groundColor = new Color(0.25f, 0.42f, 0.22f);
        for (int i = 0; i < cols.Length; i++) cols[i] = groundColor;
        diffuseTex.SetPixels(cols);
        diffuseTex.Apply();
        layer.diffuseTexture = diffuseTex;
        layer.tileSize = new Vector2(25f, 25f);
        terrainDataAsset.terrainLayers = new TerrainLayer[] { layer };

        // Create terrain GameObject
        GameObject terrainGo = UnityEngine.Terrain.CreateTerrainGameObject(terrainDataAsset);
        terrainGo.name = "Terrain";
        terrainGo.transform.SetParent(_parent);
        terrainGo.transform.position = Vector3.zero;

        UnityTerrain = terrainGo.GetComponent<Terrain>();

        Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (terrainShader != null)
        {
            UnityTerrain.materialTemplate = new Material(terrainShader);
        }
        else if (_mats?.Terrain != null)
        {
            UnityTerrain.materialTemplate = _mats.Terrain;
        }

        Debug.Log($"[Terrain] Generated terrain {width}x{depth}m, resolution {resolution}, elevation range [{MinHeight:F1}m - {MaxHeight:F1}m].");
    }

    /// <summary>Authoritative terrain height query at world coordinates (worldX, worldZ).</summary>
    public float SampleHeight(float worldX, float worldZ)
    {
        if (UnityTerrain == null) return 0f;
        return UnityTerrain.SampleHeight(new Vector3(worldX, 0f, worldZ));
    }

    // ─── Heightmap generation ─────────────────────────────────────────────────

    private float[,] GenerateHeights(
        string method, int res, float heightScale,
        float width, float depth,
        int seed, TerrainData terrainData, TerrainGenerationData gen,
        out float actualMin, out float actualMax)
    {
        float[,] h = new float[res, res];
        actualMin = 0f;
        actualMax = 0f;

        if (method == "flat")
        {
            // All zero → terrain is flat
            return h;
        }

        // Noise / rolling / hilly
        NoiseData noise   = gen.noise ?? new NoiseData();
        float noiseScale  = noise.scale > 0 ? noise.scale : 500f;
        int   octaves     = Mathf.Max(noise.octaves, 1);
        float persistence = noise.persistence;
        float lacunarity  = Mathf.Max(noise.lacunarity, 1f);

        // Seed-based offset
        System.Random rng = new System.Random(seed);
        float offX = (float)rng.NextDouble() * 10000f;
        float offZ = (float)rng.NextDouble() * 10000f;

        float elevMin = terrainData.elevation?.minimum ?? 0f;
        float elevMax = terrainData.elevation?.maximum ?? heightScale;
        float elevRange = elevMax - elevMin;
        if (elevRange <= 0f) elevRange = heightScale;

        // Distribution modifier
        string dist = terrainData.elevation?.distribution ?? "uniform";

        float minH = float.MaxValue;
        float maxH = float.MinValue;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = ((float)x / (res - 1)) * width;
                float nz = ((float)z / (res - 1)) * depth;

                float value      = FractalNoise(nx + offX, nz + offZ, noiseScale, octaves, persistence, lacunarity);
                float normalised = Mathf.InverseLerp(0f, 1f, value); // FractalNoise returns 0..1

                // Apply distribution biases
                normalised = ApplyDistribution(normalised, dist,
                    (float)x / (res - 1), (float)z / (res - 1));

                // Map into elevation range, then normalise to [0,1] for Unity heightmap
                float worldHeight = elevMin + normalised * elevRange;
                if (worldHeight < minH) minH = worldHeight;
                if (worldHeight > maxH) maxH = worldHeight;

                h[z, x] = Mathf.Clamp01(worldHeight / heightScale);
            }
        }

        actualMin = minH;
        actualMax = maxH;
        return h;
    }

    private static float FractalNoise(float x, float z, float scale,
        int octaves, float persistence, float lacunarity)
    {
        float value    = 0f;
        float amp      = 1f;
        float freq     = 1f / scale;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            value    += Mathf.PerlinNoise(x * freq, z * freq) * amp;
            maxValue += amp;
            amp      *= persistence;
            freq     *= lacunarity;
        }

        return maxValue > 0f ? value / maxValue : 0f;
    }

    private static float ApplyDistribution(float v, string dist, float nx, float nz)
    {
        float cx = nx - 0.5f;
        float cz = nz - 0.5f;
        float r  = Mathf.Sqrt(cx * cx + cz * cz); // 0..~0.707

        return dist switch
        {
            "center_high" => Mathf.Lerp(v, 1f - r, 0.4f),
            "center_low"  => Mathf.Lerp(v, r,       0.4f),
            "north_high"  => Mathf.Lerp(v, nz,      0.4f),
            "south_high"  => Mathf.Lerp(v, 1f - nz, 0.4f),
            "east_high"   => Mathf.Lerp(v, nx,      0.4f),
            "west_high"   => Mathf.Lerp(v, 1f - nx, 0.4f),
            _             => v,
        };
    }

    private static int NextTerrainResolution(int requested)
    {
        // Unity terrain heightmap resolution must be 2^n + 1
        int[] valid = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };
        foreach (int v in valid)
            if (v >= requested) return v;
        return 513;
    }
}
