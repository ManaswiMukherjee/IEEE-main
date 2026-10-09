
using UnityEngine;

public class TerrainGenerator
{
    private readonly Transform _parent;
    private readonly CityMaterials _mats;

    public Terrain UnityTerrain { get; private set; } = null;

    public float MinHeight => 0f;
    public float MaxHeight => 0f;

    public TerrainGenerator(Transform parent, CityMaterials mats)
    {
        _parent = parent;
        _mats = mats;
    }

    public void Generate(TerrainData terrainData, CityData city)
    {
        // Intentionally create no terrain or ground object.
        UnityTerrain = null;

        Debug.Log(
            "[Terrain] Ground generation disabled for artifact isolation.");
    }

    public float SampleHeight(float worldX, float worldZ)
    {
        return 0f;
    }
}
