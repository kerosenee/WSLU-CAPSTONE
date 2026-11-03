using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Times are entered in the Inspector as "HH:MM:SS"
[Serializable]
public class VcrClip
{
    public string startTime;
    public VideoClip video;
    [NonSerialized] public float start; // seconds
}

[Serializable]
public class VcrStill
{
    public string startTime;
    public Texture2D image;
    [NonSerialized] public float start; // seconds
}

[Serializable]
public class VcrTape
{
    public string name;
    public string length = "19:00:00";
    public List<VcrStill> stills; // must be in time order, first one at 00:00:00
    public List<VcrClip> clips;

    [NonSerialized] public float lengthSeconds;
    [NonSerialized] public float position; // where the tape currently is, in seconds

    static float ToSeconds(string time) => (float)TimeSpan.Parse(time).TotalSeconds;

    public void Init()
    {
        lengthSeconds = ToSeconds(length);
        foreach (VcrStill still in stills) still.start = ToSeconds(still.startTime);
        foreach (VcrClip clip in clips) clip.start = ToSeconds(clip.startTime);
    }

    // The clip whose footage covers this position, or null
    public VcrClip ClipAt(float seconds)
    {
        foreach (VcrClip clip in clips)
            if (seconds >= clip.start && seconds < clip.start + (float)clip.video.length) return clip;
        return null;
    }

    // The most recent still at or before this position
    public Texture2D StillAt(float seconds)
    {
        Texture2D image = null;
        foreach (VcrStill still in stills)
            if (still.start <= seconds) image = still.image;
        return image;
    }
}

public class VcrTapePlayer : MonoBehaviour
{
    public VcrCounter counter;
    public VcrTape[] tapes;

    public RawImage overlay;
    public TMP_Text timestampText; // on-screen timestamp, put it on the overlay canvas above the RawImage

    public TMP_Text tapeInsertedText;
    public TMP_Text instructionsText; // text is blank while no tape is inserted
    public TMP_Text searchText;       // "FF" / "RW" while the tape is being wound or searched, blank otherwise
    public GameObject playIcon;       // footage corner icons: play, pause, and fast-forward (mirrored for rewind)
    public GameObject searchIcon;
    public GameObject pauseIcon;
    public VideoPlayer videoPlayer;

    // Tape seconds per real second
    public float playSpeed = 1f;
    public float searchSpeed = 60f;   // hold an arrow while playing
    public float windSpeed = 1000f;   // hold an arrow while stopped

    public float videoStartDelay = 0.5f; // seconds to show the still while a clip loads/seeks

    enum State { Stopped, Playing }

    State state;
    bool searching;
    int searchDirection; // 1 = fast-forward, -1 = rewind, 0 = not searching
    int scanDirection;   // like searchDirection, but also while stopped, and only while the tape actually moves
    bool paused;
    string instructions;
    VcrTape activeTape; // null = no tape
    VcrClip playingClip;
    bool clipStarted;
    float clipStartTime;
    RenderTexture videoTexture;

    void Awake()
    {
        foreach (VcrTape tape in tapes) tape.Init();

        videoTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoTexture;
        videoPlayer.playbackSpeed = playSpeed;

        // Keep the instructions as written in the scene, but blank until a tape is inserted
        instructions = instructionsText.text;
        instructionsText.text = "";
    }

    void Update()
    {
        // Temp keyboard stand-in for RFID reader / VCR buttons
        if (Input.GetKeyDown(KeyCode.Alpha0)) SelectTape(null);
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectTape("Tape1");
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectTape("Tape2");

        if (Input.GetKeyDown(KeyCode.P)) Play();
        if (Input.GetKeyDown(KeyCode.O)) Stop();
        if (Input.GetKeyDown(KeyCode.Space)) TogglePause();

        scanDirection = 0;
        if (activeTape != null) MoveTape();
        UpdateOverlay();
        if (searchText != null) searchText.text = scanDirection > 0 ? "FF" : scanDirection < 0 ? "RW" : "";
        searchIcon.transform.localScale = new Vector3(searchDirection < 0 ? -1f : 1f, 1f, 1f);
        playIcon.SetActive(!paused && !searching);
        searchIcon.SetActive(searching);
        pauseIcon.SetActive(paused);
        counter.Show(activeTape != null ? activeTape.position : 0f);
    }

    // tapeName null = eject
    void SelectTape(string tapeName)
    {
        Stop();
        activeTape = Array.Find(tapes, tape => tape.name == tapeName);
        if (tapeInsertedText != null)
        {
            if (activeTape != null)
            {
                tapeInsertedText.text = "[" + activeTape.name + " Inserted]";
            }
            else
            {
                tapeInsertedText.text = "[no tape inserted]";
            }
        }
        instructionsText.text = activeTape != null ? instructions : "";
        Debug.Log("Active tape: " + (activeTape != null ? activeTape.name : "none"));
    }

    public void Play()
    {
        if (!HasTape()) return;
        state = State.Playing;
        SetPaused(false); // Play also resumes a paused tape
    }

    public void Stop()
    {
        state = State.Stopped;
        paused = false;
    }

    // Freezes the tape and clip but keeps the overlay up
    public void TogglePause()
    {
        if (state == State.Playing) SetPaused(!paused);
    }

    void SetPaused(bool value)
    {
        paused = value;
        if (!clipStarted) return;
        if (paused) videoPlayer.Pause();
        else videoPlayer.Play();
    }

    bool HasTape()
    {
        if (activeTape == null) Debug.Log("No tape inserted");
        return activeTape != null;
    }

    void MoveTape()
    {
        
        // Arrows while held: search while playing, fast wind while stopped
        int direction = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
        searching = state == State.Playing && !paused && direction != 0;
        searchDirection = searching ? direction : 0;
        if (paused) return;

        float speed = direction * windSpeed;
        if (state == State.Playing) speed = searching ? direction * searchSpeed : playSpeed;

        float before = activeTape.position;
        activeTape.position = Mathf.Clamp(activeTape.position + speed * Time.deltaTime, 0f, activeTape.lengthSeconds);
        if (direction != 0 && activeTape.position != before) scanDirection = direction;

        // Ran out of tape
        bool atEnd = (speed > 0f && activeTape.position >= activeTape.lengthSeconds) || (speed < 0f && activeTape.position <= 0f);
        if (atEnd) Stop();
    }

    // Picture is only shown while playing. Clips only play at normal speed; search shows the still.
    void UpdateOverlay()
    {
        bool showing = state == State.Playing;
        overlay.gameObject.SetActive(showing);
        timestampText.gameObject.SetActive(showing);
        if (showing) timestampText.text = VcrCounter.Format(activeTape.position);
        if (!showing)
        {
            StopClip();
            return;
        }

        VcrClip clip = searching ? null : activeTape.ClipAt(activeTape.position);

        if (clip == null)
        {
            StopClip();
            overlay.texture = activeTape.StillAt(activeTape.position);
        }
        else
        {
            if (clip != playingClip)
            {
                videoPlayer.clip = clip.video;
                videoPlayer.Prepare();
                playingClip = clip;
                clipStarted = false;
            }

            // Wait until the video is loaded, then play it from the tape's position
            if (!clipStarted && !paused && videoPlayer.isPrepared)
            {
                clipStarted = true;
                clipStartTime = Time.time;
                videoPlayer.Play();
                videoPlayer.time = activeTape.position - clip.start;
                Debug.Log("Playing clip " + clip.video.name);
            }

            // The video texture is blank while loading/seeking, so keep showing the still for a moment
            bool videoReady = clipStarted && Time.time - clipStartTime >= videoStartDelay;
            overlay.texture = videoReady ? videoTexture : activeTape.StillAt(activeTape.position);
        }

        overlay.color = searching ? Color.gray : Color.white;
    }

    void StopClip()
    {
        if (playingClip == null) return;
        videoPlayer.Stop();
        playingClip = null;
        clipStarted = false;
    }
}
