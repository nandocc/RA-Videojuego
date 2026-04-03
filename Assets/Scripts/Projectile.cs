using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetimeSeconds = 5f;
    [SerializeField] private float hitRadius = 0.2f;

    private void Start()
    {
        Destroy(gameObject, lifetimeSeconds);
    }

    private void Update()
    {
        var hits = Physics.OverlapSphere(transform.position, hitRadius, ~0, QueryTriggerInteraction.Collide);
        for (var i = 0; i < hits.Length; i++)
        {
            var health = hits[i].GetComponentInParent<ShipHealth>();
            if (health == null)
                continue;

            health.ApplyDamage(damage);
            Destroy(gameObject);
            return;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var health = collision.collider.GetComponentInParent<ShipHealth>();
        if (health == null)
            return;

        health.ApplyDamage(damage);
        Destroy(gameObject);
    }
}
