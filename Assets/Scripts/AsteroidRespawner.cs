using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidRespawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnParent;
    [SerializeField] private float respawnDelayMin = 1.2f;
    [SerializeField] private float respawnDelayMax = 3.0f;

    private struct SpawnData
    {
        public AsteroidHazard hazard;
        public Vector3 localPos;
        public Vector3 localScale;
    }

    private readonly List<SpawnData> _spawnData = new();
    private int _targetCount;
    private int _alive;

    private void Awake()
    {
        if (spawnParent == null)
            spawnParent = transform;

        CacheSpawnPoints();
        _targetCount = _spawnData.Count;
        _alive = 0;
        foreach (var data in _spawnData)
        {
            if (data.hazard != null && data.hazard.gameObject.activeInHierarchy)
                _alive++;
        }
    }

    private void OnEnable()
    {
        ShipHealth.ShipDestroyed += OnShipDestroyed;
    }

    private void OnDisable()
    {
        ShipHealth.ShipDestroyed -= OnShipDestroyed;
    }

    private void CacheSpawnPoints()
    {
        _spawnData.Clear();
        var hazards = spawnParent.GetComponentsInChildren<AsteroidHazard>(true);
        foreach (var hz in hazards)
        {
            _spawnData.Add(new SpawnData
            {
                hazard = hz,
                localPos = hz.transform.localPosition,
                localScale = hz.transform.localScale
            });
        }
    }

    private void OnShipDestroyed(ShipHealth ship, int _)
    {
        if (ship == null)
            return;

        var hz = ship.GetComponent<AsteroidHazard>();
        if (hz == null)
            return; // ignore non-asteroids

        _alive = Mathf.Max(0, _alive - 1);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        if (_alive >= _targetCount)
            yield break;

        var delay = Random.Range(respawnDelayMin, respawnDelayMax);
        yield return new WaitForSeconds(delay);

        // Pick an inactive hazard to reuse.
        SpawnData? candidate = null;
        foreach (var data in _spawnData)
        {
            if (data.hazard != null && !data.hazard.gameObject.activeInHierarchy)
            {
                candidate = data;
                break;
            }
        }

        if (candidate == null)
            yield break;

        var h = candidate.Value.hazard;
        var t = h.transform;
        t.SetParent(spawnParent, false);
        t.localPosition = candidate.Value.localPos;
        t.localRotation = Random.rotation;
        t.localScale = candidate.Value.localScale;

        h.ResetForRespawn();
        h.gameObject.SetActive(true);
        _alive++;
    }
}
