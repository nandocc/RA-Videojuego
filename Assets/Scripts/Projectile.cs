using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetimeSeconds = 5f;
    [SerializeField] private float hitRadius = 0.2f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private int overlapBufferSize = 12;

    private Collider[] _overlapBuffer;
    private bool _consumed;

    private void Start()
    {
        _overlapBuffer = new Collider[Mathf.Max(4, overlapBufferSize)];
        Destroy(gameObject, lifetimeSeconds);
    }

    private void Update()
    {
        if (_consumed)
            return;

        var hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            hitRadius,
            _overlapBuffer,
            targetLayers,
            QueryTriggerInteraction.Collide);

        for (var i = 0; i < hitCount; i++)
        {
            var hit = _overlapBuffer[i];
            if (hit == null)
                continue;

            var health = hit.GetComponentInParent<ShipHealth>();
            if (health == null)
                continue;

            health.ApplyDamage(damage);
            _consumed = true;
            Destroy(gameObject);
            return;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_consumed)
            return;

        var health = collision.collider.GetComponentInParent<ShipHealth>();
        if (health == null)
            return;

        health.ApplyDamage(damage);
        _consumed = true;
        Destroy(gameObject);
    }
}
