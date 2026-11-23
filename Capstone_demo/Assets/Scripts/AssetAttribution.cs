using UnityEngine;

// Add this script to external model prefabs (in folder Assets/External Models/Prefabs)to record where they came from.
// Tools > Generate Attributions collects these for prefabs used in Build Settings scenes.
public class AssetAttribution : MonoBehaviour
{
    public string title;
    public string author;
    public string sourceUrl;
    public string license;
}
