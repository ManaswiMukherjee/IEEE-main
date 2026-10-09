using UnityEngine;

/// <summary>
/// Attached to individual solar panel GameObjects.
/// Smoothly tilts the panel to match the tilt angle set in the Energy Console UI.
/// Preserves the original azimuth (yaw) so panels keep facing the configured solar direction.
/// </summary>
public class SolarPanelTilter : MonoBehaviour
{
    /// <summary>Azimuth (Y-axis rotation) baked in at generation time. Never changes.</summary>
    private float _azimuthDeg;

    /// <summary>How quickly the panel physically rotates to a new tilt angle (seconds).</summary>
    [SerializeField] private float tiltSmoothTime = 0.8f;

    private float _currentTilt;
    private float _tiltVelocity;

    /// <summary>Called once by EnergyGenerator after placing the panel, to record its initial orientation.</summary>
    public void Initialize(float azimuthDeg, float initialTiltDeg)
    {
        _azimuthDeg = azimuthDeg;
        _currentTilt = initialTiltDeg;
        ApplyRotation(_currentTilt);
    }

    private void Update()
    {
        if (EnergyManager.Instance == null) return;

        float targetTilt = EnergyManager.Instance.SolarData.tiltDegrees;

        // Smooth-damp the tilt angle
        _currentTilt = Mathf.SmoothDamp(_currentTilt, targetTilt, ref _tiltVelocity, tiltSmoothTime);

        ApplyRotation(_currentTilt);
    }

    private void ApplyRotation(float tiltDeg)
    {
        // Pitch (X) = negative tilt so the panel faces up/forward
        // Yaw (Y) = preserved azimuth from generation (e.g. 180° = south-facing)
        transform.rotation = Quaternion.Euler(-tiltDeg, _azimuthDeg - 180f, 0f);
    }
}
