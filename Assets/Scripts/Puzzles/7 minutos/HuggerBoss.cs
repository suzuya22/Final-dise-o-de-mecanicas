using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// La persona del pasillo. Se acerca para abrazarte (cuanto más cerca, más cordura pierdes)
// y hace amagues: se inclina hacia un lado, a veces finge, y se lanza.
// Si se lanza hacia el lado de un hilo tenso, tropieza y cae boca abajo: su espalda muestra
// los puntos débiles. Con los golpes necesarios se abre la puerta, la luz la asusta y se esconde.
public class HuggerBoss : MonoBehaviour
{
    enum BossState { Idle, Approach, Telegraph, Lunge, Fallen, Defeated }

    [Header("Referencias")]
    public PlayerMovement player;
    public SanityManager sanity;
    [Tooltip("El modelo de la persona (hijo de este objeto). Se inclina y cae sin mover la lógica.")]
    public Transform model;
    [Tooltip("Los 2 hilos del suelo, uno a cada lado del pasillo.")]
    public FloorThread[] floorThreads;
    [Tooltip("Los puntos débiles de la espalda.")]
    public BossWeakPoint[] weakPoints;
    [Tooltip("Lanzador de pelotas del jugador; se activa durante la pelea.")]
    public BallThrower ballThrower;
    [Tooltip("Puerta que se abre al vencerla (con su luz).")]
    public PuzzleDoor exitDoor;
    [Tooltip("Esquina donde se esconde al final.")]
    public Transform hidePoint;

    [Header("Acercarse")]
    public float approachSpeed = 0.7f;
    [Tooltip("Distancia a la que se queda abrazando (no se acerca más).")]
    public float hugDistance = 0.8f;
    [Tooltip("Segundos acercándose entre amague y amague.")]
    public Vector2 approachTime = new Vector2(2f, 4f);

    [Header("Cordura")]
    [Tooltip("Distancia desde la que empieza a quitar cordura.")]
    public float drainRadius = 5f;
    [Tooltip("Cordura por segundo cuando está pegada a ti.")]
    public float maxDrainPerSecond = 15f;

    [Header("Amagues")]
    [Tooltip("Segundos que se inclina hacia un lado antes de lanzarse.")]
    public float telegraphTime = 0.9f;
    [Tooltip("Probabilidad de fingir hacia un lado y lanzarse hacia el otro.")]
    [Range(0f, 1f)] public float feintChance = 0.4f;
    public float leanAngle = 20f;
    public float lungeSpeed = 4f;
    public float lungeDistance = 1.5f;
    [Tooltip("Qué tanto va el lanzamiento hacia los lados (1) o hacia el jugador (0).")]
    [Range(0f, 1f)] public float lungeSideAmount = 0.7f;

    [Header("Caída")]
    [Tooltip("Golpes necesarios para abrir la puerta.")]
    public int hitsToWin = 3;
    [Tooltip("Segundos que se queda en el suelo.")]
    public float fallenTime = 4f;
    [Tooltip("Grados que gira el modelo al caer boca abajo.")]
    public float fallAngle = 85f;
    public float fallSpeed = 4f;

    [Header("Final")]
    public float hideSpeed = 2.5f;
    [Tooltip("Cuánto se encoge en la esquina (0.7 = 70 % de su tamaño).")]
    public float cowerScale = 0.7f;

    [Header("Eventos")]
    public UnityEvent onFightStart;
    public UnityEvent onFall;
    public UnityEvent onWeakPointHit;
    public UnityEvent onDefeated;

    public int Hits { get; private set; }

    BossState state = BossState.Idle;
    Vector3 startPosition;
    Quaternion startRotation;
    Quaternion modelBaseRotation;
    Vector3 modelBaseScale;
    float currentLean, currentFall, currentScale = 1f;
    float targetLean, targetFall, targetScale = 1f;
    Coroutine routine;

    void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        if (model != null)
        {
            modelBaseRotation = model.localRotation;
            modelBaseScale = model.localScale;
        }
        foreach (var wp in weakPoints)
            if (wp != null) wp.Init(this);
        if (ballThrower != null) ballThrower.enabled = false;
    }

    // Conéctalo a un PlayerTrigger en la entrada del pasillo.
    public void StartFight()
    {
        if (state != BossState.Idle) return;
        if (ballThrower != null) ballThrower.enabled = true;
        onFightStart.Invoke();
        routine = StartCoroutine(FightLoop());
    }

    IEnumerator FightLoop()
    {
        while (true)
        {
            // Acercarse
            state = BossState.Approach;
            targetLean = 0f;
            float approachEnd = Time.time + Random.Range(approachTime.x, approachTime.y);
            while (Time.time < approachEnd)
            {
                FacePlayer();
                Vector3 toPlayer = Flat(player.transform.position - transform.position);
                if (toPlayer.magnitude > hugDistance)
                    transform.position += toPlayer.normalized * (approachSpeed * Time.deltaTime);
                yield return null;
            }

            // Amague: se inclina hacia un lado; a veces cambia al otro
            state = BossState.Telegraph;
            float side = Random.value < 0.5f ? -1f : 1f;
            if (Random.value < feintChance)
            {
                targetLean = side;
                yield return new WaitForSeconds(telegraphTime);
                side = -side;
            }
            targetLean = side;
            yield return new WaitForSeconds(telegraphTime);

            // Se lanza
            state = BossState.Lunge;
            Vector3 forward = Flat(player.transform.position - transform.position).normalized;
            Vector3 lateral = Vector3.Cross(Vector3.up, forward) * side;
            Vector3 lungeDir = Vector3.Lerp(forward, lateral, lungeSideAmount).normalized;
            float travelled = 0f;
            bool tripped = false;
            while (travelled < lungeDistance)
            {
                if (IsThreadTenseOnSide(lateral)) { tripped = true; break; }
                float step = lungeSpeed * Time.deltaTime;
                transform.position += lungeDir * step;
                travelled += step;
                yield return null;
            }
            targetLean = 0f;

            if (tripped)
            {
                yield return Fall();
                if (Hits >= hitsToWin)
                {
                    yield return Defeat();
                    yield break;
                }
            }
        }
    }

    IEnumerator Fall()
    {
        state = BossState.Fallen;
        targetFall = 1f;
        onFall.Invoke();
        yield return new WaitForSeconds(1f / fallSpeed);

        foreach (var wp in weakPoints)
            if (wp != null) wp.SetExposed(true);

        float end = Time.time + fallenTime;
        while (Time.time < end && Hits < hitsToWin) yield return null;

        foreach (var wp in weakPoints)
            if (wp != null) wp.SetExposed(false);

        if (Hits < hitsToWin)
        {
            targetFall = 0f;
            yield return new WaitForSeconds(1f / fallSpeed);
        }
    }

    IEnumerator Defeat()
    {
        state = BossState.Defeated;
        if (ballThrower != null) ballThrower.enabled = false;
        if (exitDoor != null) exitDoor.Open();
        onDefeated.Invoke();

        // Se levanta asustada y corre a esconderse en la esquina.
        targetFall = 0f;
        yield return new WaitForSeconds(1f / fallSpeed);
        if (hidePoint != null)
        {
            while (Flat(hidePoint.position - transform.position).magnitude > 0.05f)
            {
                Vector3 dir = Flat(hidePoint.position - transform.position);
                transform.position += dir.normalized * Mathf.Min(dir.magnitude, hideSpeed * Time.deltaTime);
                if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                yield return null;
            }
            // Mira hacia la pared, de espaldas a la luz.
            transform.rotation = hidePoint.rotation;
        }
        targetScale = cowerScale;
    }

    // Lo llama el BossWeakPoint al recibir un pelotazo.
    public void OnWeakPointHit()
    {
        Hits++;
        onWeakPointHit.Invoke();
    }

    bool IsThreadTenseOnSide(Vector3 lateral)
    {
        foreach (var t in floorThreads)
        {
            if (t == null || !t.IsTense) continue;
            if (Vector3.Dot(Flat(t.transform.position - transform.position), lateral) > 0f) return true;
        }
        return false;
    }

    void Update()
    {
        // Cordura según la distancia (no en reposo ni al final).
        if (sanity != null && player != null && (state == BossState.Approach || state == BossState.Telegraph || state == BossState.Lunge))
        {
            float dist = Flat(player.transform.position - transform.position).magnitude;
            float closeness = 1f - Mathf.Clamp01((dist - hugDistance) / Mathf.Max(0.01f, drainRadius - hugDistance));
            sanity.Drain(maxDrainPerSecond * closeness * Time.deltaTime);
        }

        UpdateModel();
    }

    void UpdateModel()
    {
        if (model == null) return;
        float k = 1f - Mathf.Exp(-fallSpeed * 2f * Time.deltaTime);
        currentLean = Mathf.Lerp(currentLean, targetLean, k);
        currentFall = Mathf.Lerp(currentFall, targetFall, k);
        currentScale = Mathf.Lerp(currentScale, targetScale, k);

        // Inclinación lateral en Z (amague) y caída hacia delante en X.
        model.localRotation = modelBaseRotation * Quaternion.Euler(currentFall * fallAngle, 0f, -currentLean * leanAngle);
        model.localScale = modelBaseScale * currentScale;
    }

    void FacePlayer()
    {
        Vector3 dir = Flat(player.transform.position - transform.position);
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 1f - Mathf.Exp(-5f * Time.deltaTime));
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    // Lo llama SevenMinutesPuzzle al reiniciar.
    public void ResetState()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        state = BossState.Idle;
        Hits = 0;
        transform.SetPositionAndRotation(startPosition, startRotation);
        targetLean = currentLean = 0f;
        targetFall = currentFall = 0f;
        targetScale = currentScale = 1f;
        UpdateModel();

        foreach (var wp in weakPoints)
            if (wp != null) wp.ResetState();
        foreach (var t in floorThreads)
            if (t != null) t.ResetState();
        if (ballThrower != null) ballThrower.enabled = false;
        if (exitDoor != null) exitDoor.ResetState();
    }
}
