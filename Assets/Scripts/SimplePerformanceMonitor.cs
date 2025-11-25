using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple performance monitor to check FPS and performance impact
/// </summary>
public class SimplePerformanceMonitor : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private bool showInConsole = true;
    [SerializeField] private bool showOnScreen = true;
    [SerializeField] private int fontSize = 24;

    [Header("Update Interval")]
    [SerializeField] private float updateInterval = 0.5f;

    private float fps = 0f;
    private float deltaTime = 0f;
    private float nextUpdate = 0f;

    private GUIStyle style;

    void Start()
    {
        nextUpdate = Time.time + updateInterval;
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        if (Time.time >= nextUpdate)
        {
            fps = 1.0f / deltaTime;
            nextUpdate = Time.time + updateInterval;

            if (showInConsole)
            {
                Debug.Log($"[Performance] FPS: {fps:F1}");
            }
        }
    }

    void OnGUI()
    {
        if (!showOnScreen)
            return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontSize = fontSize;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperLeft;
        }

        int w = Screen.width, h = Screen.height;
        Rect rect = new Rect(10, 10, w, h * 2 / 100);

        float ms = deltaTime * 1000.0f;
        string text = $"FPS: {fps:F1} ({ms:F1}ms)";

        // Color based on performance
        if (fps >= 60f)
            style.normal.textColor = Color.green;
        else if (fps >= 30f)
            style.normal.textColor = Color.yellow;
        else
            style.normal.textColor = Color.red;

        GUI.Label(rect, text, style);
    }
}
