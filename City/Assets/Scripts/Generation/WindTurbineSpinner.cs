using UnityEngine;

/// <summary>
/// Smoothly rotates a wind turbine rotor around its axle.
/// </summary>
public class WindTurbineSpinner : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float rotationSpeed = 80f;

    [Tooltip("Local axis of rotation. Defaults to local forward (the turbine axle).")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    public float RotationSpeed
    {
        get => rotationSpeed;
        set => rotationSpeed = value;
    }

    public Vector3 RotationAxis
    {
        get => rotationAxis;
        set => rotationAxis = value;
    }

    private void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
