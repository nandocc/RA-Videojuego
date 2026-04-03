using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ProjectileShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private Transform cannonRoot;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject cannonVisualPrefab;
    [SerializeField] private GameObject projectileVisualPrefab;

    [Header("Projectile")]
    [SerializeField] private float projectileSpeed = 14f;
    [SerializeField] private float shotCooldown = 0.14f;
    [SerializeField] private float spawnForwardOffset = 0.32f;
    [SerializeField] private float spawnDownOffset = 0.12f;
    [SerializeField] private float projectileScale = 0.05f;
    [SerializeField] private Vector3 cannonLocalPosition = new Vector3(0f, -0.24f, 0.50f);
    [SerializeField] private Vector3 cannonLocalEuler = new Vector3(-6f, 180f, 0f);
    [SerializeField] private Vector3 cannonLocalScale = new Vector3(0.07f, 0.07f, 0.07f);
    [SerializeField] private Vector3 projectileLocalScale = new Vector3(0.08f, 0.08f, 0.08f);

    [Header("Gameplay")]
    [SerializeField] private bool gameplayEnabled;

    private float _nextShotTime;

    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        EnsureCannonVisual();
    }

    private void Update()
    {
        if (!gameplayEnabled)
            return;

        if (!TryGetShootInput(out var screenPos, out var pointerId))
            return;

        // Do not block mobile taps with UI pointer checks; this game shoots on tap anywhere.
        if (pointerId == -1 && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
            return;

        if (Time.time < _nextShotTime)
            return;

        Shoot(screenPos);
        _nextShotTime = Time.time + shotCooldown;
    }

    public void SetGameplayEnabled(bool enabled)
    {
        gameplayEnabled = enabled;
    }

    public Transform GetPlayerVisualTransform()
    {
        if (cannonRoot == null)
            EnsureCannonVisual();

        return cannonRoot != null ? cannonRoot : transform;
    }

    private void Shoot(Vector2 screenPosition)
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (arCamera == null)
            return;

        var spawn = GetProjectileSpawnPosition();
        var direction = ComputeShotDirection(screenPosition, spawn);

        var go = CreateProjectileVisual(spawn, direction);

        var rb = go.GetComponent<Rigidbody>();
        if (rb == null)
            rb = go.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = direction * projectileSpeed;

        EnsureProjectileLogic(go);
        IgnoreCollisionWithCannon(go);
        IgnoreCollisionWithCamera(go);
        SpawnMuzzleFlash(spawn);

        AudioManager.Instance?.PlayShot();
    }

    private Vector3 ComputeShotDirection(Vector2 screenPosition, Vector3 spawn)
    {
        var ray = arCamera.ScreenPointToRay(screenPosition);
        var aimPoint = ray.origin + ray.direction * 40f;

        if (Physics.Raycast(ray, out var hit, 120f, ~0, QueryTriggerInteraction.Collide))
            aimPoint = hit.point;

        var direction = (aimPoint - spawn).normalized;
        if (direction.sqrMagnitude < 0.0001f)
            direction = ray.direction.normalized;

        return direction;
    }

    private Vector3 GetProjectileSpawnPosition()
    {
        if (muzzlePoint != null)
            return muzzlePoint.position;

        return arCamera.transform.position
            + arCamera.transform.forward * spawnForwardOffset
            + Vector3.down * spawnDownOffset;
    }

    private void EnsureCannonVisual()
    {
        if (arCamera == null)
            return;

        if (cannonRoot != null && muzzlePoint != null)
            return;

        var existing = arCamera.transform.Find("FPCannonRoot");
        if (existing != null)
        {
            cannonRoot = existing;
            var existingMuzzle = existing.Find("MuzzlePoint");
            if (existingMuzzle != null)
                muzzlePoint = existingMuzzle;
            ApplyCannonPose(cannonRoot);
            DisableCannonColliders(cannonRoot);
            return;
        }

        if (cannonVisualPrefab != null)
        {
            var visual = Instantiate(cannonVisualPrefab, arCamera.transform);
            visual.name = "FPCannonRoot";
            ApplyCannonPose(visual.transform);
            cannonRoot = visual.transform;
            DisableCannonColliders(cannonRoot);

            var muzzleExisting = cannonRoot.Find("MuzzlePoint");
            if (muzzleExisting != null)
            {
                muzzlePoint = muzzleExisting;
                return;
            }

            var autoMuzzle = new GameObject("MuzzlePoint").transform;
            autoMuzzle.SetParent(cannonRoot, false);
            autoMuzzle.localPosition = new Vector3(0f, 0f, 0.35f);
            muzzlePoint = autoMuzzle;
            return;
        }

        var root = new GameObject("FPCannonRoot").transform;
        root.SetParent(arCamera.transform, false);
        root.localPosition = new Vector3(0f, -0.22f, 0.45f);
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "CannonBase";
        baseObj.transform.SetParent(root, false);
        baseObj.transform.localPosition = new Vector3(0f, -0.02f, -0.02f);
        baseObj.transform.localScale = new Vector3(0.16f, 0.08f, 0.16f);

        var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "CannonBarrel";
        barrel.transform.SetParent(root, false);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrel.transform.localPosition = new Vector3(0f, 0.02f, 0.13f);
        barrel.transform.localScale = new Vector3(0.05f, 0.16f, 0.05f);

        var muzzleGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        muzzleGlow.name = "MuzzleGlow";
        muzzleGlow.transform.SetParent(root, false);
        muzzleGlow.transform.localPosition = new Vector3(0f, 0.02f, 0.30f);
        muzzleGlow.transform.localScale = Vector3.one * 0.04f;

        var matColor = new Color(0.25f, 0.28f, 0.35f, 1f);
        SetRendererColor(baseObj, matColor);
        SetRendererColor(barrel, new Color(0.18f, 0.20f, 0.26f, 1f));
        SetRendererColor(muzzleGlow, Color.cyan);

        Destroy(baseObj.GetComponent<Collider>());
        Destroy(barrel.GetComponent<Collider>());
        Destroy(muzzleGlow.GetComponent<Collider>());

        var muzzle = new GameObject("MuzzlePoint").transform;
        muzzle.SetParent(root, false);
        muzzle.localPosition = new Vector3(0f, 0.02f, 0.32f);
        muzzle.localRotation = Quaternion.identity;

        cannonRoot = root;
        muzzlePoint = muzzle;
        ApplyCannonPose(cannonRoot);
        DisableCannonColliders(cannonRoot);
    }

    private void ApplyCannonPose(Transform targetRoot)
    {
        if (targetRoot == null)
            return;

        targetRoot.localPosition = cannonLocalPosition;
        targetRoot.localRotation = Quaternion.Euler(cannonLocalEuler);
        targetRoot.localScale = cannonLocalScale;
    }

    private GameObject CreateProjectileVisual(Vector3 spawn, Vector3 direction)
    {
        GameObject go;
        if (projectileVisualPrefab != null)
        {
            go = Instantiate(projectileVisualPrefab);
            go.name = "Projectile";
            go.transform.position = spawn;
            go.transform.rotation = Quaternion.LookRotation(direction, arCamera.transform.up);
            go.transform.localScale = projectileLocalScale;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            go.transform.position = spawn;
            go.transform.rotation = Quaternion.LookRotation(direction, arCamera.transform.up);
            go.transform.localScale = Vector3.one * projectileScale;
            SetRendererColor(go, Color.cyan);
        }

        return go;
    }

    private static void EnsureProjectileLogic(GameObject go)
    {
        var projectile = go.GetComponent<Projectile>();
        if (projectile == null)
            projectile = go.AddComponent<Projectile>();

        var hasCollider = go.GetComponentInChildren<Collider>() != null;
        if (!hasCollider)
            go.AddComponent<SphereCollider>();

        var trail = go.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.startWidth = 0.05f;
            trail.endWidth = 0.01f;
            trail.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            trail.startColor = new Color(1f, 0.55f, 0.1f, 1f);
            trail.endColor = new Color(1f, 0.1f, 0.0f, 0f);
        }
    }

    private void IgnoreCollisionWithCannon(GameObject projectile)
    {
        if (projectile == null || cannonRoot == null)
            return;

        var projectileColliders = projectile.GetComponentsInChildren<Collider>(true);
        var cannonColliders = cannonRoot.GetComponentsInChildren<Collider>(true);
        for (var i = 0; i < projectileColliders.Length; i++)
        {
            for (var j = 0; j < cannonColliders.Length; j++)
            {
                Physics.IgnoreCollision(projectileColliders[i], cannonColliders[j], true);
            }
        }
    }

    private void IgnoreCollisionWithCamera(GameObject projectile)
    {
        if (projectile == null || arCamera == null)
            return;

        var projectileColliders = projectile.GetComponentsInChildren<Collider>(true);
        var camColliders = arCamera.GetComponentsInChildren<Collider>(true);
        for (var i = 0; i < projectileColliders.Length; i++)
        {
            for (var j = 0; j < camColliders.Length; j++)
            {
                Physics.IgnoreCollision(projectileColliders[i], camColliders[j], true);
            }
        }
    }

    private void SpawnMuzzleFlash(Vector3 position)
    {
        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "MuzzleFlash";
        flash.transform.position = position;
        flash.transform.localScale = Vector3.one * 0.05f;
        SetRendererColor(flash, new Color(1f, 0.7f, 0.2f, 1f));
        Destroy(flash.GetComponent<Collider>());
        Destroy(flash, 0.06f);
    }

    private static void SetRendererColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.material.color = color;
    }

    private static void DisableCannonColliders(Transform root)
    {
        if (root == null)
            return;

        var colliders = root.GetComponentsInChildren<Collider>(true);
        for (var i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private static bool TryGetShootInput(out Vector2 screenPosition, out int pointerId)
    {
        screenPosition = default;
        pointerId = -1;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            pointerId = -1;
            return true;
        }

        return false;
    }
}
