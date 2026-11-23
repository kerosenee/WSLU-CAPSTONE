using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WallFader : MonoBehaviour
{
    public bool insideFacesForward = true;
    [Range(-1f, 1f)] public float hideThreshold = 0.1f;  
    public Camera cam;

    Renderer[] renderers;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        Vector3 inside = insideFacesForward ? transform.forward : -transform.forward;

        // If the camera is looking at the wall's outside, the wall is between camera and the room
        //Camera forward pointing in the same direction as the inside normal which means camera is outside the wall
        float facing = Vector3.Dot(cam.transform.forward, inside);
        bool shouldHide = facing > hideThreshold;

        foreach (var r in renderers)
            r.enabled = !shouldHide;
    }
}