using UnityEngine;
using UnityEngine.InputSystem;

// Clic izquierdo lanza una pelota hacia donde está el mouse en pantalla
// (con Aim Camera asignada) o, si no, hacia donde mira el Throw Point.
// Empieza desactivado; la pelea con la persona lo activa.
public class BallThrower : MonoBehaviour
{
    [Header("Referencias")]
    public ThrownBall ballPrefab;
    [Tooltip("Desde dónde sale la pelota (por ejemplo un objeto vacío a la altura de la mano).")]
    public Transform throwPoint;
    [Tooltip("Cámara del jugador: la pelota va hacia el punto de la pantalla donde se hace clic.")]
    public Camera aimCamera;

    [Header("Lanzamiento")]
    public float throwSpeed = 9f;
    [Tooltip("Elevación del tiro en grados cuando no se apunta con el mouse.")]
    public float throwAngle = 12f;
    [Tooltip("Segundos entre lanzamientos.")]
    public float cooldown = 0.6f;
    [Tooltip("Distancia máxima a la que se apunta si el mouse no señala nada.")]
    public float maxAimDistance = 20f;

    float nextThrowTime;
    Collider ownCollider;

    void Awake()
    {
        ownCollider = GetComponent<Collider>();
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (Time.time < nextThrowTime || ballPrefab == null || throwPoint == null) return;

        nextThrowTime = Time.time + cooldown;
        Vector3 velocity = aimCamera != null
            ? AimAt(GetMouseTarget(mouse.position.ReadValue()))
            : Quaternion.AngleAxis(-throwAngle, throwPoint.right) * throwPoint.forward * throwSpeed;

        var ball = Instantiate(ballPrefab, throwPoint.position, throwPoint.rotation);

        // Que la pelota no choque con el propio jugador al salir.
        var ballCollider = ball.GetComponent<Collider>();
        if (ownCollider != null && ballCollider != null) Physics.IgnoreCollision(ownCollider, ballCollider);

        ball.Launch(velocity);
    }

    Vector3 GetMouseTarget(Vector2 screenPos)
    {
        Ray ray = aimCamera.ScreenPointToRay(screenPos);
        var hits = Physics.RaycastAll(ray, maxAimDistance, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        Vector3 point = ray.GetPoint(maxAimDistance);
        foreach (var h in hits)
        {
            if (h.collider == ownCollider || h.distance >= best) continue;
            best = h.distance;
            point = h.point;
        }
        return point;
    }

    // Velocidad para que la pelota, con gravedad, llegue al objetivo (tiro bajo).
    Vector3 AimAt(Vector3 target)
    {
        Vector3 start = throwPoint.position;
        Vector3 delta = target - start;
        Vector3 flat = new Vector3(delta.x, 0f, delta.z);
        float d = flat.magnitude;
        float h = delta.y;
        float g = -Physics.gravity.y;
        float v = throwSpeed;

        if (d < 0.01f) return delta.normalized * v;

        float root = v * v * v * v - g * (g * d * d + 2f * h * v * v);
        if (root < 0f) return delta.normalized * v; // fuera de alcance: tiro recto

        float angle = Mathf.Atan((v * v - Mathf.Sqrt(root)) / (g * d));
        return flat.normalized * (Mathf.Cos(angle) * v) + Vector3.up * (Mathf.Sin(angle) * v);
    }
}
