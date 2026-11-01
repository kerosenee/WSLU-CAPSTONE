using TMPro;
using UnityEngine;


[System.Serializable]
public class TimeDisplay
{
    public int maxValue;
    public int currentValue;
    public TextMeshPro textMesh;

    public void Increment()
    {
        currentValue = currentValue >= maxValue ? 0 : currentValue + 1;
        Refresh();
    }

    public void Decrement()
    {
        currentValue = currentValue <= 0 ? maxValue : currentValue - 1;
        Refresh();
    }

    public void SetActive(bool active)
    {
        textMesh.color = active ? Color.blue : Color.black;
    }

    public void Refresh()
    {
        textMesh.text = currentValue.ToString();
    }
}

public class TimestampControl : MonoBehaviour
{
    public TimeDisplay[] timeDisplays;

    // Control hint labels that light up while their key is pressed
    // This is mostly for the demo, and also a guide for where to click with Mouse-Only movement
    public TextMeshPro upText;
    public TextMeshPro downText;
    public TextMeshPro enterText;

    int activeIndex;

    void Start()
    {
        foreach (TimeDisplay display in timeDisplays)
        {
            display.Refresh();
            display.SetActive(false);
        }
        timeDisplays[activeIndex].SetActive(true);
    }

    void Update()
    {
        TimeDisplay active = timeDisplays[activeIndex];

        //Keyboard controls temp for demo
        if (Input.GetKeyDown(KeyCode.UpArrow)) active.Increment();
        if (Input.GetKeyDown(KeyCode.DownArrow)) active.Decrement();

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            active.SetActive(false);
            activeIndex = (activeIndex + 1) % timeDisplays.Length;
            timeDisplays[activeIndex].SetActive(true);
        }

        HighlightWhileHeld(upText, KeyCode.UpArrow, KeyCode.UpArrow);
        HighlightWhileHeld(downText, KeyCode.DownArrow, KeyCode.DownArrow);
        HighlightWhileHeld(enterText, KeyCode.RightArrow, KeyCode.RightArrow);
    }


    //Temp for demo
    void HighlightWhileHeld(TextMeshPro text, KeyCode keyA, KeyCode keyB)
    {
        if (Input.GetKeyDown(keyA) || Input.GetKeyDown(keyB)) text.color = Color.white;
        if (Input.GetKeyUp(keyA) || Input.GetKeyUp(keyB)) text.color = Color.black;
    }
}
