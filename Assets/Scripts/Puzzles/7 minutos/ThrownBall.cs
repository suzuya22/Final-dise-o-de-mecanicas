using UnityEngine;

// Pelota que lanza el jugador en la pelea. Rompe el punto débil que golpee.
[RequireComponent(typeof(Rigidbody))]
public class ThrownBall : MonoBehaviour
{
    [Tooltip("Segundos antes de desaparecer.")]
    public float lifetime = 4f;

    Rigidbody rb;
    bool hasHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Launch(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        var weakPoint = collision.collider.GetComponentInParent<BossWeakPoint>();
        if (weakPoint == null) return;

        hasHit = true;
        weakPoint.Hit();
        Destroy(gameObject);
    }
}
