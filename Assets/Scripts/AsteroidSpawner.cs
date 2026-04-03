using System.Collections.Generic;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private GameObject asteroidBluePrefab;
    [SerializeField] private GameObject asteroidRedPrefab;

    [Header("Spawn")]
    [SerializeField] private bool gameplayEnabled;
    [SerializeField] private int maxAlive = 6;
    [SerializeField] private float spawnInterval = 1.1f;
    [SerializeField] private float minSpawnDistance = 2.4f;
    [SerializeField] private float maxSpawnDistance = 4.8f;
    [SerializeField] private float frontBias = 0.85f;
    [SerializeField] private bool useImageTargetBackSpawn = true;
    [SerializeField] private Vector3 laneA = new Vector3(-10.23f, 0.55f, 5.75f);
    [SerializeField] private Vector3 laneB = new Vector3(4.67f, 0.60f, 6.0f);
    [SerializeField] private Vector3 laneC = new Vector3(-2.0f, 0.50f, 6.2f);
    [SerializeField] private float laneJitter = 0.9f;

    [Header("Asteroid Stats")]
    [SerializeField] private float asteroidSpeedMin = 2.2f;
    [SerializeField] private float asteroidSpeedMax = 3.8f;
    [SerializeField] private float asteroidCollisionDamage = 20f;
    [SerializeField] private int asteroidHitPoints = 5;
    [SerializeField] private float asteroidScale = 0.12f;

    private readonly List<GameObject> _alive = new List<GameObject>();
    private float _nextSpawnTime;

    private void Update()
    {
        CleanupDestroyed();

        if (!gameplayEnabled || target == null)
            return;

        if (Time.time < _nextSpawnTime)
            return;

        if (_alive.Count >= maxAlive)
            return;

        SpawnOne();
        _nextSpawnTime = Time.time + Mathf.Max(0.1f, spawnInterval);
    }

    public void SetGameplayEnabled(bool enabled)
    {
        gameplayEnabled = enabled;
    }

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
    }

    private void SpawnOne()
    {
        var prefab = PickPrefab();
        if (prefab == null)
            return;

        var spawnPos = GetSpawnPositionAroundTarget();
        var asteroid = Instantiate(prefab, spawnPos, Random.rotation);
        if (spawnParent != null)
            asteroid.transform.SetParent(spawnParent, true);
        asteroid.name = "Hazard_" + asteroid.name;
        asteroid.transform.localScale = Vector3.one * asteroidScale;

        var hazard = asteroid.GetComponent<AsteroidHazard>();
        if (hazard == null)
            hazard = asteroid.AddComponent<AsteroidHazard>();

        var speed = Random.Range(asteroidSpeedMin, asteroidSpeedMax);
        hazard.Configure(target, speed, asteroidCollisionDamage);

        var health = asteroid.GetComponent<ShipHealth>();
        if (health == null)
            health = asteroid.AddComponent<ShipHealth>();

        // Force asteroid to require multiple hits.
        SetShipHealthForAsteroid(health);

        EnsureAsteroidCollision(asteroid);
        _alive.Add(asteroid);
    }

    private GameObject PickPrefab()
    {
        if (asteroidBluePrefab == null && asteroidRedPrefab == null)
            return null;
        if (asteroidBluePrefab != null && asteroidRedPrefab != null)
            return Random.value < 0.5f ? asteroidBluePrefab : asteroidRedPrefab;
        return asteroidBluePrefab != null ? asteroidBluePrefab : asteroidRedPrefab;
    }

    private Vector3 GetSpawnPositionAroundTarget()
    {
        if (useImageTargetBackSpawn && spawnParent != null)
            return GetSpawnFromImageTargetBack();

        var center = target.position;
        var distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        var yOffset = Random.Range(-0.25f, 0.45f);

        var basisForward = target.forward;
        var basisRight = target.right;
        var basisUp = target.up;

        var lateral = Random.Range(-0.8f, 0.8f);
        var vertical = Random.Range(-0.4f, 0.5f);
        var direction = (basisForward * Mathf.Max(0.2f, frontBias) + basisRight * lateral + basisUp * vertical).normalized;
        return center + direction * distance + basisUp * yOffset;
    }

    private Vector3 GetSpawnFromImageTargetBack()
    {
        var pick = Random.Range(0, 3);
        var baseLocal = pick == 0 ? laneA : (pick == 1 ? laneB : laneC);
        var jitter = new Vector3(
            Random.Range(-laneJitter, laneJitter),
            Random.Range(-0.25f, 0.25f),
            Random.Range(-0.35f, 0.35f));

        return spawnParent.TransformPoint(baseLocal + jitter);
    }

    private void CleanupDestroyed()
    {
        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null)
                _alive.RemoveAt(i);
        }
    }

    private void EnsureAsteroidCollision(GameObject asteroid)
    {
        if (asteroid.GetComponentInChildren<Collider>() != null)
            return;

        asteroid.AddComponent<SphereCollider>();
    }

    private void SetShipHealthForAsteroid(ShipHealth health)
    {
        health.Configure(Mathf.Max(1, asteroidHitPoints), 40);
        health.SetDestroyOnDeath(true);
        health.Respawn();
    }
}
