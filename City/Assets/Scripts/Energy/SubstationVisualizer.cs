using UnityEngine;

/// <summary>
/// Attached to individual Transformer and gantry GameObjects inside a Substation.
/// Drives emission to visually communicate:
///   - Grid frequency deviation from 50 Hz (red tint when off-frequency)
///   - Network load ratio (brighter when demand exceeds renewables → importing)
///   - Stable export state (cool blue-green shimmer when city is a net exporter)
/// </summary>
[RequireComponent(typeof(Renderer))]
public class SubstationVisualizer : MonoBehaviour
{
    // Base emission tones for different grid states
    private static readonly Color ColorStable   = new Color(0.0f, 0.60f, 1.8f);    // clean grid blue
    private static readonly Color ColorExport   = new Color(0.2f, 1.4f, 0.70f);    // net-export green
    private static readonly Color ColorImport   = new Color(1.0f, 0.55f, 0.10f);   // import amber
    private static readonly Color ColorUnstable = new Color(1.5f, 0.10f, 0.10f);   // frequency fault red

    /// <summary>Frequency deviation threshold before the unstable red tint kicks in (in Hz).</summary>
    private const float FreqDeviationWarn = 0.3f;
    private const float FreqNominal = 50.0f;

    private Material _mat;
    private float    _phase;

    private void Awake()
    {
        _phase = Random.Range(0f, Mathf.PI * 2f);

        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            _mat = r.material;
            if (_mat.HasProperty("_EmissionColor"))
                _mat.EnableKeyword("_EMISSION");
        }
    }

    private void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }

    private void Update()
    {
        if (_mat == null || EnergyManager.Instance == null) return;

        GridTelemetryData grid = EnergyManager.Instance.GridData;

        float totalClean =
            EnergyManager.Instance.WindData.CurrentOutputMw +
            EnergyManager.Instance.SolarData.CurrentOutputMw +
            Mathf.Max(0f, EnergyManager.Instance.StorageData.powerFlowMw);

        float netExchange = grid.cityDemandMw - totalClean; // positive = importing, negative = exporting

        // --- Frequency deviation → health indicator ---
        float freqDev = Mathf.Abs(grid.gridFrequencyHz - FreqNominal);
        float faultBlend = Mathf.Clamp01((freqDev - FreqDeviationWarn) / 0.5f);

        // --- Determine primary color from net exchange ---
        Color baseColor;
        float pulseFreq;

        if (faultBlend > 0.01f)
        {
            // Frequency fault — urgent red flicker
            baseColor = Color.Lerp(ColorStable, ColorUnstable, faultBlend);
            pulseFreq = Mathf.Lerp(1.5f, 6.0f, faultBlend);
        }
        else if (netExchange < -1f)
        {
            // Net exporter — calm slow green shimmer
            float exportStrength = Mathf.Clamp01(Mathf.Abs(netExchange) / Mathf.Max(1f, grid.cityDemandMw));
            baseColor = Color.Lerp(ColorStable, ColorExport, exportStrength * 0.8f);
            pulseFreq = 0.5f;
        }
        else if (netExchange > 5f)
        {
            // Significant import demand — amber pulse quickens with load
            float importRatio = Mathf.Clamp01(netExchange / Mathf.Max(1f, grid.cityDemandMw));
            baseColor = Color.Lerp(ColorStable, ColorImport, importRatio);
            pulseFreq = Mathf.Lerp(0.8f, 2.5f, importRatio);
        }
        else
        {
            // Balanced / near-balanced — steady slow blue
            baseColor = ColorStable;
            pulseFreq = 0.5f;
        }

        // --- Sinusoidal emission pulse ---
        _phase += pulseFreq * Time.deltaTime * Mathf.PI * 2f;
        float pulse = Mathf.Sin(_phase) * 0.5f + 0.5f; // 0..1

        // Intensity scales with load — always visible but livelier under load
        float loadFactor = Mathf.Clamp01(grid.cityDemandMw / 200f);
        float intensity = Mathf.Lerp(0.25f, 1.0f, pulse) * Mathf.Lerp(0.5f, 1.2f, loadFactor);

        _mat.SetColor("_EmissionColor", baseColor * intensity);
    }
}
