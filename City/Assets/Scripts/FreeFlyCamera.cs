using UnityEngine;
using UnityEngine.InputSystem;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 300f;
    [SerializeField] private float fastSpeed = 1000f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float maxPitch = 89f;

    private float yaw;
    private float pitch;

    private bool mouseCaptured;

    private void Start() {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        if (pitch > 180f)
            pitch -= 360f;

        CaptureMouse();
    }

    private void Update()
    {
        bool isConsoleOpen = EnergyManager.Instance != null && EnergyManager.Instance.IsConsoleOpen;

        if (isConsoleOpen)
        {
            if (mouseCaptured)
            {
                ReleaseMouse();
            }

            // In console mode, still allow movement with WASD so user can navigate while inspecting
            HandleMovement();
            return;
        }

        HandleMouse();
        HandleMovement();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
            ReleaseMouse();
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) {
            CaptureMouse();
        }
    }

    private void HandleMouse() {
        if (!mouseCaptured || Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        if (mouseDelta.sqrMagnitude > 0.001f)
        {
            yaw += mouseDelta.x * mouseSensitivity;
            pitch -= mouseDelta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    private void HandleMovement() {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        Vector3 direction = Vector3.zero;

        if (keyboard.wKey.isPressed)
            direction += transform.forward;
        if (keyboard.sKey.isPressed)
            direction -= transform.forward;
        if (keyboard.dKey.isPressed)
            direction += transform.right;
        if (keyboard.aKey.isPressed)
            direction -= transform.right;
        if (keyboard.spaceKey.isPressed)
            direction += Vector3.up;
        if (keyboard.leftShiftKey.isPressed)
            direction -= Vector3.up;
        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        float speed = keyboard.rightShiftKey.isPressed ? fastSpeed : moveSpeed;
        Vector3 newPos = transform.position + direction * speed * Time.deltaTime;

        // Floor collision: camera cannot penetrate below the floor
        const float minHeight = 1.0f;
        if (newPos.y < minHeight)
            newPos.y = minHeight;

        transform.position = newPos;
    }

    public void CaptureMouse() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        mouseCaptured = true;
    }

    public void ReleaseMouse() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        mouseCaptured = false;
    }

    public void ResetOrientation(Vector3 eulerAngles) {
        yaw = eulerAngles.y;
        pitch = eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
