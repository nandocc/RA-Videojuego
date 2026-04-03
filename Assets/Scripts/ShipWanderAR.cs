using UnityEngine;

public class ShipWanderAR : MonoBehaviour
{
    [Header("Movement Area (Local Space)")]
    [SerializeField] private Vector2 minXZ = new Vector2(-10f, -6f);
    [SerializeField] private Vector2 maxXZ = new Vector2(10f, 6f);
    [SerializeField] private bool keepInitialY = true;
    [SerializeField] private float fixedY = 1.0f;

    [Header("Motion")]
    [SerializeField] private float speed = 2.8f;
    [SerializeField] private float turnSpeed = 9f;
    [SerializeField] private float reachDistance = 0.12f;

    private Vector3 _targetLocal;
    private float _movementY;

    private void Awake()
    {
        _movementY = keepInitialY ? transform.localPosition.y : fixedY;
        PickNewTarget();
    }

    private void Update()
    {
        var current = transform.localPosition;
        var next = Vector3.MoveTowards(current, _targetLocal, speed * Time.deltaTime);
        transform.localPosition = next;

        var dir = _targetLocal - next;
        if (dir.sqrMagnitude > 0.0001f)
        {
            var lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, lookRot, turnSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(next, _targetLocal) <= reachDistance)
        {
            PickNewTarget();
        }
    }

    private void PickNewTarget()
    {
        _targetLocal = GetRandomPoint();
    }

    public Vector3 GetRandomPoint()
    {
        return new Vector3(
            Random.Range(minXZ.x, maxXZ.x),
            _movementY,
            Random.Range(minXZ.y, maxXZ.y)
        );
    }

    public void WarpToRandomPoint()
    {
        var point = GetRandomPoint();
        transform.localPosition = point;
        _targetLocal = GetRandomPoint();
    }

    public void SetMovementArea(Vector2 min, Vector2 max)
    {
        minXZ = min;
        maxXZ = max;
        _targetLocal = GetRandomPoint();
    }

    public void SetMotion(float newSpeed, float newTurnSpeed)
    {
        speed = Mathf.Max(0.1f, newSpeed);
        turnSpeed = Mathf.Max(0.1f, newTurnSpeed);
    }
}
