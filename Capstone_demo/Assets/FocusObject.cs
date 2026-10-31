using UnityEngine;

public class FocusObject : MonoBehaviour
{
    public Transform focusPoint;

    [Tooltip("Smaller = closer zoom.")]
    public float focusOrthoSize = 1.5f;

    [Header("Optional angle")]
    [Tooltip("Turn the view to a specific angle when focusing")]
    public bool useFocusYaw = false;
    public float focusYaw = 45f;

    public Vector3 FocusPosition => focusPoint != null ? focusPoint.position : transform.position;
}
