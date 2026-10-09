using UnityEngine;

/// <summary>
/// Smoothly rotates a wind turbine rotor around its axle.
/// Speed changes are eased in/out over time so the turbine visibly accelerates
/// and decelerates rather than snapping to the new speed instantly.
/// </summary>
public class WindTurbineSpinner : MonoBehaviour
{
    [Tooltip("Current rotation speed in degrees per second (live display value).")]
    [SerializeField] private float rotationSpeed = 80f;

    [Tooltip("Target speed the spinner is easing toward.")]
    private float _targetSpeed;

    [Tooltip("Seconds to reach a new speed (feel of turbine inertia).")]
    [SerializeField] private float speedSmoothTime = 2.2f;

    private float _speedVelocity; // used internally by SmoothDamp

    [Tooltip("Local axis of rotation. Defaults to local forward (the turbine axle).")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    private void Awake()
    {
        _targetSpeed = rotationSpeed;
    }

    /// <summary>
    /// Setting RotationSpeed assigns the *target* the spinner eases toward.
    /// Read RotationSpeed to get the current live (interpolated) speed.
    /// </summary>
    public float RotationSpeed
    {
        get => rotationSpeed;
        set => _targetSpeed = value;
    }

    public Vector3 RotationAxis
    {
        get => rotationAxis;
        set => rotationAxis = value;
    }

    private void Update()
    {
        // Smoothly ease the current speed toward the target (simulates rotor inertia)
        rotationSpeed = Mathf.SmoothDamp(rotationSpeed, _targetSpeed, ref _speedVelocity, speedSmoothTime);

        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
