using UnityEngine;

// Evita que la cámara en tercera persona atraviese paredes: si hay algo entre el jugador
// y la cámara, la acerca. Va en la cámara del jugador (hija del jugador).
public class CameraCollision : MonoBehaviour
{
    [Tooltip("Desde dónde se comprueba (la cabeza del jugador).")]
    public Transform target;
    [Tooltip("Grosor de la comprobación, para que no se vea a través de las esquinas.")]
    public float radius = 0.2f;
    [Tooltip("Distancia mínima al jugador.")]
    public float minDistance = 0.3f;
    public LayerMask collisionMask = ~0;

    Vector3 defaultLocalPosition;

    void Start()
    {
        defaultLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (target == null || transform.parent == null) return;

        Vector3 desired = transform.parent.TransformPoint(defaultLocalPosition);
        Vector3 dir = desired - target.position;
        float dist = dir.magnitude;
        if (dist < 0.001f) return;
        dir /= dist;

        if (Physics.SphereCast(target.position, radius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            transform.position = target.position + dir * Mathf.Max(minDistance, hit.distance);
        else
            transform.localPosition = defaultLocalPosition;
    }
}
