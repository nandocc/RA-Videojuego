using UnityEngine;
using Vuforia;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;

public class ARDiagnostics : MonoBehaviour
{
    [SerializeField] private bool enableDiagnostics = false;

    private TextMeshProUGUI debugText;
    private int tapCount;

    private void Start()
    {
        if (!enableDiagnostics)
        {
            enabled = false;
            return;
        }

        FixEventSystem();
        SetupDebugUI();
    }

    private void FixEventSystem()
    {
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
            return;

        var old = es.GetComponent<StandaloneInputModule>();
        if (old == null)
            return;

        Destroy(old);
        es.gameObject.AddComponent<InputSystemUIInputModule>();
        Debug.Log("[ARDiagnostics] EventSystem switched to InputSystemUIInputModule.");
    }

    private void SetupDebugUI()
    {
        var canvas = GameObject.Find("AR_Canvas");
        if (canvas == null)
        {
            var canvasGO = new GameObject("DebugCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            canvas = canvasGO;
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }

        var debugGO = new GameObject("DebugText", typeof(RectTransform));
        debugGO.transform.SetParent(canvas.transform, false);
        debugText = debugGO.AddComponent<TextMeshProUGUI>();

        debugText.fontSize = 24;
        debugText.color = Color.yellow;
        debugText.alignment = TextAlignmentOptions.BottomLeft;

        var rt = debugText.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(20, 20);
        rt.sizeDelta = new Vector2(600, 100);
    }

    private void Update()
    {
        if (debugText == null)
            return;

        var status = "Starting...";
        if (VuforiaBehaviour.Instance != null)
            status = VuforiaBehaviour.Instance.enabled ? "AR Camera Active" : "Disabled";

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            tapCount++;

        debugText.text = $"State: {status}\nTaps: {tapCount}\nDevice: {SystemInfo.deviceModel}";
    }
}
