using System;
using TMPro;
using UnityEngine;

// One TMP object and how many digits of HHMMSS it shows (1 for tens/ones, 2 if merged)
[Serializable]
public class VcrCounterDisplay
{
    public TextMeshPro text;
    public int digits = 1;
}

// Read-only tape counter. Displays seconds as HH:MM:SS across the displays, in order.
public class VcrCounter : MonoBehaviour
{
    public VcrCounterDisplay[] displays; // HR_tens, HR_ones, MIN_tens, MIN_ones, SEC_tens, SEC_ones

    public static string Format(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
    }

    public void Show(float seconds)
    {
        string digits = Format(seconds).Replace(":", "");

        int index = 0;
        foreach (VcrCounterDisplay display in displays)
        {
            display.text.text = digits.Substring(index, display.digits);
            index += display.digits;
        }
    }
}
