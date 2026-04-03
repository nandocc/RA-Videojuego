using UnityEngine;
using Vuforia;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;

public class ARDiagnostics : MonoBehaviour
{
    private TextMeshProUGUI debugText;
    private int tapCount = 0;

    void Start()
    {
        // 1. REPARACIÓN AUTOMÁTICA DEL INPUT SYSTEM (SIN TOCAR LA ESCENA)
        FixEventSystem();

        // 2. BUSCAR O CREAR CANVAS DE DEBUG
        SetupDebugUI();
    }

    void FixEventSystem()
    {
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es != null)
        {
            var old = es.GetComponent<StandaloneInputModule>();
            if (old != null)
            {
                Destroy(old);
                es.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[ARDiagnostics] EventSystem corregido a InputSystemUIInputModule.");
            }
        }
    }

    void SetupDebugUI()
    {
        var canvas = GameObject.Find("AR_Canvas");
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("DebugCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            canvas = canvasGO;
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }

        GameObject debugGO = new GameObject("DebugText", typeof(RectTransform));
        debugGO.transform.SetParent(canvas.transform);
        debugText = debugGO.AddComponent<TextMeshProUGUI>();
        
        debugText.fontSize = 24;
        debugText.color = Color.yellow;
        debugText.alignment = TextAlignmentOptions.BottomLeft;
        
        RectTransform rt = debugText.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(20, 20);
        rt.sizeDelta = new Vector2(600, 100);
    }

    void Update()
    {
        if (debugText == null) return;

        // VERIFICAR ESTADO DE RASTREO
        var status = "Iniciando...";
        if (VuforiaBehaviour.Instance != null)
        {
            status = VuforiaBehaviour.Instance.enabled ? "Cámara AR Activa" : "Desactivado";
        }

        // VERIFICAR TOQUES
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            tapCount++;
        }

        debugText.text = $"Estado: {status}\nToques Detectados: {tapCount}\nDispositivo: {SystemInfo.deviceModel}";
    }
}
