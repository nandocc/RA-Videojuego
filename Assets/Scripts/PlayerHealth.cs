using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public event Action<float, float> OnHealthChanged;
    public event Action OnPlayerDied;

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        NotifyHealthChanged();
    }

    public void ApplyDamage(float damage)
    {
        if (IsDead)
            return;

        var previousHealth = currentHealth;
        currentHealth -= Mathf.Max(0f, damage);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (currentHealth < previousHealth && currentHealth > 0f)
            AudioManager.Instance?.PlayPlayerHit();

        NotifyHealthChanged();

        if (currentHealth <= 0f)
        {
            AudioManager.Instance?.PlayPlayerDeath();
            OnPlayerDied?.Invoke();
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
