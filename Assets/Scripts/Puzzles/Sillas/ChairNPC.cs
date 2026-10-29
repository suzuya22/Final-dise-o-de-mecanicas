using UnityEngine;

// Participante NPC de las sillas musicales: gira alrededor del centro mientras suena la música
// y, cuando para, corre a la silla libre más cercana. El MusicalChairsGame lo controla.
public class ChairNPC : MonoBehaviour
{
    enum NpcState { Idle, Circling, Rushing, Seated, Eliminated }

    [Header("Animación (opcional)")]
    [Tooltip("Animator del modelo. Usa los parámetros del ProtaController: IsWalking, IsRunning y Speed.")]
    public Animator animator;

    [Header("Movimiento")]
    [Tooltip("Velocidad al caminar en círculo, en m/s.")]
    public float walkSpeed = 1.5f;
    [Tooltip("Velocidad al correr a por una silla, en m/s.")]
    public float runSpeed = 3f;
    [Tooltip("Tiempo de reacción al parar la música (mínimo y máximo, en segundos).")]
    public Vector2 reactionTime = new Vector2(0.3f, 0.9f);
    public float turnSpeed = 10f;

    [Header("Sentado (opcional)")]
    [Tooltip("Clip de la pose sentada (\"Armature|Sit down\" dentro de Prota sentao.fbx). Vacío = se queda de pie.")]
    public AnimationClip sitClip;
    [Tooltip("Segundos que tarda en sentarse (0 = de golpe).")]
    public float sitDuration = 0.4f;

    public bool IsSeated => state == NpcState.Seated;
    public bool IsEliminated => state == NpcState.Eliminated;
    public bool IsCircling => state == NpcState.Circling;

    NpcState state = NpcState.Idle;
    MusicalChairsGame game;
    MusicalChair targetChair;
    float angle;
    float rushStartTime;
    Vector3 startPosition;
    Quaternion startRotation;
    bool walking, running;
    float sitTimer;

    void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void Init(MusicalChairsGame owner, float startAngle)
    {
        game = owner;
        angle = startAngle;
        state = NpcState.Idle;
        gameObject.SetActive(true);
        transform.position = game.RingPoint(angle);
        transform.rotation = Quaternion.LookRotation(game.RingTangent(angle), Vector3.up);
    }

    public void StartCircling()
    {
        if (state == NpcState.Eliminated) return;
        // Retoma el círculo desde donde esté (por ejemplo, al levantarse de la silla).
        Vector3 offset = transform.position - game.Center;
        angle = Mathf.Atan2(offset.z, offset.x);
        targetChair = null;
        state = NpcState.Circling;
    }

    public void StartRush()
    {
        if (state == NpcState.Eliminated) return;
        state = NpcState.Rushing;
        targetChair = null;
        rushStartTime = Time.time + Random.Range(reactionTime.x, reactionTime.y);
    }

    public void StandUp()
    {
        if (targetChair != null && targetChair.Occupant == this) targetChair.Release();
        targetChair = null;
        if (state == NpcState.Seated) state = NpcState.Idle;
        if (animator != null) animator.enabled = true;
    }

    public void Eliminate()
    {
        StandUp();
        state = NpcState.Eliminated;
        gameObject.SetActive(false);
    }

    // Deja de caminar o correr; si está sentado, sigue sentado.
    public void Stop()
    {
        if (state == NpcState.Circling || state == NpcState.Rushing) state = NpcState.Idle;
    }

    void Update()
    {
        walking = false;
        running = false;
        if (game == null) return;

        if (state == NpcState.Circling)
        {
            float radius = Mathf.Max(0.1f, game.WalkRadius);
            angle += game.Direction * (walkSpeed / radius) * Time.deltaTime;
            MoveTowards(game.RingPoint(angle), walkSpeed * 1.5f);
            walking = true;
        }
        else if (state == NpcState.Rushing && Time.time >= rushStartTime)
        {
            if (targetChair == null || !targetChair.IsFree) targetChair = game.NearestFreeChair(transform.position);
            if (targetChair != null)
            {
                Vector3 seat = targetChair.Seat.position;
                MoveTowards(seat, runSpeed);
                walking = running = true;
                if (Flat(seat - transform.position).magnitude < 0.25f && targetChair.TryOccupy(this))
                {
                    transform.SetPositionAndRotation(game.SeatPosition(targetChair), game.SeatRotation(targetChair));
                    state = NpcState.Seated;
                    walking = running = false;
                    sitTimer = 0f;
                    if (sitClip != null && animator != null) animator.enabled = false;
                }
            }
        }
        else if (state == NpcState.Seated && sitClip != null && animator != null)
        {
            // Pose sentada: se recorre el clip hasta su último fotograma y se queda ahí.
            sitTimer += Time.deltaTime;
            float t = sitDuration > 0f ? Mathf.Clamp01(sitTimer / sitDuration) : 1f;
            sitClip.SampleAnimation(animator.gameObject, t * sitClip.length);
        }

        UpdateAnimator();
    }

    void MoveTowards(Vector3 target, float speed)
    {
        // Siempre a la altura del objetivo (el suelo del círculo), para no quedarse flotando.
        Vector3 pos = transform.position;
        pos.y = Mathf.MoveTowards(pos.y, target.y, 3f * Time.deltaTime);
        transform.position = pos;

        Vector3 to = Flat(target - transform.position);
        if (to.sqrMagnitude < 1e-8f) return;
        Vector3 step = to.normalized * Mathf.Min(to.magnitude, speed * Time.deltaTime);
        transform.position += step;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    // La animación depende de lo que está haciendo (caminar en círculo / correr a la silla),
    // no del movimiento de cada fotograma: así no parpadea entre caminar y quieto.
    void UpdateAnimator()
    {
        if (animator == null || !animator.isActiveAndEnabled) return;
        animator.SetBool("IsWalking", walking);
        animator.SetBool("IsRunning", running);
        animator.SetFloat("Speed", running ? 2f : walking ? 1f : 0f);
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    public void ResetState()
    {
        targetChair = null;
        state = NpcState.Idle;
        if (animator != null) animator.enabled = true;
        gameObject.SetActive(true);
        transform.SetPositionAndRotation(startPosition, startRotation);
    }
}
