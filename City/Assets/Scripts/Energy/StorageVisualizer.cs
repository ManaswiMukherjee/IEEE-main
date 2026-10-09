using UnityEngine;

/// <summary>
/// Attached to individual BESS battery unit GameObjects.
/// Drives emission color and pulse rhythm to visually reflect:
///   - State of charge (green at high SOC → amber → dim at low SOC)
///   - Power flow direction (steady slow pulse when idle, fast pulse when discharging, reverse when charging)
/// </summary>
[RequireComponent(typeof(Renderer))]
public class StorageVisualizer : MonoBehaviour
{
    // Emission colors for different states
    private static readonly Color ColorCharging    = new Color(0.0f, 0.55f, 2.0f);   // deep blue-white
    private static readonly Color ColorIdle        = new Color(0.0f, 0.45f, 1.2f);   // steady blue
    private static readonly Color ColorDischarging = new Color(0.1f, 1.2f, 0.65f);   // teal-green

    private Material _mat;
    private float    _phase;     // internal oscillator phase in radians

    private void Awake()
    {
        // Stagger phase so each unit doesn't pulse in sync with its neighbors
        _phase = Random.Range(0f, Mathf.PI * 2f);

        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            _mat = r.material; // instanced material so we don't corrupt the shared one
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

        StorageTelemetryData data = EnergyManager.Instance.StorageData;

        // --- Determine base emission color from flow direction ---
        Color baseColor;
        float pulseFreq;

        if (data.powerFlowMw > 1f)
        {
            // Discharging to grid — fast teal pulse
            baseColor = ColorDischarging;
            pulseFreq = Mathf.Lerp(1.2f, 3.5f, Mathf.Clamp01(data.powerFlowMw / data.maxPowerMw));
        }
        else if (data.powerFlowMw < -1f)
        {
            // Charging from renewables — moderate blue pulse
            baseColor = ColorCharging;
            pulseFreq = Mathf.Lerp(0.8f, 2.0f, Mathf.Clamp01(Mathf.Abs(data.powerFlowMw) / data.maxPowerMw));
        }
        else
        {
            // Standby / idle — very slow, dim blue breath
            baseColor = ColorIdle;
            pulseFreq = 0.4f;
        }

        // --- Scale brightness by State of Charge ---
        // SOC 0%→100% maps to emission multiplier 0.15→1.0
        float socMultiplier = Mathf.Lerp(0.15f, 1.0f, data.stateOfChargePercent / 100f);

        // --- Sinusoidal pulse ---
        _phase += pulseFreq * Time.deltaTime * Mathf.PI * 2f;
        float pulse = (Mathf.Sin(_phase) * 0.5f + 0.5f); // 0..1

        // Emission intensity: oscillates between 30% and 100% of max brightness
        float intensity = Mathf.Lerp(0.30f, 1.0f, pulse) * socMultiplier;

        _mat.SetColor("_EmissionColor", baseColor * intensity);
    }
}
