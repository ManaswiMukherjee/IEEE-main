// TransportGenerator.cs
// Generates transport infrastructure (bus stops, transit hubs)
// from transport data and transport-typed sectors.

using System.Collections.Generic;
using UnityEngine;

public class TransportGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    public TransportGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(
        TransportData  transport,
        RoadGraphData  roadGraph,
        CityData       city)
    {
        Transform transportRoot = new GameObject("Transport").transform;
        transportRoot.SetParent(_parent);

        int count = 0;

        // Place transit hubs at road nodes typed transit_hub
        if (roadGraph?.nodes != null)
        {
            foreach (var node in roadGraph.nodes)
            {
                if (node.type != "transit_hub") continue;
                CreateTransitHub(node, transportRoot);
                count++;
            }
        }

        Debug.Log($"[Transport] Generated transport infrastructure ({count} transit hubs).");
    }

    private void CreateTransitHub(RoadNodeData node, Transform parent)
    {
        float terrainH = _terrainGen != null ? _terrainGen.SampleHeight(node.position.x, node.position.z) : 0f;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = $"TransitHub_{node.id}";
        go.transform.SetParent(parent);
        go.transform.position   = new Vector3(node.position.x, terrainH + 1.5f, node.position.z);
        go.transform.localScale = new Vector3(15f, 3f, 15f);
        go.GetComponent<Renderer>().material = _mats.Transport;
        UnityEngine.Object.Destroy(go.GetComponent<CapsuleCollider>());
    }
}
