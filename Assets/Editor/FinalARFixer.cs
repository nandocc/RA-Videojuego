using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class FinalARFixer : EditorWindow
{
    [MenuItem("Tools/AR/Aplicar Arreglos Finales P30 Lite")]
    public static void Execute()
    {
        // 1. REPARAR EL EVENT SYSTEM (ELIMINAR EL ERROR ROJO)
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es != null)
        {
            var old = es.GetComponent<StandaloneInputModule>();
            if (old != null)
            {
                DestroyImmediate(old);
                es.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("- EventSystem: Corregido a InputSystemUIInputModule.");
            }
        }

        // 2. AÑADIR EL DIAGNÓSTICO (PARA VER QUÉ PASA EN EL CELULAR)
        var cam = GameObject.Find("ARCamera");
        if (cam != null)
        {
            if (cam.GetComponent<ARDiagnostics>() == null)
            {
                cam.AddComponent<ARDiagnostics>();
                Debug.Log("- ARCamera: Añadido script de diagnóstico ARDiagnostics.");
            }
        }
        else
        {
            Debug.LogWarning("No se encontró ARCamera en la escena.");
        }

        Debug.Log("¡Arreglos finales aplicados correctamente sin mover tus objetos!");
        EditorUtility.DisplayDialog("Arreglo AR", "Se reparó el Input System y se añadió el rastreador de debug.\nYa puedes hacer el Build Final.", "OK");
    }
}
