using UnityEngine;
using Vuforia;
using System;
using UnityEngine.InputSystem;

public class VuforiaGroundPlaneFallback : MonoBehaviour
{
    public event Action OnPlacementApplied;

    [Header("References")]
    [SerializeField] private AnchorBehaviour anchorBehaviour;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Camera arCamera;

    [Header("Tap To Place")]
    [SerializeField] private bool enableTapToPlace = true;
    [SerializeField] private bool allowRepositionOnTap = true;
    [SerializeField] private float tapRayMaxDistance = 8f;

    [Header("Fallback")]
    [SerializeField] private bool autoPlaceAfterDelay = false;
    [SerializeField] private float fallbackDelaySeconds = 3f;
    [SerializeField] private float fallbackDistance = 1.2f;
    [SerializeField] private bool placeOnEstimatedFloor = true;
    [SerializeField] private float assumedCameraHeightMeters = 1.35f;
    [SerializeField] private float fallbackVerticalOffset = -0.2f;

    private float _startTime;
    private bool _fallbackApplied;
    public bool HasPlacement => _fallbackApplied;

    private void Awake()
    {
        if (anchorBehaviour == null)
            anchorBehaviour = GetComponent<AnchorBehaviour>();

        if (contentRoot == null)
        {
            var candidate = transform.Find("Diorama_Holograma");
            contentRoot = candidate != null ? candidate : transform;
        }

        if (arCamera == null)
            arCamera = Camera.main;

        _startTime = Time.time;
    }

    private void Update()
    {
        HandleTapToPlace();

        if (_fallbackApplied)
            return;

        if (HasStableTracking())
            return;

        if (!autoPlaceAfterDelay)
            return;

        if (Time.time - _startTime < fallbackDelaySeconds)
            return;

        ApplyFallbackPlacementInternal();
    }

    private void HandleTapToPlace()
    {
        if (!enableTapToPlace)
            return;

        if (_fallbackApplied && !allowRepositionOnTap)
            return;

        if (!TryGetTapPosition(out var tapPosition))
            return;

        ApplyTapPlacement(tapPosition);
    }

    private bool HasStableTracking()
    {
        if (anchorBehaviour == null)
            return false;

        var status = anchorBehaviour.TargetStatus.Status;
        return status == Status.TRACKED || status == Status.EXTENDED_TRACKED;
    }

    private void ApplyFallbackPlacementInternal()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (arCamera == null || contentRoot == null)
            return;

        // Disable Vuforia's visibility gate when the device never reaches tracked state.
        DisableDefaultObserverEventHandlerInternal();

        var camTransform = arCamera.transform;
        var targetPosition = ComputeFallbackPosition(camTransform);

        contentRoot.position = targetPosition;

        var forward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude > 0.0001f)
            contentRoot.rotation = Quaternion.LookRotation(forward, Vector3.up);

        FinalizePlacement();

        Debug.Log("[VuforiaGroundPlaneFallback] Fallback placement applied.");
    }

    private void ApplyTapPlacement(Vector2 screenPosition)
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (arCamera == null || contentRoot == null)
            return;

        DisableDefaultObserverEventHandlerInternal();

        var camTransform = arCamera.transform;
        var targetPosition = ComputeTapPlacementPosition(screenPosition, camTransform);

        contentRoot.position = targetPosition;

        var forward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude > 0.0001f)
            contentRoot.rotation = Quaternion.LookRotation(forward, Vector3.up);

        FinalizePlacement();
    }

    private Vector3 ComputeFallbackPosition(Transform camTransform)
    {
        var cameraPosition = camTransform.position;
        var floorY = cameraPosition.y - Mathf.Max(0.3f, assumedCameraHeightMeters);

        if (placeOnEstimatedFloor && TryGetFloorIntersection(camTransform, floorY, out var floorHitPoint))
            return floorHitPoint;

        var horizontalForward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
        if (horizontalForward.sqrMagnitude < 0.0001f)
            horizontalForward = Vector3.forward;

        var fallback = cameraPosition + horizontalForward * fallbackDistance + Vector3.up * fallbackVerticalOffset;
        if (placeOnEstimatedFloor)
            fallback.y = floorY;

        return fallback;
    }

    private Vector3 ComputeTapPlacementPosition(Vector2 screenPosition, Transform camTransform)
    {
        var cameraPosition = camTransform.position;
        var floorY = cameraPosition.y - Mathf.Max(0.3f, assumedCameraHeightMeters);

        if (placeOnEstimatedFloor)
        {
            var ray = arCamera.ScreenPointToRay(screenPosition);
            if (TryRaycastToFloor(ray, floorY, out var point))
                return point;
        }

        return ComputeFallbackPosition(camTransform);
    }

    private bool TryRaycastToFloor(Ray ray, float floorY, out Vector3 point)
    {
        point = default;

        if (Mathf.Abs(ray.direction.y) < 0.0001f)
            return false;

        var distance = (floorY - ray.origin.y) / ray.direction.y;
        if (distance <= 0.1f || distance > tapRayMaxDistance)
            return false;

        point = ray.origin + ray.direction * distance;
        return true;
    }

    private bool TryGetFloorIntersection(Transform camTransform, float floorY, out Vector3 point)
    {
        point = default;

        var direction = camTransform.forward;
        if (direction.y >= -0.05f)
            return false;

        var distance = (floorY - camTransform.position.y) / direction.y;
        if (distance <= 0.1f || distance > 10f)
            return false;

        point = camTransform.position + direction * distance;
        return true;
    }

    private static bool TryGetTapPosition(out Vector2 screenPosition)
    {
        screenPosition = default;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    private void DisableDefaultObserverEventHandlerInternal()
    {
        var component = GetComponent("DefaultObserverEventHandler") as Behaviour;
        if (component != null)
            component.enabled = false;
    }

    public bool PlaceAtScreenCenter()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (arCamera == null)
            return false;

        var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        return PlaceAtScreenPosition(center);
    }

    public bool PlaceAtScreenPosition(Vector2 screenPosition)
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (arCamera == null)
            return false;

        ApplyTapPlacement(screenPosition);
        return _fallbackApplied;
    }

    private void FinalizePlacement()
    {
        SetChildRenderersEnabled(contentRoot, true);
        var wasPlaced = _fallbackApplied;
        _fallbackApplied = true;
        if (!wasPlaced)
            OnPlacementApplied?.Invoke();
    }

    private static void SetChildRenderersEnabled(Transform root, bool enabled)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
            renderer.enabled = enabled;
    }
}
