using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class IsoOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;              
    public float distance = 20f;

    [Header("Look")]
    public float pitch = 30f;
    public float startYaw = 45f;
    public float orthoSize = 4f;

    [Header("Controls")]
    public float dragSensitivity = 0.3f;
    public float smoothing = 12f;
    public bool allowZoom = true;
    public float zoomSpeed = 1f;
    public float minOrthoSize = 2.5f;
    public float maxOrthoSize = 6f;
    public float maxPanDistance = 3f;

    [Header("Click to focus")]
    public LayerMask clickMask = ~0;      
    public float clickMaxPixels = 6f;     

    Camera cam;
    float yaw, targetYaw;
    float ortho, targetOrtho;
    Vector3 panOffset, targetPan;

    FocusObject focused;
    Vector3 savedPan;
    float savedOrtho, savedYaw;
    Vector2 pressPos;

    public bool IsFocused => focused != null;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        //Makes sure the starting zoom is always inside the allowed zone
        maxOrthoSize = Mathf.Max(maxOrthoSize, orthoSize);
        minOrthoSize = Mathf.Min(minOrthoSize, orthoSize);

        yaw = targetYaw = startYaw;
        ortho = targetOrtho = orthoSize;
        cam.orthographicSize = ortho;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        if (mouse != null)
        {
            if (IsFocused)
            {
                //right click or Esc to leave out of focus(clicking empty space is handled in HandleClick)
                bool exit = mouse.rightButton.wasPressedThisFrame ||
                            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
                if (exit) ExitFocus();
            }
            else
            {
                // Hold and drag to rotate (horizontal only)
                if (mouse.leftButton.isPressed)
                    targetYaw += mouse.delta.ReadValue().x * dragSensitivity;

                // Zoom
                if (allowZoom)
                {
                    float scroll = mouse.scroll.ReadValue().y;
                    if (Mathf.Abs(scroll) > 0.01f)
                        targetOrtho = Mathf.Clamp(targetOrtho - Mathf.Sign(scroll) * zoomSpeed, minOrthoSize, maxOrthoSize);
                }

                // drag with middle mouse
                if (mouse.middleButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;

                    Vector3 right = transform.right;
                    Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                    float depthScale = 1f / Mathf.Sin(pitch * Mathf.Deg2Rad);

                    targetPan -= (right * d.x + forward * d.y * depthScale) * unitsPerPixel;
                    targetPan = Vector3.ClampMagnitude(targetPan, maxPanDistance);
                }
            }

            HandleClick(mouse);
        }

        float t = smoothing <= 0f ? 1f : 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        yaw = Mathf.LerpAngle(yaw, targetYaw, t);
        ortho = Mathf.Lerp(ortho, targetOrtho, t);
        panOffset = Vector3.Lerp(panOffset, targetPan, t);
        cam.orthographicSize = ortho;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = (target.position + panOffset) - rot * Vector3.forward * distance;
        transform.rotation = rot;
    }

    void HandleClick(Mouse mouse)
    {
        if (mouse.leftButton.wasPressedThisFrame)
            pressPos = mouse.position.ReadValue();

        if (!mouse.leftButton.wasReleasedThisFrame) return;

        // A drag that rotated the room shouldn't count as a click
        if (Vector2.Distance(pressPos, mouse.position.ReadValue()) > clickMaxPixels) return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        FocusObject hit = null;
        if (Physics.Raycast(ray, out RaycastHit info, 100f, clickMask))
            hit = info.collider.GetComponentInParent<FocusObject>();

        if (hit != null)
            FocusOn(hit);
        else if (IsFocused)
            ExitFocus();
    }

    public void FocusOn(FocusObject obj)
    {
        if (!IsFocused)
        {
            // Remembers where we were so we can return to it
            savedPan = targetPan;
            savedOrtho = targetOrtho;
            savedYaw = targetYaw;
        }

        focused = obj;
        targetPan = obj.FocusPosition - target.position;
        targetOrtho = obj.focusOrthoSize;
        if (obj.useFocusYaw)
            targetYaw = obj.focusYaw;
    }

    public void ExitFocus()
    {
        if (!IsFocused) return;
        focused = null;
        targetPan = savedPan;
        targetOrtho = savedOrtho;
        targetYaw = savedYaw;
    }
}