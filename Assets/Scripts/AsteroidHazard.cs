using UnityEngine;

public class AsteroidHazard : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeed = 2.8f;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float collisionDamage = 20f;
    [SerializeField] private float impactDistance = 0.32f;
    [SerializeField] private float maxLifeSeconds = 14f;
    [SerializeField] private Vector2 launchDelayRange = new Vector2(0.3f, 1.4f);
    [SerializeField] private Vector2 launchGapRange = new Vector2(0.55f, 1.1f);
    [SerializeField] private Vector2 speedVariance = new Vector2(0.85f, 1.2f);
    [SerializeField] private float lateralWobble = 0.28f;

    private float _spawnTime;
    private bool _consumed;
    private float _launchAt;
    private bool _launchScheduled;
    private float _speedMul = 1f;
    private float _wobbleSeed;
    private static float s_nextGlobalLaunchAt;
    private bool _visible;
    private Renderer[] _renderers;
    private Collider[] _colliders;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        ResetForRespawn();
    }

    private void Update()
    {
        if (!ARGameFlowManager.AsteroidThreatActive)
            return;

        if (!_launchScheduled)
        {
            var ownDelay = Random.Range(launchDelayRange.x, launchDelayRange.y);
            var desiredLaunch = Time.time + ownDelay;
            var forcedGapLaunch = s_nextGlobalLaunchAt + Random.Range(launchGapRange.x, launchGapRange.y);
            _launchAt = Mathf.Max(desiredLaunch, forcedGapLaunch);
            s_nextGlobalLaunchAt = _launchAt;
            _launchScheduled = true;
        }

        if (Time.time < _launchAt)
            return;

        if (!_visible)
            SetVisible(true);

        if (target == null)
            TryResolveTarget();

        if (target == null)
            return;

        var toTarget = target.position - transform.position;
        if (toTarget.sqrMagnitude <= impactDistance * impactDistance)
        {
            var playerHealth = target.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.ApplyDamage(collisionDamage);

            _consumed = true;
            gameObject.SetActive(false);
            return;
        }

        var direction = toTarget.normalized;
        var wobble = Mathf.Sin((Time.time + _wobbleSeed) * 2.1f) * lateralWobble;
        direction = (direction + transform.right * wobble * 0.1f).normalized;

        transform.position += direction * (moveSpeed * _speedMul) * Time.deltaTime;
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        transform.Rotate(Vector3.right, rotationSpeed * 0.6f * Time.deltaTime, Space.Self);

        if (Time.time - _spawnTime > maxLifeSeconds)
            gameObject.SetActive(false);
    }

    private void TryResolveTarget()
    {
        if (target != null)
            return;

        var health = FindFirstObjectByType<PlayerHealth>();
        if (health != null)
        {
            target = health.transform;
            return;
        }

        if (Camera.main != null)
            target = Camera.main.transform;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!ARGameFlowManager.AsteroidThreatActive)
            return;

        if (_consumed)
            return;

        var playerHealth = collision.collider.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            return;

        playerHealth.ApplyDamage(collisionDamage);
        _consumed = true;
        gameObject.SetActive(false);
    }

    public void Configure(Transform targetTransform, float speed, float damage)
    {
        target = targetTransform;
        moveSpeed = Mathf.Max(0.2f, speed);
        collisionDamage = Mathf.Max(1f, damage);
    }

    public static void ResetLaunchScheduler()
    {
        s_nextGlobalLaunchAt = 0f;
    }

    private void EnsureHealth()
    {
        var health = GetComponent<ShipHealth>();
        if (health == null)
            health = gameObject.AddComponent<ShipHealth>();

        health.Configure(5, 100);
        health.SetDestroyOnDeath(false);
        health.Respawn();
    }

    private void EnsureCollider()
    {
        // Use a simple sphere collider big enough to get hit by small projectiles.
        var col = GetComponent<SphereCollider>();
        if (col == null)
            col = gameObject.AddComponent<SphereCollider>();

        col.isTrigger = false;
        col.radius = 0.35f;
        col.center = Vector3.zero;
    }

    public void ResetForRespawn()
    {
        _spawnTime = Time.time;
        _consumed = false;
        _launchScheduled = false;
        _speedMul = Random.Range(speedVariance.x, speedVariance.y);
        _wobbleSeed = Random.Range(0f, 100f);
        SetVisible(false);
        TryResolveTarget();
        EnsureHealth();
        EnsureCollider();
    }

    private void SetVisible(bool state)
    {
        _visible = state;
        if (_renderers != null)
        {
            foreach (var r in _renderers)
            {
                if (r != null)
                    r.enabled = state;
            }
        }

        if (_colliders != null)
        {
            foreach (var c in _colliders)
            {
                if (c != null)
                    c.enabled = state;
            }
        }
    }
}
