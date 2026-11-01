using UnityEngine;
using UnityEngine.InputSystem;

// WASD to move, mouse to look. Put on the player root with a CharacterController;
// the camera should be a child of the player.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public Transform cameraTransform;
    public float moveSpeed = 4f;
    public float lookSensitivity = 0.1f;
    public bool lookEnabled = true; // other scripts turn this off to take over the mouse

    CharacterController controller;
    float pitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        // Esc frees the cursor, clicking locks it again
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState != CursorLockMode.Locked) return;

        if (mouse != null && lookEnabled)
        {
            Vector2 look = mouse.delta.ReadValue() * lookSensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -89f, 89f);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        if (keyboard != null)
        {
            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float z = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            Vector3 move = (transform.right * x + transform.forward * z).normalized;
            controller.Move(move * moveSpeed * Time.deltaTime);
        }
    }
}
