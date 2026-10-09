using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attached to individual energy infrastructure objects (wind turbines, solar panels, battery units, substations).
/// Handles click detection, multi-object registration, and visual selection highlighting.
/// </summary>
public class EnergySource : MonoBehaviour
{
    [SerializeField] public EnergyType sourceType;

    private Renderer[] _renderers;
    private readonly List<Material> _instancedMats = new List<Material>();
    private readonly List<Color> _originalEmissions = new List<Color>();
    private readonly List<bool> _hadEmission = new List<bool>();
    private bool _isHighlighted = false;

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnEnable()
    {
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.RegisterSource(this);
        }
    }

    private void OnDisable()
    {
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.UnregisterSource(this);
        }
    }

    public void CacheRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _instancedMats.Clear();
        _originalEmissions.Clear();
        _hadEmission.Clear();

        if (_renderers == null || _renderers.Length == 0) return;

        foreach (Renderer r in _renderers)
        {
            if (r == null) continue;
            Material mat = r.material; // creates an instance per renderer so highlighting is isolated
            _instancedMats.Add(mat);

            if (mat != null && mat.HasProperty("_EmissionColor"))
            {
                _hadEmission.Add(mat.IsKeywordEnabled("_EMISSION"));
                _originalEmissions.Add(mat.GetColor("_EmissionColor"));
            }
            else
            {
                _hadEmission.Add(false);
                _originalEmissions.Add(Color.black);
            }
        }
    }

    public void SetSelected(bool isSelected, Color highlightColor)
    {
        if (_isHighlighted == isSelected) return;
        _isHighlighted = isSelected;

        if (_instancedMats.Count == 0)
        {
            CacheRenderers();
        }

        for (int i = 0; i < _instancedMats.Count; i++)
        {
            Material mat = _instancedMats[i];
            if (mat == null || !mat.HasProperty("_EmissionColor")) continue;

            if (isSelected)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", highlightColor);
            }
            else
            {
                if (i < _hadEmission.Count && _hadEmission[i])
                {
                    mat.SetColor("_EmissionColor", _originalEmissions[i]);
                }
                else
                {
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
