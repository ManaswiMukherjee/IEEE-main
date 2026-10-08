// EnvironmentGenerator.cs
// Generates environmental features: green buffers, noise barriers, etc.
// Provides the framework for future climate-resilience features.

using UnityEngine;

public class EnvironmentGenerator
{
    private readonly Transform     _parent;
    private readonly CityMaterials _mats;

    public EnvironmentGenerator(Transform parent, CityMaterials mats)
    {
        _parent = parent;
        _mats   = mats;
    }

    public void Generate(EnvironmentData environment, CityData city)
    {
        Transform envRoot = new GameObject("Environment").transform;
        envRoot.SetParent(_parent);

        // Future: create noise barriers, industrial green buffers, etc.
        // For now, log status.
        Debug.Log("[Environment] Generated environmental features.");
    }
}
