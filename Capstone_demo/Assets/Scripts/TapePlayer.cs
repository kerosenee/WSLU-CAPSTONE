using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Something that plays when the tape is at a specific timestamp
[System.Serializable]
public class TapeClip
{
    public string timestamp; // digits only, e.g. "075420" for 07:54:20
    public VideoClip video;
}

[System.Serializable]
public class Tape
{
    public string name;
    public Texture2D still; // shown at any timestamp without a clip
    public List<TapeClip> clips;

    public VideoClip FindClip(string timestamp)
    {
        foreach (TapeClip clip in clips)
            if (clip.timestamp == timestamp) return clip.video;
        return null;
    }
}

public class TapePlayer : MonoBehaviour
{
    public TimestampControl timestampControl;
    public Tape[] tapes;

    public RawImage overlay;
    public VideoPlayer videoPlayer;

    Tape activeTape; // null = no tape
    RenderTexture videoTexture;

    void Awake()
    {
        videoTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoTexture;
        overlay.gameObject.SetActive(false);
    }

    void Update()
    {
        // Temp keyboard stand-in for RFID reader / VCR button
        if (Input.GetKeyDown(KeyCode.Alpha0)) SelectTape(null);
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectTape("Tape1");
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectTape("Tape2");

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (overlay.gameObject.activeSelf) HideOverlay();
            else ShowOverlay();
        }
    }

    // tapeName null = eject
    void SelectTape(string tapeName)
    {
        HideOverlay();
        activeTape = System.Array.Find(tapes, tape => tape.name == tapeName);
        Debug.Log("Active tape: " + (activeTape != null ? activeTape.name : "none"));
    }

    void ShowOverlay()
    {
        if (activeTape == null)
        {
            Debug.Log("No tape inserted");
            return;
        }

        string timestamp = timestampControl.GetTimestamp();
        VideoClip clip = activeTape.FindClip(timestamp);

        if (clip != null)
        {
            videoPlayer.clip = clip;
            videoPlayer.Play();
            overlay.texture = videoTexture;
        }
        else
        {
            overlay.texture = activeTape.still;
        }

        Debug.Log($"Playing {activeTape.name} at {timestamp}: " + (clip != null ? clip.name : "still"));
        overlay.gameObject.SetActive(true);
    }

    void HideOverlay()
    {
        videoPlayer.Stop();
        overlay.gameObject.SetActive(false);
    }
}
