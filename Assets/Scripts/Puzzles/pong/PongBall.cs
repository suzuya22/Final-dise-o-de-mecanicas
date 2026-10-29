using UnityEngine;

// Pelota de ping pong: física de rebote y detección de cuándo termina el tiro.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class PongBall : MonoBehaviour
{
    [Header("Física")]
    [Range(0f, 1f)] public float bounciness = 0.8f;
    [Range(0f, 1f)] public float friction = 0.2f;
    [Tooltip("Una pelota real pesa unos 2.7 g.")]
    public float mass = 0.0027f;
    [Tooltip("Resistencia del aire. Si es mayor a 0, la parábola de previsualización deja de ser exacta.")]
    public float airDrag = 0f;
    [Tooltip("Giro aleatorio al lanzar, para que los rebotes no sean siempre iguales.")]
    public float randomSpin = 10f;

    [Header("Fin del tiro")]
    [Tooltip("Segundos máximos que dura un tiro.")]
    public float maxLifetime = 6f;
    [Tooltip("Si la pelota cae por debajo de esta altura (Y mundial), cuenta como fallo.")]
    public float killHeight = -5f;
    [Tooltip("Velocidad por debajo de la cual se considera que la pelota está quieta.")]
    public float restSpeed = 0.05f;
    [Tooltip("Segundos que debe estar quieta para terminar el tiro.")]
    public float restTime = 1f;

    public int ColorIndex { get; private set; }
    public bool Launched { get; private set; }

    BeerPongManager manager;
    Rigidbody rb;
    SphereCollider col;
    float launchTime;
    float restTimer;
    bool finished;
    bool inFlight;
    float driftSeed;

    // La borrachera tuerce el tiro: mientras vuela (hasta el primer rebote) recibe un empujón lateral
    // suave que va cambiando de dirección, proporcional al nivel de borrachera.
    void ApplyDrunkDrift()
    {
        if (!inFlight || manager == null || manager.drunkDrift <= 0f) return;

        float level = manager.CurrentDrunk;
        if (level <= 0f) return;

        Vector3 flat = rb.linearVelocity;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f) return;

        Vector3 lateral = Vector3.Cross(Vector3.up, flat.normalized);
        float noise = Mathf.PerlinNoise(driftSeed, (Time.time - launchTime) * manager.drunkDriftFrequency) * 2f - 1f;
        rb.AddForce(lateral * (noise * manager.drunkDrift * level), ForceMode.Acceleration);
    }

    void OnCollisionEnter(Collision collision)
    {
        inFlight = false;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<SphereCollider>();

        rb.mass = mass;
        rb.linearDamping = airDrag;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        col.material = new PhysicsMaterial("PongBall")
        {
            bounciness = bounciness,
            dynamicFriction = friction,
            staticFriction = friction,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum,
        };

        Hold();
    }

    public void Init(BeerPongManager owner, int colorIndex, Color color)
    {
        manager = owner;
        ColorIndex = colorIndex;
        BeerPongManager.Tint(GetComponentsInChildren<Renderer>(), color);
    }

    // Mientras está en la mano: sin física ni colisiones.
    public void Hold()
    {
        rb.isKinematic = true;
        col.enabled = false;
    }

    public void Launch(Vector3 velocity)
    {
        col.enabled = true;
        rb.isKinematic = false;
        rb.linearVelocity = velocity;
        rb.angularVelocity = Random.insideUnitSphere * randomSpin;

        Launched = true;
        launchTime = Time.time;
        restTimer = 0f;
        inFlight = true;
        driftSeed = Random.Range(0f, 100f);
    }

    void FixedUpdate()
    {
        if (!Launched || finished) return;

        ApplyDrunkDrift();

        if (transform.position.y < killHeight || Time.time - launchTime > maxLifetime)
        {
            Finish(null);
            return;
        }

        if (rb.linearVelocity.magnitude < restSpeed)
        {
            restTimer += Time.fixedDeltaTime;
            if (restTimer >= restTime) Finish(null);
        }
        else
        {
            restTimer = 0f;
        }
    }

    // Los vasos tienen un trigger en el fondo; si la pelota lo toca, entró.
    void OnTriggerEnter(Collider other)
    {
        if (!Launched || finished) return;

        var cup = other.GetComponentInParent<PongCup>();
        if (cup != null && cup.IsAvailable) Finish(cup);
    }

    void Finish(PongCup cup)
    {
        finished = true;
        if (manager != null) manager.OnBallFinished(this, cup);
        else Destroy(gameObject, 1f);
    }
}
