using System;
using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    public static event Action<ShipHealth, int> ShipDestroyed;

    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int scoreValue = 100;
    [SerializeField] private bool destroyOnDeath;
    [SerializeField] private HitFlash hitFlash;

    private int _currentHealth;
    private bool _isDead;

    private void Awake()
    {
        _currentHealth = Mathf.Max(1, maxHealth);
        EnsureCollider();
        if (hitFlash == null)
            hitFlash = GetComponent<HitFlash>();
    }

    public void ApplyDamage(int amount)
    {
        if (_isDead)
            return;

        _currentHealth -= Mathf.Max(1, amount);
        hitFlash?.Play();
        if (_currentHealth > 0)
            return;

        _isDead = true;
        ShipDestroyed?.Invoke(this, scoreValue);
        if (destroyOnDeath)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    public void Respawn()
    {
        _isDead = false;
        _currentHealth = Mathf.Max(1, maxHealth);
        gameObject.SetActive(true);
    }

    public void Configure(int newMaxHealth, int newScoreValue)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        scoreValue = Mathf.Max(0, newScoreValue);
        _currentHealth = maxHealth;
        _isDead = false;
    }

    public void SetDestroyOnDeath(bool shouldDestroy)
    {
        destroyOnDeath = shouldDestroy;
    }

    private void EnsureCollider()
    {
        // Do not add colliders to the AR camera (player root).
        if (GetComponent<Camera>() != null)
            return;

        var hasCollider = GetComponentInChildren<Collider>() != null;
        if (hasCollider)
            return;

        var col = gameObject.AddComponent<SphereCollider>();
        col.radius = 0.35f;
        col.isTrigger = false;
    }
}
