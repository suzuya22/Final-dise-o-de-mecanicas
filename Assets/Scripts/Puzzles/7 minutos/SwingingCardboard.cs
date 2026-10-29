using UnityEngine;

// Cartón colgante que se balancea como un péndulo. Si golpea al jugador, lo empuja hacia atrás.
// Va en un objeto vacío colocado en el punto de donde cuelga (con el cartón como hijo).
// El cartón necesita un Collider marcado como "Is Trigger".
[RequireComponent(typeof(Rigidbody))]
public class SwingingCardboard : MonoBehaviour
{
    [Header("Balanceo")]
    [Tooltip("Grados que se aleja de la vertical hacia cada lado.")]
    public float swingAngle = 35f;
    [Tooltip("Segundos que tarda una ida y vuelta completa.")]
    public float period = 2.5f;
    [Tooltip("Eje local sobre el que se balancea.")]
    public Vector3 swingAxis = Vector3.forward;
    [Tooltip("Desfase al azar para que no se muevan todos a la vez.")]
    public bool randomPhase = true;

    [Header("Golpe")]
    public PlayerMovement player;
    [Tooltip("Fuerza del empujón.")]
    public float knockbackForce = 6f;
    [Tooltip("Dirección del empujón (por ejemplo hacia el inicio de la habitación). Si está vacío, empuja en la dirección del balanceo.")]
    public Transform pushTowards;
    [Tooltip("Cordura que quita cada golpe (0 = nada).")]
    public float sanityDamage = 0f;
    public SanityManager sanity;

    Rigidbody rb;
    Quaternion baseRotation;
    float phase;
    float lastHitTime = -10f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        baseRotation = transform.localRotation;
        phase = randomPhase ? Random.value * Mathf.PI * 2f : 0f;
    }

    void FixedUpdate()
    {
        float angle = Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, period) + phase) * swingAngle;
        Quaternion local = baseRotation * Quaternion.AngleAxis(angle, swingAxis);
        Quaternion world = transform.parent != null ? transform.parent.rotation * local : local;
        rb.MoveRotation(world);
    }

    void OnTriggerEnter(Collider other)
    {
        if (player == null || other.GetComponentInParent<PlayerMovement>() != player) return;
        if (Time.time - lastHitTime < 0.5f) return;
        lastHitTime = Time.time;

        Vector3 dir;
        if (pushTowards != null)
        {
            dir = pushTowards.position - player.transform.position;
        }
        else
        {
            // Hacia donde se está moviendo el cartón en este momento.
            float velocitySign = Mathf.Sign(Mathf.Cos(Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, period) + phase));
            dir = Vector3.Cross(transform.TransformDirection(swingAxis), Vector3.up) * velocitySign;
        }
        dir.y = 0f;
        player.AddKnockback(dir.normalized * knockbackForce);

        if (sanity != null && sanityDamage > 0f) sanity.Drain(sanityDamage);
    }
}
