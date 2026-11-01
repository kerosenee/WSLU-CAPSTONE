using UnityEngine;
using UnityEngine.InputSystem;

// Hold left mouse on a Grabbable to carry it in front of the camera, release to drop.
// While holding, hold R and move the mouse to rotate the object.
// Put on the camera.
public class HoldInteractor : MonoBehaviour
{
    public FirstPersonController player;
    public float reach = 3f;
    public float holdDistance = 1.5f;
    public float followStrength = 15f;
    public float rotateSensitivity = 0.3f;

    Rigidbody held;
    Collider[] heldColliders;
    Collider[] playerColliders;
    Quaternion heldRotation; // relative to the camera

    void Awake()
    {
        playerColliders = player.GetComponentsInChildren<Collider>();
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null) return;

        if (held == null && mouse.leftButton.wasPressedThisFrame) TryGrab();
        if (held != null && mouse.leftButton.wasReleasedThisFrame) Drop();

        // R + mouse rotates the held object instead of the camera
        bool rotating = held != null && keyboard != null && keyboard.rKey.isPressed;
        player.lookEnabled = !rotating;
        if (rotating)
        {
            Vector2 d = mouse.delta.ReadValue() * rotateSensitivity;
            heldRotation = Quaternion.AngleAxis(-d.x, Vector3.up) * Quaternion.AngleAxis(d.y, Vector3.right) * heldRotation;
        }
    }

    void FixedUpdate()
    {
        if (held == null) return;

        // Velocity-driven so the object still collides with walls instead of clipping through
        Vector3 target = transform.position + transform.forward * holdDistance;
        held.linearVelocity = (target - held.position) * followStrength;
        held.angularVelocity = Vector3.zero;
        held.MoveRotation(transform.rotation * heldRotation);
    }

    void TryGrab()
    {
        if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, reach)) return;

        Grabbable grabbable = hit.collider.GetComponentInParent<Grabbable>();
        if (grabbable == null) return;

        held = grabbable.GetComponent<Rigidbody>();
        held.useGravity = false;
        heldRotation = Quaternion.Inverse(transform.rotation) * held.rotation;

        // Don't let the carried object shove the player around
        heldColliders = held.GetComponentsInChildren<Collider>();
        SetIgnorePlayer(true);
    }

    void Drop()
    {
        SetIgnorePlayer(false);
        held.useGravity = true;
        held = null;
        player.lookEnabled = true;
    }

    void SetIgnorePlayer(bool ignore)
    {
        foreach (Collider a in heldColliders)
            foreach (Collider b in playerColliders)
                Physics.IgnoreCollision(a, b, ignore);
    }
}
